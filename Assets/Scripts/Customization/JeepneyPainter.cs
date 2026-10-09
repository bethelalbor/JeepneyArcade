using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Reads pointer input on the Jeepney's left paint panel and paints the
/// existing runtime RenderTexture through a GPU ping-pong pass.
/// </summary>
[DisallowMultipleComponent]
public class JeepneyPainter : MonoBehaviour
{
    private const string BrushShaderName = "Jeepney/Paint Brush";

    [Header("Raycast Setup")]
    [SerializeField] private Camera paintCamera;
    [SerializeField] private string paintablePanelName = "Side_Panel_Left";
    [Min(0.1f)] [SerializeField] private float maxRaycastDistance = 100f;

    [Header("Runtime Paint")]
    [SerializeField] private JeepneyPaintManager paintManager;
    [SerializeField] private bool paintingEnabled = true;
    [SerializeField] private Color brushColor = Color.red;
    [Range(0.001f, 0.25f)] [SerializeField] private float brushSize = 0.025f;
    [Range(0f, 1f)] [SerializeField] private float brushOpacity = 1f;
    [SerializeField] private bool eraseMode;

    [Header("GPU Undo / Redo")]
    [Min(1)] [SerializeField] private int maxHistoryStates = 5;

    [Header("Debug")]
    [SerializeField] private bool logUvCoordinates;
    [SerializeField] private bool clearRuntimePaint;

    private Transform paintablePanel;
    private GameObject raycastProxy;
    private MeshCollider paintableCollider;
    private RenderTexture scratchPaintTexture;
    private Material brushMaterial;
    private Vector2 previousUv;
    private bool hasPreviousUv;
    private bool strokeActive;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private readonly List<RenderTexture> undoHistory = new List<RenderTexture>();
    private readonly List<RenderTexture> redoHistory = new List<RenderTexture>();

    public float BrushSize => brushSize;
    public float BrushOpacity => brushOpacity;
    public bool IsEraseMode => eraseMode;
    public bool CanUndo => undoHistory.Count > 0;
    public bool CanRedo => redoHistory.Count > 0;

    private void Awake()
    {
        if (paintCamera == null)
            paintCamera = Camera.main;
        if (paintManager == null)
            paintManager = GetComponent<JeepneyPaintManager>();

        FindPaintablePanel();
        CreateRaycastProxy();
        CreateBrushMaterial();
        CreateScratchPaintTexture();
    }

    private void Update()
    {
        if (clearRuntimePaint)
        {
            ClearRuntimePaint();
            clearRuntimePaint = false;
        }

        if (!paintingEnabled || !TryGetPointerPosition(out Vector2 screenPosition, out int pointerId) ||
            paintCamera == null || paintableCollider == null || paintManager == null || paintManager.paintTexture == null)
        {
            EndCurrentStroke();
            return;
        }

        if (!TryGetPaintUv(screenPosition, pointerId, out Vector2 currentUv))
        {
            EndCurrentStroke();
            return;
        }

        if (!strokeActive)
        {
            hasPreviousUv = false;
            if (!CaptureUndoSnapshotForNewStroke())
                return;
            strokeActive = true;
        }

        PaintStroke(hasPreviousUv ? previousUv : currentUv, currentUv);
        previousUv = currentUv;
        hasPreviousUv = true;

        if (logUvCoordinates)
            Debug.Log($"Side_Panel_Left UV1: {currentUv}", this);
    }

    private void LateUpdate() => SyncRaycastProxy();

    public bool IsPointerOverPaintableSurface(Vector2 screenPosition, int pointerId)
    {
        return paintingEnabled && TryGetPaintUv(screenPosition, pointerId, out _);
    }

    public void SetBrushColor(Color color)
    {
        brushColor = color;
        eraseMode = false;
        Debug.Log($"Brush color changed to {color}.", this);
    }

    public void SetBrushSize(float size) => brushSize = Mathf.Clamp(size, 0.001f, 0.25f);
    public void SetBrushOpacity(float opacity) => brushOpacity = Mathf.Clamp01(opacity);
    public void SetEraseMode(bool enabled) => eraseMode = enabled;

    public void Undo()
    {
        EndCurrentStroke();
        if (paintManager == null || paintManager.paintTexture == null || undoHistory.Count == 0)
            return;
        if (!PushSnapshot(redoHistory, paintManager.paintTexture, "Redo"))
            return;
        RestoreAndRemoveLatest(undoHistory);
    }

    public void Redo()
    {
        EndCurrentStroke();
        if (paintManager == null || paintManager.paintTexture == null || redoHistory.Count == 0)
            return;
        if (!PushSnapshot(undoHistory, paintManager.paintTexture, "Undo"))
            return;
        RestoreAndRemoveLatest(redoHistory);
    }

    public void ClearHistory()
    {
        ReleaseHistory(undoHistory);
        ReleaseHistory(redoHistory);
        EndCurrentStroke();
    }

    private void FindPaintablePanel()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name != paintablePanelName)
                continue;
            MeshFilter meshFilter = child.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                paintablePanel = meshFilter.transform;
                return;
            }
        }
        Debug.LogError($"JeepneyPainter could not find mesh '{paintablePanelName}'.", this);
    }

    private void CreateRaycastProxy()
    {
        if (paintablePanel == null)
            return;
        MeshFilter meshFilter = paintablePanel.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;
        raycastProxy = new GameObject($"{paintablePanelName}_PaintRaycastProxy");
        paintableCollider = raycastProxy.AddComponent<MeshCollider>();
        paintableCollider.sharedMesh = meshFilter.sharedMesh;
        SyncRaycastProxy();
    }

    private void SyncRaycastProxy()
    {
        if (raycastProxy == null || paintablePanel == null)
            return;
        raycastProxy.transform.SetPositionAndRotation(paintablePanel.position, paintablePanel.rotation);
        raycastProxy.transform.localScale = paintablePanel.lossyScale;
    }

    private bool TryGetPaintUv(Vector2 screenPosition, int pointerId, out Vector2 paintUv)
    {
        paintUv = default;
        if (paintCamera == null || paintableCollider == null || paintManager == null || paintManager.paintTexture == null ||
            IsPointerOverInteractiveUi(screenPosition, pointerId))
            return false;

        SyncRaycastProxy();
        Physics.SyncTransforms();
        if (!paintableCollider.Raycast(paintCamera.ScreenPointToRay(screenPosition), out RaycastHit hit, maxRaycastDistance))
            return false;

        paintUv = hit.textureCoord2;
        return true;
    }

    private bool IsPointerOverInteractiveUi(Vector2 screenPosition, int pointerId)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            position = screenPosition,
            pointerId = pointerId,
            button = PointerEventData.InputButton.Left
        };
        uiRaycastResults.Clear();
        eventSystem.RaycastAll(eventData, uiRaycastResults);
        foreach (RaycastResult result in uiRaycastResults)
        {
            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.isActiveAndEnabled && selectable.interactable)
            {
                uiRaycastResults.Clear();
                return true;
            }
        }
        uiRaycastResults.Clear();
        return false;
    }

    private static bool TryGetPointerPosition(out Vector2 screenPosition, out int pointerId)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            pointerId = touchscreen.primaryTouch.touchId.ReadValue();
            return true;
        }
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            screenPosition = mouse.position.ReadValue();
            pointerId = PointerInputModule.kMouseLeftId;
            return true;
        }
        screenPosition = default;
        pointerId = PointerInputModule.kMouseLeftId;
        return false;
    }

    private void CreateBrushMaterial()
    {
        Shader shader = Shader.Find(BrushShaderName);
        if (shader == null)
        {
            Debug.LogError($"JeepneyPainter could not find '{BrushShaderName}'.", this);
            return;
        }
        brushMaterial = new Material(shader) { name = "Runtime Jeepney Paint Brush Material", hideFlags = HideFlags.DontSave };
        if (brushMaterial.shader == null || !brushMaterial.shader.isSupported)
        {
            Debug.LogError($"JeepneyPainter created '{BrushShaderName}', but it is not supported on this graphics device.", this);
            Destroy(brushMaterial);
            brushMaterial = null;
        }
    }

    private void CreateScratchPaintTexture()
    {
        if (paintManager == null || paintManager.paintTexture == null)
            return;
        ReleaseScratchPaintTexture();
        RenderTexture target = paintManager.paintTexture;
        scratchPaintTexture = new RenderTexture(target.descriptor)
        {
            name = "Runtime Jeepney Paint Scratch",
            filterMode = target.filterMode,
            wrapMode = target.wrapMode,
            anisoLevel = target.anisoLevel,
            mipMapBias = target.mipMapBias,
            hideFlags = HideFlags.DontSave
        };
        scratchPaintTexture.Create();
    }

    private bool EnsurePaintTargets()
    {
        if (paintManager == null || paintManager.paintTexture == null || brushMaterial == null)
            return false;
        RenderTexture target = paintManager.paintTexture;
        if (scratchPaintTexture == null || !scratchPaintTexture.IsCreated() || scratchPaintTexture.width != target.width ||
            scratchPaintTexture.height != target.height || scratchPaintTexture.graphicsFormat != target.graphicsFormat)
            CreateScratchPaintTexture();
        return scratchPaintTexture != null && scratchPaintTexture.IsCreated();
    }

    private void PaintStroke(Vector2 fromUv, Vector2 toUv)
    {
        if (!EnsurePaintTargets())
            return;
        brushMaterial.SetVector("_BrushFromUV", fromUv);
        brushMaterial.SetVector("_BrushToUV", toUv);
        brushMaterial.SetColor("_BrushColor", brushColor);
        brushMaterial.SetFloat("_BrushSize", brushSize);
        brushMaterial.SetFloat("_BrushOpacity", brushOpacity);
        brushMaterial.SetFloat("_EraseMode", eraseMode ? 1f : 0f);
        Graphics.Blit(paintManager.paintTexture, scratchPaintTexture, brushMaterial);
        Graphics.Blit(scratchPaintTexture, paintManager.paintTexture);
    }

    [ContextMenu("Clear Runtime Paint")]
    public void ClearRuntimePaint()
    {
        if (paintManager != null)
            paintManager.ResetPaintLayer();
        ClearHistory();
    }

    private bool CaptureUndoSnapshotForNewStroke()
    {
        if (paintManager == null || paintManager.paintTexture == null)
            return false;
        ReleaseHistory(redoHistory);
        return PushSnapshot(undoHistory, paintManager.paintTexture, "Undo");
    }

    private bool PushSnapshot(List<RenderTexture> history, RenderTexture source, string historyName)
    {
        RenderTexture snapshot = CreateHistoryTexture(source, historyName);
        if (snapshot == null)
            return false;
        Graphics.Blit(source, snapshot);
        history.Add(snapshot);
        while (history.Count > Mathf.Max(1, maxHistoryStates))
        {
            RenderTexture oldest = history[0];
            history.RemoveAt(0);
            ReleaseRenderTexture(oldest);
        }
        return true;
    }

    private static RenderTexture CreateHistoryTexture(RenderTexture source, string historyName)
    {
        if (!source.IsCreated())
            source.Create();
        RenderTexture snapshot = new RenderTexture(source.descriptor)
        {
            name = $"Jeepney Paint {historyName} Snapshot",
            filterMode = source.filterMode,
            wrapMode = source.wrapMode,
            anisoLevel = source.anisoLevel,
            mipMapBias = source.mipMapBias,
            hideFlags = HideFlags.DontSave
        };
        snapshot.Create();
        if (snapshot.IsCreated())
            return snapshot;
        ReleaseRenderTexture(snapshot);
        return null;
    }

    private void RestoreAndRemoveLatest(List<RenderTexture> history)
    {
        int index = history.Count - 1;
        RenderTexture snapshot = history[index];
        history.RemoveAt(index);
        Graphics.Blit(snapshot, paintManager.paintTexture);
        ReleaseRenderTexture(snapshot);
    }

    private static void ReleaseHistory(List<RenderTexture> history)
    {
        foreach (RenderTexture snapshot in history)
            ReleaseRenderTexture(snapshot);
        history.Clear();
    }

    private void EndCurrentStroke()
    {
        hasPreviousUv = false;
        strokeActive = false;
    }

    private void ReleaseScratchPaintTexture()
    {
        if (scratchPaintTexture == null)
            return;
        scratchPaintTexture.Release();
        Destroy(scratchPaintTexture);
        scratchPaintTexture = null;
    }

    private static void ReleaseRenderTexture(RenderTexture texture)
    {
        if (texture == null)
            return;
        texture.Release();
        Destroy(texture);
    }

    private void OnDestroy()
    {
        if (raycastProxy != null)
            Destroy(raycastProxy);
        ReleaseScratchPaintTexture();
        ReleaseHistory(undoHistory);
        ReleaseHistory(redoHistory);
        if (brushMaterial != null)
            Destroy(brushMaterial);
    }
}
