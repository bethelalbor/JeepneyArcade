using TMPro;
using UnityEngine;


public enum FeedbackAnimationStyle
{
    RiseAndFade,
    Pop
}


public class FloatingFeedbackText : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float riseSpeed = 0.8f;
    [SerializeField] private float lifetime = 1.25f;

    [Header("Pop Animation")]
    [SerializeField] private float popDuration = 0.6f;
    [SerializeField] private float popScale = 1.3f;


    private TextMeshPro textMesh;
    private Color startColor;
    private float elapsedTime;
    private Vector3 startingScale;
    private FeedbackAnimationStyle animationStyle;



    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();

        startingScale = transform.localScale;
    }



    public void Initialize(
        string message,
        Color color,
        float fontSize,
        FeedbackAnimationStyle style =
            FeedbackAnimationStyle.RiseAndFade
    )
    {
        textMesh.text = message;
        textMesh.color = color;
        textMesh.fontSize = fontSize;

        startColor = color;
        animationStyle = style;
    }



    void Update()
    {
        if(animationStyle == FeedbackAnimationStyle.Pop)
        {
            UpdatePopAnimation();

            return;
        }


        transform.position +=
            Vector3.up * riseSpeed * Time.deltaTime;


        FaceGameplayCamera();


        elapsedTime += Time.deltaTime;

        float fadeProgress = Mathf.Clamp01(
            elapsedTime / lifetime
        );

        Color color = startColor;
        color.a *= 1f - fadeProgress;
        textMesh.color = color;


        if(elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }



    void UpdatePopAnimation()
    {
        FaceGameplayCamera();


        elapsedTime += Time.deltaTime;

        float progress = Mathf.Clamp01(
            elapsedTime / popDuration
        );

        float scale;

        if(progress < 0.4f)
        {
            scale = Mathf.Lerp(
                0f,
                popScale,
                progress / 0.4f
            );
        }
        else
        {
            scale = Mathf.Lerp(
                popScale,
                0f,
                (progress - 0.4f) / 0.6f
            );
        }


        transform.localScale = startingScale * scale;


        if(progress >= 1f)
        {
            Destroy(gameObject);
        }
    }



    void FaceGameplayCamera()
    {
        if(Camera.main == null)
            return;


        Vector3 directionToCamera =
            Camera.main.transform.position - transform.position;


        if(directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(
                -directionToCamera,
                Camera.main.transform.up
            );
        }
    }
}
