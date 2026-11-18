
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
    }
}
