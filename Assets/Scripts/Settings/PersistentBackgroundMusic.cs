
using UnityEngine;

public class PersistentBackgroundMusic : MonoBehaviour
{
    private static PersistentBackgroundMusic instance;

    public static void StopMusic()
    {
        if (instance == null)
            return;

        PersistentBackgroundMusic music = instance;
        // Release ownership before the next scene creates its music instance.
        instance = null;

        AudioSource musicSource = music.GetComponent<AudioSource>();
        if (musicSource != null)
            musicSource.Stop();

        Destroy(music.gameObject);
    }

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
