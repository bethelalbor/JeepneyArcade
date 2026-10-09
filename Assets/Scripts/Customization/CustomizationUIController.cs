using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Temporary functional controls for the Customization scene. Button visuals
/// remain ordinary uGUI Images so they can be replaced with final sprites.
/// </summary>
public class CustomizationUIController : MonoBehaviour
{
    private const string GameplaySceneName = "OnePlayerGame";
    private const string MainMenuSceneName = "MainMenu";

    [SerializeField] private JeepneyPaintManager paintManager;
    [SerializeField] private JeepneyPainter painter;
    [SerializeField] private Text statusText;
    [Tooltip("Non-raycastable cursor Image under CustomizationCanvas. Assign a Figma brush sprite directly on its Image component.")]
    [SerializeField] private Image brushCursor;

    [Header("Temporary Brush Cursor")]
    [SerializeField] private float minimumCursorPixels = 24f;
    [SerializeField] private float maximumCursorPixels = 132f;

    [Header("Persistent Painting Controls")]
    [SerializeField] private Slider brushSizeSlider;
    [SerializeField] private Slider brushOpacitySlider;
    [SerializeField] private Button paintButton;
    [SerializeField] private Button eraserButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;

    private bool cursorHiddenSystemCursor;

    public JeepneyPainter ActivePainter => painter;

    private void Awake()
    {
        // A scene reference normally resolves to the prefab instance. This
        // fallback also covers an unassigned field or a prefab-asset reference.
        if (paintManager == null || !paintManager.gameObject.scene.IsValid())
            paintManager = FindAnyObjectByType<JeepneyPaintManager>();

        if (painter == null || !painter.gameObject.scene.IsValid())
            painter = paintManager != null && paintManager.gameObject.scene.IsValid()
                ? paintManager.GetComponent<JeepneyPainter>()
                : null;

        if (painter == null || !painter.gameObject.scene.IsValid())
            Debug.LogError("Customization UI requires a JeepneyPainter on the active paint-manager object.", this);

        if (brushCursor == null)
            brushCursor = transform.Find("BrushCursor")?.GetComponent<Image>();
    }

    private void Start()
    {
        InitializePersistentPaintingControls();
        UpdateBrushCursor();
    }

    private void Update()
    {
        UpdateBrushCursor();
        UpdateHistoryButtonStates();
    }

    private void OnDisable()
    {
        SetSystemCursorVisible(true);
    }

    private void OnDestroy()
    {
        SetSystemCursorVisible(true);
    }

    public void SavePaint()
    {
        if (!TryGetPaintManager())
            return;

        paintManager.SavePaint();
        SetStatus("Paint Saved");
    }

    public void ResetPaint()
    {
        if (!TryGetPaintManager())
            return;

        paintManager.ResetPaintLayer();
        if (painter != null)
            painter.ClearHistory();
        SetStatus("Paint Reset");
    }

    public void Play()
    {
        if (!TryGetPaintManager())
            return;

        paintManager.SavePaint();
        SetStatus("Loading Game");
        LoadConfiguredScene(GameplaySceneName);
    }

    public void Back()
    {
        SetStatus("Returning to Main Menu");
        LoadConfiguredScene(MainMenuSceneName);
    }

    private bool TryGetPaintManager()
    {
        if (paintManager != null)
            return true;

        Debug.LogError("Customization UI could not find JeepneyPaintManager.", this);
        SetStatus("Paint Manager Missing");
        return false;
    }

    private void UpdateBrushCursor()
    {
        if (brushCursor == null || painter == null ||
            !TryGetCursorPointerPosition(out Vector2 screenPosition, out int pointerId, out bool isMousePointer) ||
            !painter.IsPointerOverPaintableSurface(screenPosition, pointerId))
        {
            if (brushCursor != null && brushCursor.gameObject.activeSelf)
                brushCursor.gameObject.SetActive(false);

            SetSystemCursorVisible(true);
            return;
        }

        RectTransform cursorRect = brushCursor.rectTransform;
        cursorRect.position = screenPosition;
        float sizeT = Mathf.InverseLerp(0.005f, 0.1f, painter.BrushSize);
        float cursorSize = Mathf.Lerp(minimumCursorPixels, maximumCursorPixels, sizeT);
        cursorRect.sizeDelta = Vector2.one * cursorSize;

        if (!brushCursor.gameObject.activeSelf)
            brushCursor.gameObject.SetActive(true);

        SetSystemCursorVisible(!isMousePointer);
    }

    private static bool TryGetCursorPointerPosition(out Vector2 screenPosition, out int pointerId, out bool isMousePointer)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            pointerId = touchscreen.primaryTouch.touchId.ReadValue();
            isMousePointer = false;
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            screenPosition = mouse.position.ReadValue();
            pointerId = PointerInputModule.kMouseLeftId;
            isMousePointer = true;
            return true;
        }

        screenPosition = default;
        pointerId = PointerInputModule.kMouseLeftId;
        isMousePointer = false;
        return false;
    }

    private void SetSystemCursorVisible(bool visible)
    {
        if (visible == !cursorHiddenSystemCursor)
            return;

        Cursor.visible = visible;
        cursorHiddenSystemCursor = !visible;
    }

    private void InitializePersistentPaintingControls()
    {
        if (painter == null)
            return;

        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = 0.005f;
            brushSizeSlider.maxValue = 0.1f;
            brushSizeSlider.SetValueWithoutNotify(Mathf.Clamp(painter.BrushSize, brushSizeSlider.minValue, brushSizeSlider.maxValue));
            brushSizeSlider.onValueChanged.AddListener(painter.SetBrushSize);
        }

        if (brushOpacitySlider != null)
        {
            brushOpacitySlider.minValue = 0f;
            brushOpacitySlider.maxValue = 1f;
            brushOpacitySlider.SetValueWithoutNotify(painter.BrushOpacity);
            brushOpacitySlider.onValueChanged.AddListener(painter.SetBrushOpacity);
        }

        if (paintButton != null)
            paintButton.onClick.AddListener(() => painter.SetEraseMode(false));
        if (eraserButton != null)
            eraserButton.onClick.AddListener(() => painter.SetEraseMode(true));
        UpdateHistoryButtonStates();
    }

    private void UpdateHistoryButtonStates()
    {
        if (painter == null)
            return;

        if (undoButton != null)
            undoButton.interactable = painter.CanUndo;
        if (redoButton != null)
            redoButton.interactable = painter.CanRedo;
    }

    private void LoadConfiguredScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Customization UI cannot load '{sceneName}'. Add the scene to the enabled Build Profile scene list.", this);
            SetStatus("Scene Not Available");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"Customization UI: {message}", this);
    }
}
