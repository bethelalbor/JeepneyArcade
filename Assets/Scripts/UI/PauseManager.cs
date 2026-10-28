using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the in-game pause menu without changing the shared input actions.
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private Button resumeButton;

    private bool isPaused;

    private void Awake()
    {
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    private void Update()
    {
        if (Gamepad.current == null || !Gamepad.current.startButton.wasPressedThisFrame)
            return;

        if (isPaused)
        {
            Resume();
        }
        else if (Time.timeScale > 0f)
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        SelectResumeButton();
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        Time.timeScale = 1f;
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (isPaused)
            Time.timeScale = 1f;
    }

    private void SelectResumeButton()
    {
        if (resumeButton == null || !resumeButton.isActiveAndEnabled || !resumeButton.interactable)
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(resumeButton.gameObject);
    }
}
