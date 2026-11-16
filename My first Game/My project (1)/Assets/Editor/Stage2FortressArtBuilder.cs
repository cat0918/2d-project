using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2FortressArtBuilder
{
    private const string ArtPath = "Assets/Art/RuinedFortress/";
    private static readonly string[] TerrainNames =
    {
        "SafeEntry", "ArrowRunway", "SafeFloor", "Landing_A", "Landing_B", "Landing_C",
        "Step_A", "Step_B", "Step_C", "Step_D", "ShrineFloor", "EndWall"
    };

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform[] roots = all.Where(t => t.name == "Stage2_TrapCourse").ToArray();
        Transform[] stageGroups = all.Where(t => t.name == "Stage2" || t.name == "Stage_02").ToArray();
        if (roots.Length != 1)
        {
            EditorUtility.DisplayDialog("Stage 2 art", "Expected one Stage2_TrapCourse. No changes made.", "OK");
            return;
        }
        SpriteRenderer[] targets = all.Where(t =>
                (t.IsChildOf(roots[0]) && TerrainNames.Contains(t.name)) ||
                t.name == "Floor_Stage2_Start" ||
                (string.Equals(t.name, "Stage2_Platform", StringComparison.OrdinalIgnoreCase) &&
                 (t.IsChildOf(roots[0]) || stageGroups.Any(stage => t.IsChildOf(stage)))))
            .Select(t => t.GetComponent<SpriteRenderer>())
            .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy && r.sprite != null &&
                r.GetComponent<BoxCollider2D>() != null && r.GetComponent<BoxCollider2D>().enabled &&
                !r.GetComponent<BoxCollider2D>().isTrigger && r.GetComponent<BoxCollider2D>().attachedRigidbody == null)
            .ToArray();
        if (targets.Length == 0) return;
        Shader shader = Shader.Find("Fortress/Damp Stone");
        Texture2D stone = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath + "FortressStone.png");
        if (shader == null || stone == null) throw new InvalidOperationException("Damp stone shader or texture missing.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Stage 2 damp stone material only");
        try
        {
            string path = ArtPath + "Stage2DampStone.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                material.SetTexture("_StoneTex", stone);
                material.SetFloat("_TileWorldSize", 4);
                material.SetColor("_StoneTint", new Color(0.78f, 0.88f, 0.80f, 1));
                material.SetFloat("_MossAmount", 0.65f);
                AssetDatabase.CreateAsset(material, path);
            }
            foreach (SpriteRenderer renderer in targets)
            {
                // Match the tested Stage 1 approach: never modify geometry or physics.
                Undo.RecordObject(renderer, "Apply Stage 2 stone material");
                renderer.sharedMaterial = material;
                renderer.color = Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("Stage 2 damp stone (material/color only): " + string.Join(", ", targets.Select(r => r.name)));
            EditorUtility.DisplayDialog("Stage 2 art", $"Applied damp stone to {targets.Length} terrain blocks. Sizes, colliders, traps, checkpoints and other stages were not changed.", "OK");
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }
}
