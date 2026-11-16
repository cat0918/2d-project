using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FortressArtBuilder
{
    private const string ArtPath = "Assets/Art/RuinedFortress/";
    private static bool IsTerrain(Transform t) => t != null &&
        (t.name == "Floor_Horizon" || t.name == "Wall_Left_Horizon");

    public static void ApplyStage1Platforms()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] stages = scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "Stage1").ToArray();
        if (stages.Length != 1)
        {
            EditorUtility.DisplayDialog("Stage 1 stone", "Expected exactly one Stage1 group in the active scene.", "OK");
            return;
        }
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "FortressStone.mat");
        if (material == null || material.shader == null || material.shader.name != "Fortress/World Stone")
            throw new InvalidOperationException("The tested FortressStone material is missing. No terrain was changed.");

        // Only the known Stage 1 terrain groups qualify; actors and Stage 2/3 are excluded.
        string[] groups = { "RoomVertical", "Connection12", "Route_to_Stage3" };
        SpriteRenderer[] targets = stages[0].GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.gameObject.activeInHierarchy && r.enabled && r.sprite != null &&
                r.transform.parent != null && r.transform.parent.parent == stages[0] &&
                groups.Contains(r.transform.parent.name) &&
                (r.name.StartsWith("Platform", StringComparison.Ordinal) ||
                 r.name.StartsWith("Floor_", StringComparison.Ordinal) ||
                 r.name == "Landing1" || r.name == "DoubleJumpGate") &&
                r.GetComponent<BoxCollider2D>() != null &&
                r.GetComponent<BoxCollider2D>().enabled && !r.GetComponent<BoxCollider2D>().isTrigger &&
                r.GetComponent<Rigidbody2D>() == null).ToArray();
        if (targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Stage 1 stone", "No matching Stage 1 platforms were found. No changes made.", "OK");
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Stage 1 platform stone material only");
        try
        {
            foreach (SpriteRenderer renderer in targets)
            {
                // Do not change sprite, draw mode, size, transform, or collider properties.
                Undo.RecordObject(renderer, "Apply platform stone material");
                renderer.sharedMaterial = material;
                renderer.color = Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("Stage 1 stone applied (material/color only): " + string.Join(", ", targets.Select(r => r.name)));
            EditorUtility.DisplayDialog("Stage 1 stone", $"Applied stone to {targets.Length} platforms. Geometry and collision settings were not modified. Stage 2/3 and the existing start floor were excluded.", "OK");
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    // Kept for recovery history, but hidden from menus to avoid overwriting manual sizing.
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) return;
        if (!EditorUtility.DisplayDialog("Repair fortress terrain",
            "Restore ONLY Floor_Horizon and Wall_Left_Horizon transforms, sprites and box dimensions from the saved scene, then apply the stone material. Other objects and the background stay unchanged. This can be undone.", "Repair", "Cancel")) return;
        Scene saved = EditorSceneManager.OpenPreviewScene(scene.path);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair fortress terrain from saved scene");
        try
        {
            Transform[] source = saved.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Where(IsTerrain).ToArray();
            Transform[] targets = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Where(IsTerrain).ToArray();
            if (source.Length != 2 || targets.Length != 2) throw new InvalidOperationException("Expected exactly two matching terrain blocks.");
            foreach (Transform original in source)
            {
                SpriteRenderer sr = original.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null || sr.drawMode != SpriteDrawMode.Simple ||
                    AssetDatabase.GetAssetPath(sr.sprite).StartsWith(ArtPath))
                    throw new InvalidOperationException("Saved scene no longer contains the original blocks. No changes were made.");
            }
            Material material = GetStoneMaterial();
            foreach (Transform target in targets)
            {
                Transform original = source.Single(t => t.name == target.name);
                BoxCollider2D box = target.GetComponent<BoxCollider2D>();
                BoxCollider2D originalBox = original.GetComponent<BoxCollider2D>();
                SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
                SpriteRenderer originalRenderer = original.GetComponent<SpriteRenderer>();
                Undo.RecordObjects(new UnityEngine.Object[] { target, box, renderer }, "Restore original terrain dimensions");
                target.localPosition = original.localPosition;
                target.localRotation = original.localRotation;
                target.localScale = original.localScale;
                box.autoTiling = false;
                // Preserve original square geometry; stone is sampled by the material.
                renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sprite = originalRenderer.sprite;
                renderer.size = originalRenderer.size;
                renderer.sharedMaterial = material;
                renderer.color = Color.white;
                renderer.sortingLayerID = originalRenderer.sortingLayerID;
                renderer.sortingOrder = originalRenderer.sortingOrder;
                box.size = originalBox.size;
                box.offset = originalBox.offset;
                foreach (UnityEngine.Object obj in new UnityEngine.Object[] { target, box, renderer })
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                Debug.Log($"Fortress repair: {target.name}, scale {target.localScale}, collider size {box.size}.", target);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = targets.Single(t => t.name == "Floor_Horizon").gameObject;
            SceneView.RepaintAll();
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(saved);
            Undo.CollapseUndoOperations(group);
        }
    }

    private static Material GetStoneMaterial()
    {
        Shader shader = Shader.Find("Fortress/World Stone");
        Texture2D stone = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath + "FortressStone.png");
        if (shader == null || stone == null) throw new InvalidOperationException("Stone shader or texture is missing.");
        string path = ArtPath + "FortressStone.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        Undo.RecordObject(material, "Configure stone material");
        material.shader = shader;
        material.SetTexture("_StoneTex", stone);
        material.SetFloat("_TileWorldSize", 4);
        EditorUtility.SetDirty(material);
        return material;
    }
}
