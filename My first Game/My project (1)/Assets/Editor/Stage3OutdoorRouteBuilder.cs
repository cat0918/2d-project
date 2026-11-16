using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage3OutdoorRouteBuilder
{
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        SpriteRenderer courtyard = all.Where(t => t.name == "DistantFortress_Background")
            .Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault(r => r != null);
        SpriteRenderer tower = all.Where(t => t.name == "Stage3_UpperTowerBackground")
            .Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault(r => r != null);
        Transform course = all.FirstOrDefault(t => t.name == "Stage3_ExplorationCourse");
        Transform runway = course != null ? course.Find("03_Long_Gallery/CombatRunway") : null;
        Collider2D gallery = runway != null ? runway.GetComponent<Collider2D>() : null;
        Shader shader = Shader.Find("Fortress/Outdoor Ascent");
        if (courtyard == null || tower == null || gallery == null || shader == null ||
            courtyard.sprite == null || tower.sprite == null)
            throw new InvalidOperationException("Expected courtyard/tower backgrounds and Stage 3 CombatRunway.");
        Bounds outside = courtyard.bounds;
        Bounds inside = tower.bounds;
        // Walking left from the summit into the first eight units of the gallery enters indoors.
        float entranceX = gallery.bounds.max.x;
        float transitionWidth = Mathf.Min(8, gallery.bounds.size.x * 0.7f);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Outdoor climb then indoor gallery");
        try
        {
            const string path = "Assets/Art/RuinedFortress/Stage3OutdoorRoute.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            Undo.RecordObject(material, "Set route-based background");
            material.shader = shader;
            material.SetTexture("_Outside", courtyard.sprite.texture);
            material.SetTexture("_Inside", tower.sprite.texture);
            material.SetColor("_OutsideTint", courtyard.color);
            material.SetColor("_InsideTint", tower.color);
            material.SetVector("_OutsideRect", new Vector4(outside.min.x, outside.min.y, outside.size.x, outside.size.y));
            material.SetVector("_InsideRect", new Vector4(inside.min.x, inside.min.y, inside.size.x, inside.size.y));
            material.SetVector("_Entrance", new Vector4(entranceX - transitionWidth, entranceX,
                gallery.bounds.max.y - 8, gallery.bounds.max.y - 2));
            EditorUtility.SetDirty(material);
            Undo.RecordObject(tower, "Use outdoor ascent background");
            tower.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tower);
            // Retire the old height-only fade. Stage 1 must remain outdoors.
            Material oldFade = courtyard.sharedMaterial;
            if (oldFade != null && oldFade.shader.name == "Fortress/Upper Background Fade")
            {
                Undo.RecordObject(oldFade, "Disable old upper fade");
                oldFade.SetFloat("_FadeStartY", outside.max.y + 1);
                oldFade.SetFloat("_FadeEndY", outside.max.y + 2);
                EditorUtility.SetDirty(oldFade);
            }
            SpriteRenderer bridge = all.Where(t => t.name == "Stage2_FortressBackground")
                .Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault(r => r != null);
            if (bridge != null && bridge.sharedMaterial != null && bridge.sharedMaterial.HasProperty("_UpperFade"))
            {
                Undo.RecordObject(bridge.sharedMaterial, "Disable old bridge upper fade");
                bridge.sharedMaterial.SetVector("_UpperFade", Vector4.zero);
                EditorUtility.SetDirty(bridge.sharedMaterial);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log($"Outdoor ascent set. Enter the indoor gallery walking left from X={entranceX:F2}. Background materials only.", tower);
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
