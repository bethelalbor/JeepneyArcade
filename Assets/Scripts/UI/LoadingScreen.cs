
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;

    private IEnumerator Start()
    {
        UpdateProgress(0f);

        // Allow the loading UI to appear first.
        yield return null;

        AsyncOperation loading =
            SceneManager.LoadSceneAsync("BGCMap");

        if (loading == null)
        {
            Debug.LogError("Could not load BGCMap.");
            yield break;
        }

        loading.allowSceneActivation = false;

        float displayedProgress = 0f;
        float elapsed = 0f;

        while (displayedProgress < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            bool readyToActivate = loading.progress >= 0.9f;

            // Estimated visual progress keeps the UI moving when Unity reports
            // coarse updates. Only a scene ready for activation can reach 100%.
            float estimatedProgress = 0.9f * (1f - Mathf.Exp(-elapsed / 8f));
            float reportedProgress = Mathf.Clamp01(loading.progress / 0.9f);
            float targetProgress = readyToActivate
                ? 1f
                : Mathf.Min(0.95f, Mathf.Max(estimatedProgress, reportedProgress));

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

        // Let the completed bar render before activating gameplay.
        yield return new WaitForSecondsRealtime(0.25f);

        loading.allowSceneActivation = true;
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
