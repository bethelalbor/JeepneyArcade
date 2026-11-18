
using UnityEngine;

public class PersistentBackgroundMusic : MonoBehaviour
{
    private static PersistentBackgroundMusic instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        AudioSource musicSource = GetComponent<AudioSource>();
        if (musicSource != null && PlayerPrefs.HasKey("Arangkada.MusicVolume"))
        {
            musicSource.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Arangkada.MusicVolume"));
        }
    }
}
