
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField, Min(1f)] private float readinessTimeout = 10f;
    private string initializationError;

    private void OnDestroy()
    {
        Application.logMessageReceived -= CaptureInitializationError;
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        UpdateProgress(0f);

        Scene loadingScene = gameObject.scene;
        Canvas loadingCanvas = progressFill != null ? progressFill.GetComponentInParent<Canvas>() : null;
        if (loadingCanvas == null && progressText != null)
            loadingCanvas = progressText.GetComponentInParent<Canvas>();
        if (loadingCanvas == null)
        {
            yield return FailLoading("Loading canvas is missing.", default(Scene));
            yield break;
        }
        loadingCanvas = loadingCanvas.rootCanvas;
        loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadingCanvas.overrideSorting = true;
        loadingCanvas.sortingOrder = short.MaxValue;

        // The Loading scene stays present as an opaque cover while BGCMap initializes.
        // Remove competing cameras, listeners, lights and input systems from Loading only.
        foreach (GameObject root in loadingScene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;
            foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = false;
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
                light.enabled = false;
            foreach (EventSystem events in root.GetComponentsInChildren<EventSystem>(true))
                events.gameObject.SetActive(false);
        }

        // Allow the loading UI to appear first.
        yield return null;

        if (!Application.CanStreamedLevelBeLoaded("BGCMap"))
        {
            yield return FailLoading("BGCMap is missing from the build scene list.", default(Scene));
            yield break;
        }

        Application.logMessageReceived += CaptureInitializationError;
        AsyncOperation loading = null;
        string loadError = null;
        try
        {
            loading = SceneManager.LoadSceneAsync("BGCMap", LoadSceneMode.Additive);
        }
        catch (System.Exception exception)
        {
            loadError = exception.Message;
        }

        if (loading == null)
        {
            yield return FailLoading(loadError ?? "Could not load BGCMap.", default(Scene));
            yield break;
        }

        loading.allowSceneActivation = false;

        float displayedProgress = 0f;
        float elapsed = 0f;

        while (displayedProgress < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            bool readyToActivate = loading.progress >= 0.9f;

            // Estimated visual progress keeps the UI moving when Unity reports
            // coarse updates. Reserve the final 10% for gameplay readiness checks.
            float estimatedProgress = 0.9f * (1f - Mathf.Exp(-elapsed / 8f));
            float reportedProgress = Mathf.Clamp01(loading.progress / 0.9f);
            float targetProgress = readyToActivate
                ? 0.9f
                : Mathf.Min(0.89f, Mathf.Max(estimatedProgress, reportedProgress * 0.9f));

            // Cap the animation step so a loading hitch cannot skip the bar
            // straight to completion on the next rendered frame.
            float animationDelta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                Mathf.Max(displayedProgress, targetProgress),
                0.5f * animationDelta
            );

            UpdateProgress(displayedProgress);

            yield return null;
        }

        // Activation is necessary for Awake/Start to run, but does not reveal gameplay.
        loading.allowSceneActivation = true;
        while (!loading.isDone)
            yield return null;

        Scene gameplayScene = SceneManager.GetSceneByName("BGCMap");
        if (!gameplayScene.IsValid() || !gameplayScene.isLoaded)
        {
            yield return FailLoading("BGCMap did not finish activation.", gameplayScene);
            yield break;
        }
        SceneManager.SetActiveScene(gameplayScene);
        UpdateProgress(0.95f);

        // Resolve once, strictly inside the newly loaded scene, never a previous run.
        PlayerMovement movement = null;
        CameraFollow follow = null;
        GameOverManager gameOver = null;
        foreach (GameObject root in gameplayScene.GetRootGameObjects())
        {
            foreach (PlayerMovement candidate in root.GetComponentsInChildren<PlayerMovement>(true))
            {
                if (candidate.CompareTag("Player"))
                    movement = candidate;
            }
            foreach (CameraFollow candidate in root.GetComponentsInChildren<CameraFollow>(true))
            {
                if (candidate.isActiveAndEnabled)
                    follow = candidate;
            }
            if (gameOver == null)
                gameOver = root.GetComponentInChildren<GameOverManager>(true);
        }
        FuelSystem fuel = movement != null ? movement.GetComponent<FuelSystem>() : null;
        VehicleHealth health = movement != null ? movement.GetComponent<VehicleHealth>() : null;
        Camera gameplayCamera = follow != null ? follow.GetComponent<Camera>() : null;
        float startedAt = Time.realtimeSinceStartup;
        float initialGameTime = Time.time;
        int initialPhysicsCount = movement != null ? movement.PhysicsUpdateCount : 0;
        float healthySince = -1f;
        string reason = "Waiting for gameplay initialization.";

        while (Time.realtimeSinceStartup - startedAt < Mathf.Max(1f, readinessTimeout))
        {
            // Give Start, Update, physics and the follow camera time to execute.
            yield return null;
            reason = GetReadinessFailure(movement, fuel, health, follow, gameplayCamera, gameOver,
                initialGameTime, initialPhysicsCount);
            if (initializationError != null)
            {
                reason = initializationError;
                break;
            }
            if (reason != null)
            {
                healthySince = -1f;
                continue;
            }
            if (healthySince < 0f)
                healthySince = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - healthySince < 0.5f)
                continue;

            UpdateProgress(1f);
            // Validate once more after the completion frame before removing the cover.
            yield return null;
            reason = initializationError ?? GetReadinessFailure(movement, fuel, health, follow,
                gameplayCamera, gameOver, initialGameTime, initialPhysicsCount);
            if (reason != null)
                break;
            Application.logMessageReceived -= CaptureInitializationError;
            SceneManager.UnloadSceneAsync(loadingScene);
            yield break;
        }

        yield return FailLoading(reason ?? "Gameplay did not remain ready long enough.", gameplayScene);
    }

    private string GetReadinessFailure(PlayerMovement movement, FuelSystem fuel, VehicleHealth health,
        CameraFollow follow, Camera camera, GameOverManager gameOver, float initialTime, int physicsCount)
    {
        if (!(Time.timeScale > 0f) || !(Time.time > initialTime))
            return "Gameplay time is not advancing.";
        if (movement == null || !movement.IsReadyForGameplay)
            return "Player movement, input, or Rigidbody is not ready.";
        if (movement.PhysicsUpdateCount <= physicsCount)
            return "Player physics is not updating.";
        if (fuel == null || !fuel.isActiveAndEnabled || !(fuel.currentFuel > 0f))
            return "Player fuel system is not ready.";
        if (health == null || !health.isActiveAndEnabled || !(health.currentHealth > 0f))
            return "Player health system is not ready.";
        if (follow == null || !follow.isActiveAndEnabled || follow.target != movement.transform ||
            camera == null || !camera.isActiveAndEnabled)
            return "The gameplay camera is not following the player.";
        if (gameOver != null && gameOver.gameOverCanvas != null && gameOver.gameOverCanvas.activeInHierarchy)
            return "Game over was triggered during startup.";
        return null;
    }

    private void CaptureInitializationError(string message, string stackTrace, LogType type)
    {
        if (initializationError == null && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
            initializationError = "Startup error: " + message;
    }

    private IEnumerator FailLoading(string reason, Scene gameplayScene)
    {
        Application.logMessageReceived -= CaptureInitializationError;
        Debug.LogError("Gameplay startup blocked: " + reason, this);
        if (progressText != null)
            progressText.text = "Unable to start game\n" + reason + "\nReturning to Main Menu...";

        // Remove the failed run while the loading screen still hides it.
        if (gameplayScene.IsValid() && gameplayScene.isLoaded)
        {
            AsyncOperation unloading = SceneManager.UnloadSceneAsync(gameplayScene);
            if (unloading != null)
                yield return unloading;
        }
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(3f);
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateProgress(float progress)
    {
        if (progressFill != null)
            progressFill.fillAmount = progress;

        if (progressText != null)
            progressText.text =
                (progress >= 1f ? 100 : Mathf.FloorToInt(progress * 100f)) + "%";
    }
}
