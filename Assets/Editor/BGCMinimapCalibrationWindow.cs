using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BGCMinimapCalibrationWindow : EditorWindow
{
    private static readonly string[] PairNames = { "landmark1", "landmark2", "landmark3" };
    private static readonly string[] PointNames = { "A", "B", "C" };
    private static readonly Color[] PointColors = { Color.yellow, Color.cyan, Color.magenta };

    [SerializeField] private BGCMinimapController controller;
    [SerializeField] private Sprite mapSprite;
    [SerializeField] private Transform[] landmarks = new Transform[3];
    [SerializeField] private Vector2[] imagePoints = new Vector2[3];
    [SerializeField] private bool[] pointAssigned = new bool[3];
    [SerializeField] private int selectedPoint = -1;
    private Texture2D sourceTexture;
    private string imageError;
    private string feedback;
    private Vector2 scroll;

    [MenuItem("Tools/Arangkada/Minimap/Calibrate 3 Points")]
    private static void Open()
    {
        GetWindow<BGCMinimapCalibrationWindow>("BGC Calibration");
    }

    private void OnEnable()
    {
        minSize = new Vector2(480f, 620f);
        LoadSourceTexture();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Three-point minimap calibration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign three recognizable scene landmarks, then select and click each matching location on the complete PNG. Marker numbers 1, 2, 3 correspond to A, B, C.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        controller = (BGCMinimapController)EditorGUILayout.ObjectField("Controller", controller, typeof(BGCMinimapController), true);
        if (EditorGUI.EndChangeCheck())
            feedback = null;

        EditorGUI.BeginChangeCheck();
        mapSprite = (Sprite)EditorGUILayout.ObjectField("BGC minimap Sprite", mapSprite, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            // Points belong to a specific image; never reuse them on a different one.
            Array.Clear(pointAssigned, 0, pointAssigned.Length);
            selectedPoint = -1;
            feedback = null;
            LoadSourceTexture();
        }

        for (int i = 0; i < 3; i++)
        {
            EditorGUILayout.Space(4f);
            EditorGUI.BeginChangeCheck();
            landmarks[i] = (Transform)EditorGUILayout.ObjectField("Landmark " + PointNames[i], landmarks[i], typeof(Transform), true);
            if (EditorGUI.EndChangeCheck())
            {
                pointAssigned[i] = false;
                feedback = null;
            }

            if (landmarks[i] != null)
            {
                Vector3 world = landmarks[i].position;
                EditorGUILayout.LabelField("World X / Z", world.x.ToString("G9") + " / " + world.z.ToString("G9"));
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(sourceTexture == null))
            {
                if (GUILayout.Button("Select Point " + PointNames[i], GUILayout.Width(130f)))
                {
                    selectedPoint = i;
                    feedback = null;
                }
            }
            EditorGUILayout.LabelField(pointAssigned[i]
                ? "U: " + imagePoints[i].x.ToString("F6") + "   V: " + imagePoints[i].y.ToString("F6")
                : "Image point not selected");
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(selectedPoint >= 0
            ? "Click the PNG for point " + PointNames[selectedPoint] + "."
            : "Select a point button to record or replace its location.");

        if (sourceTexture != null)
            DrawMap();
        else
            EditorGUILayout.HelpBox(imageError ?? "Assign the existing BGC_minimap Sprite.", MessageType.Warning);

        string validationError;
        bool valid = ValidateInputs(out validationError);
        if (!valid)
            EditorGUILayout.HelpBox(validationError, MessageType.Warning);

        using (new EditorGUI.DisabledScope(!valid))
        {
            if (GUILayout.Button("Apply Calibration", GUILayout.Height(30f)))
                ApplyCalibration();
        }

        if (!string.IsNullOrEmpty(feedback))
            EditorGUILayout.HelpBox(feedback, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    private void LoadSourceTexture()
    {
        sourceTexture = null;
        imageError = null;
        if (mapSprite == null)
            return;

        // Load the original texture asset, not an atlas or a tightly cropped sprite preview.
        string path = AssetDatabase.GetAssetPath(mapSprite);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            imageError = "Assign a Sprite from the original minimap PNG asset.";
            return;
        }

        Rect spriteRect = mapSprite.rect;
        if (spriteRect.x != 0f || spriteRect.y != 0f ||
            spriteRect.width != texture.width || spriteRect.height != texture.height)
        {
            imageError = "This Sprite does not cover the complete source PNG. Full-PNG coordinates would not match the controller's sprite coordinates. Assign the full-image Sprite; no assets have been changed.";
            return;
        }
        sourceTexture = texture;
    }

    private void DrawMap()
    {
        Rect available = GUILayoutUtility.GetRect(1f, Mathf.Max(200f, position.height * 0.4f), GUILayout.ExpandWidth(true));
        float scale = Mathf.Min(available.width / sourceTexture.width, available.height / sourceTexture.height);
        Vector2 size = new Vector2(sourceTexture.width * scale, sourceTexture.height * scale);
        Rect imageRect = new Rect(available.center - size * 0.5f, size);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(available, new Color(0.12f, 0.12f, 0.12f));
            // The rectangle already matches the full texture's aspect ratio.
            GUI.DrawTexture(imageRect, sourceTexture, ScaleMode.StretchToFill, true);
        }

        Event current = Event.current;
        if (selectedPoint >= 0 && current.type == EventType.MouseDown && current.button == 0 && imageRect.Contains(current.mousePosition))
        {
            imagePoints[selectedPoint] = new Vector2(
                Mathf.Clamp01((current.mousePosition.x - imageRect.xMin) / imageRect.width),
                Mathf.Clamp01(1f - (current.mousePosition.y - imageRect.yMin) / imageRect.height));
            pointAssigned[selectedPoint] = true;
            selectedPoint = -1;
            feedback = null;
            current.Use();
            Repaint();
        }

        if (Event.current.type != EventType.Repaint)
            return;

        GUIStyle numberStyle = new GUIStyle(EditorStyles.boldLabel);
        numberStyle.alignment = TextAnchor.MiddleCenter;
        numberStyle.normal.textColor = Color.black;
        for (int i = 0; i < 3; i++)
        {
            if (!pointAssigned[i])
                continue;
            Vector2 point = new Vector2(imageRect.xMin + imagePoints[i].x * imageRect.width,
                                        imageRect.yMax - imagePoints[i].y * imageRect.height);
            Rect marker = new Rect(point.x - 10f, point.y - 10f, 20f, 20f);
            EditorGUI.DrawRect(new Rect(marker.x - 1f, marker.y - 1f, 22f, 22f), Color.black);
            EditorGUI.DrawRect(marker, PointColors[i]);
            GUI.Label(marker, (i + 1).ToString(), numberStyle);
        }
    }

    private bool ValidateInputs(out string error)
    {
        error = null;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            error = "Apply calibration in Edit Mode only.";
        else if (controller == null || EditorUtility.IsPersistent(controller) ||
                 !controller.gameObject.scene.IsValid() || !controller.gameObject.scene.isLoaded ||
                 EditorSceneManager.IsPreviewScene(controller.gameObject.scene))
            error = "Assign the existing controller from a loaded scene, not a prefab asset or Prefab Mode.";
        else if (mapSprite == null || sourceTexture == null)
            error = imageError ?? "Assign the full-image BGC_minimap Sprite.";
        if (error != null)
            return false;

        Vector2[] worldPoints = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            Transform landmark = landmarks[i];
            if (landmark == null || EditorUtility.IsPersistent(landmark) ||
                !landmark.gameObject.scene.IsValid() || !landmark.gameObject.scene.isLoaded ||
                EditorSceneManager.IsPreviewScene(landmark.gameObject.scene) || !pointAssigned[i])
            {
                error = "Assign a scene Transform and click an image point for landmark " + PointNames[i] + ".";
                return false;
            }
            Vector3 position = landmark.position;
            worldPoints[i] = new Vector2(position.x, position.z);
            Vector2 uv = imagePoints[i];
            if (!Finite(worldPoints[i]) || !Finite(uv) || uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            {
                error = "All world X/Z values must be finite and image U/V values must be finite and within [0, 1].";
                return false;
            }
        }

        if (!NonCollinear(worldPoints) || !NonCollinear(imagePoints))
        {
            error = "Choose three distinct, non-collinear points in both world X/Z and the PNG. Avoid nearly straight lines.";
            return false;
        }

        SerializedObject serialized = new SerializedObject(controller);
        return ValidateFields(serialized, out error);
    }

    private static bool ValidateFields(SerializedObject serialized, out string error)
    {
        serialized.Update();
        foreach (string name in PairNames)
        {
            SerializedProperty pair = serialized.FindProperty(name);
            SerializedProperty world = pair != null ? pair.FindPropertyRelative("worldXZ") : null;
            SerializedProperty uv = pair != null ? pair.FindPropertyRelative("imageUV") : null;
            if (world == null || uv == null || !world.editable || !uv.editable ||
                world.propertyType != SerializedPropertyType.Vector2 || uv.propertyType != SerializedPropertyType.Vector2)
            {
                error = "Cannot safely write " + name + ".worldXZ/imageUV as Vector2 fields. Calibration has not been applied; the runtime controller will not be modified.";
                return false;
            }
        }
        error = null;
        return true;
    }

    private void ApplyCalibration()
    {
        string error;
        if (!ValidateInputs(out error))
        {
            feedback = error;
            return;
        }
        SerializedObject serialized = new SerializedObject(controller);
        if (!ValidateFields(serialized, out error))
        {
            feedback = error;
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Apply BGC minimap calibration");
        int undoGroup = Undo.GetCurrentGroup();
        for (int i = 0; i < 3; i++)
        {
            Vector3 world = landmarks[i].position;
            SerializedProperty pair = serialized.FindProperty(PairNames[i]);
            pair.FindPropertyRelative("worldXZ").vector2Value = new Vector2(world.x, world.z);
            pair.FindPropertyRelative("imageUV").vector2Value = imagePoints[i];
        }
        // SerializedObject records Undo and prefab-instance overrides for these fields.
        bool changed = serialized.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(undoGroup);
        if (changed)
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        feedback = changed
            ? "Calibration applied. Undo reverses this change. The scene has not been saved."
            : "These calibration values are already applied. The scene has not been saved.";
    }

    private static bool Finite(Vector2 point)
    {
        return !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
               !float.IsNaN(point.y) && !float.IsInfinity(point.y);
    }

    private static bool NonCollinear(Vector2[] points)
    {
        // Validate geometry only; the runtime controller owns the affine conversion.
        double ax = (double)points[1].x - points[0].x;
        double ay = (double)points[1].y - points[0].y;
        double bx = (double)points[2].x - points[0].x;
        double by = (double)points[2].y - points[0].y;
        double lengthProduct = Math.Sqrt((ax * ax + ay * ay) * (bx * bx + by * by));
        return lengthProduct > 0.0 && Math.Abs(ax * by - ay * bx) > lengthProduct * 0.000001;
    }
}
