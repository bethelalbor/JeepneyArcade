
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ArangkadaSelectedLOD
{
    private const float CullThreshold = 0.02f;
    private const int MaxPerOperation = 100;

    [MenuItem("Tools/Arangkada/LOD/Apply to Selected Meshes")]
    private static void Apply()
    {
        GameObject[] targets = Selection.gameObjects
            .Where(go => go.scene.IsValid())
            .Where(go => go.GetComponent<MeshRenderer>() != null)
            .Where(go => go.GetComponentInParent<LODGroup>() == null)
            .Distinct()
            .ToArray();

        if (targets.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No eligible meshes",
                "Select individual mesh objects without existing LOD Groups.",
                "OK");
            return;
        }

        if (targets.Length > MaxPerOperation)
        {
            EditorUtility.DisplayDialog(
                "Selection too large",
                "Select no more than 100 objects per operation.",
                "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Apply LOD",
            $"Add LOD 0 -> Culled to {targets.Length} selected objects?\n" +
            $"Cull threshold: {CullThreshold * 100}%\n" +
            "Only LODGroup components will be added.",
            "Apply",
            "Cancel");

        if (!confirmed) return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Arangkada LOD");

        foreach (GameObject go in targets)
        {
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();

            LODGroup group = Undo.AddComponent<LODGroup>(go);

            Undo.RecordObject(group, "Configure LOD");
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[]
            {
                new LOD(CullThreshold, new Renderer[] { renderer })
            });

            group.RecalculateBounds();
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"Applied LOD to {targets.Length} selected meshes.");
    }
}
