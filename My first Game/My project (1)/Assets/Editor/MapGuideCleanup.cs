using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapGuideCleanup
{
    [MenuItem("Tools/Scene Cleanup/Remove Map Guide Text")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;
        var texts = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<TextMesh>(true)).ToArray();
        var checkpoints = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Stage2Checkpoint>(true))
            .ToArray();
        var fixedLabels = checkpoints.Select(checkpoint => checkpoint.statusLabel).ToArray();
        var guides = texts.Where(text => text.name.StartsWith("Guide_", System.StringComparison.Ordinal)
            || fixedLabels.Contains(text) || text.name == "WingedBootPrompt"
            || text.GetComponentInParent<DoubleJumpPickup>() != null).ToArray();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Remove map authoring text");
        foreach (Stage2Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint.statusLabel == null || !guides.Contains(checkpoint.statusLabel)) continue;
            Undo.RecordObject(checkpoint, "Remove fixed checkpoint label reference");
            checkpoint.statusLabel = null;
            PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint);
        }
        foreach (TextMesh text in guides)
        {
            // Keep any unrelated components or children a designer may have added.
            bool labelOnly = text.transform.childCount == 0 && text.GetComponents<Component>()
                .All(component => component is Transform || component is TextMesh || component is MeshRenderer);
            if (labelOnly) Undo.DestroyObjectImmediate(text.gameObject);
            else
            {
                MeshRenderer renderer = text.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Undo.RecordObject(renderer, "Hide guide text renderer");
                    renderer.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
        }
        Undo.CollapseUndoOperations(group);
        if (guides.Length > 0) EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Cleaned {guides.Length} fixed map labels. Player proximity prompts and HUD were preserved.");
        EditorUtility.DisplayDialog("Map Guide Cleanup",
            $"Cleaned {guides.Length} fixed labels, including checkpoint/item text. Player proximity prompts and HUD were preserved. Save the scene. Ctrl+Z can undo it.", "OK");
    }
}
