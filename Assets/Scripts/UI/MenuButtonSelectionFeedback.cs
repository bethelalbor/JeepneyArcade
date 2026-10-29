using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Visually emphasizes a UI button while it has EventSystem selection.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuButtonSelectionFeedback : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Selected Appearance")]
    [SerializeField, Min(1f)] private float selectedScale = 1.05f;
    [SerializeField, Range(0f, 10f)] private float tiltDegrees = 3f;
    [SerializeField, Min(0f)] private float transitionSpeed = 12f;

    private RectTransform rectTransform;
    private Vector3 defaultScale;
    private Quaternion defaultRotation;
    private Vector3 targetScale;
    private Quaternion targetRotation;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        defaultScale = rectTransform.localScale;
        defaultRotation = rectTransform.localRotation;
        targetScale = defaultScale;
        targetRotation = defaultRotation;
    }

    private void Update()
    {
        float interpolation = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, interpolation);
        rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, interpolation);
    }

    public void OnSelect(BaseEventData eventData)
    {
        targetScale = defaultScale * selectedScale;
        float tilt = Random.value < 0.5f ? -tiltDegrees : tiltDegrees;
        targetRotation = defaultRotation * Quaternion.Euler(0f, 0f, tilt);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        targetScale = defaultScale;
        targetRotation = defaultRotation;
    }
}
