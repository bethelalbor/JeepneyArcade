using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    private const string MusicVolumeKey = "Arangkada.MusicVolume";

    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumePercentage;
    [SerializeField] private GameObject helpPanel;

    private AudioSource musicSource;
    private bool missingMusicWarningReported;

    private void OnEnable()
    {
        PersistentBackgroundMusic music = FindAnyObjectByType<PersistentBackgroundMusic>();
        musicSource = music != null ? music.GetComponent<AudioSource>() : null;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.wholeNumbers = false;
        }

        if (musicSource == null)
        {
            ReportMissingMusic();
            return;
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(musicSource.volume);
            musicVolumeSlider.interactable = true;
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        UpdatePercentage(musicSource.volume);
    }

    private void OnDisable()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
    }

    private void SetMusicVolume(float value)
    {
        if (musicSource == null)
        {
            ReportMissingMusic();
            return;
        }

        float volume = Mathf.Clamp01(value);
        musicSource.volume = volume;
        UpdatePercentage(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
    }

    private void UpdatePercentage(float volume)
    {
        if (musicVolumePercentage != null)
            musicVolumePercentage.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    private void ReportMissingMusic()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.interactable = false;

        if (!missingMusicWarningReported)
        {
            Debug.LogWarning("Music volume is unavailable: the PersistentBackgroundMusic instance or its AudioSource is missing.", this);
            missingMusicWarningReported = true;
        }
    }

    public void OpenHelp()
    {
        if (helpPanel != null)
            helpPanel.SetActive(true);
    }

    public void CloseHelp()
    {
        if (helpPanel != null)
            helpPanel.SetActive(false);
    }

    public void BackToMainMenu()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }
}
