using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JeepneyArcade.EditorTools
{
    public sealed class CityColliderWindow : EditorWindow
    {
        CityColliderBuilder.Result result;

        [MenuItem("Tools/City Map/Collider Builder")]
        public static void Open() => GetWindow<CityColliderWindow>("City Colliders");
        void OnSelectionChange() => Repaint();

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Select city objects or their parents, then generate. Buildings receive rotated child boxes fitted to their geometry; rerunning replaces generated building boxes. Streetlights, tree trunks, barriers and other authored colliders are preserved.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode ||
                !Selection.gameObjects.Any(g => g && !EditorUtility.IsPersistent(g))))
            {
                if (GUILayout.Button("Generate Colliders on Selection", GUILayout.Height(40)))
                {
                    result = CityColliderBuilder.Generate(Selection.gameObjects);
                    Debug.Log("City colliders: " + result.Summary);
                }
            }
            EditorGUILayout.LabelField("Undo (Ctrl+Z) reverses the batch. Save the scene when satisfied.", EditorStyles.wordWrappedLabel);
            if (result == null) return;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(result.Summary, MessageType.Info);
            if (result.manual.Count > 0 && GUILayout.Button("Select skipped shapes for manual correction"))
                Selection.objects = result.manual.Where(g => g).Cast<Object>().ToArray();
        }
    }
}

