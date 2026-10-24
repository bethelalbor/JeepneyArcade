using UnityEngine;


public class VehicleFeedbackText : MonoBehaviour
{
    public static VehicleFeedbackText Instance;


    [Header("References")]
    [SerializeField] private Transform feedbackSpawnPoint;
    [SerializeField] private Transform balanceFeedbackSpawnPoint;
    [SerializeField] private FloatingFeedbackText feedbackTextPrefab;


    [Header("Defaults")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private float defaultFontSize = 3f;
    [SerializeField] private Color balanceColor = Color.green;
    [SerializeField] private float balanceFontSize = 3f;



    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Destroy(this);
        }
    }



    void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }



    public void Show(string message)
    {
        Show(message, defaultColor, defaultFontSize);
    }



    public void Show(string message, Color color)
    {
        Show(message, color, defaultFontSize);
    }



    public void Show(
        string message,
        Color color,
        float fontSize
    )
    {
        SpawnFeedback(
            message,
            color,
            fontSize,
            feedbackSpawnPoint,
            FeedbackAnimationStyle.RiseAndFade
        );
    }



    public void ShowBalance(string message)
    {
        ShowBalance(message, balanceColor);
    }



    public void ShowBalance(string message, Color color)
    {
        SpawnFeedback(
            message,
            color,
            balanceFontSize,
            balanceFeedbackSpawnPoint,
            FeedbackAnimationStyle.Pop
        );
    }



    void SpawnFeedback(
        string message,
        Color color,
        float fontSize,
        Transform spawnPoint,
        FeedbackAnimationStyle animationStyle
    )
    {
        if(string.IsNullOrWhiteSpace(message) ||
           spawnPoint == null ||
           feedbackTextPrefab == null)
        {
            return;
        }


        FloatingFeedbackText feedbackText =
            Instantiate(
                feedbackTextPrefab,
                spawnPoint.position,
                Quaternion.identity
            );


        feedbackText.Initialize(
            message,
            color,
            fontSize,
            animationStyle
        );
    }
}
