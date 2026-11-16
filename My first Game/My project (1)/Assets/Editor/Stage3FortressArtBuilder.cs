using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage3FortressArtBuilder
{
    private const string Art = "Assets/Art/RuinedFortress/";
    private const string BackgroundName = "Stage3_UpperTowerBackground";
    private static readonly string[] TerrainNames =
    {
        "EntryShelf", "Climb_A", "Climb_B", "Climb_C", "CombatShelf_A", "CombatShelf_B",
        "Summit", "CombatRunway", "ArrowRunway", "RestLanding", "DropRunway",
        "CombatLanding", "CombatShelf", "UpperSummit", "LongApproach"
    };

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform[] courses = all.Where(t => t.name == "Stage3_ExplorationCourse").ToArray();
        if (courses.Length != 1)
        {
            EditorUtility.DisplayDialog("Stage 3 art", "Expected one Stage3_ExplorationCourse. No changes made.", "OK");
            return;
        }
        Transform course = courses[0];
        SpriteRenderer[] targets = course.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.sprite != null &&
                r.transform.parent != null && r.transform.parent.parent == course &&
                TerrainNames.Contains(r.name) &&
                r.GetComponent<BoxCollider2D>() != null && r.GetComponent<BoxCollider2D>().enabled &&
                !r.GetComponent<BoxCollider2D>().isTrigger && r.GetComponent<BoxCollider2D>().attachedRigidbody == null)
            .ToArray();
        if (targets.Length == 0) throw new InvalidOperationException("No Stage 3 terrain blocks found.");
        Shader shader = Shader.Find("Fortress/Damp Stone");
        Texture2D stone = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "FortressStone.png");
        if (shader == null || stone == null) throw new InvalidOperationException("Stone assets are missing.");
        Sprite tower = ImportTower();
        if (tower == null) throw new InvalidOperationException("Tower background is missing.");
        Bounds area = targets[0].GetComponent<BoxCollider2D>().bounds;
        foreach (SpriteRenderer r in targets.Skip(1)) area.Encapsulate(r.GetComponent<BoxCollider2D>().bounds);
        Camera camera = Camera.main;
        float padding = camera != null && camera.orthographic ? Mathf.Max(8, camera.orthographicSize * 2) : 12;
        area.Expand(new Vector3(padding * 2, padding * 2, 0));
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Stage 3 tower visuals only");
        try
        {
            string path = Art + "Stage3WeatheredStone.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                // Separate asset: never retint the Stage 1 or Stage 2 shared material.
                material = new Material(shader);
                material.SetTexture("_StoneTex", stone);
                material.SetFloat("_TileWorldSize", 4);
                material.SetColor("_StoneTint", new Color(0.98f, 0.95f, 0.94f, 1));
                material.SetFloat("_MossAmount", 0.08f);
                AssetDatabase.CreateAsset(material, path);
            }
            foreach (SpriteRenderer renderer in targets)
            {
                // Preserve the sprite, draw mode, size, transform and every collider.
                Undo.RecordObject(renderer, "Apply Stage 3 stone material");
                renderer.sharedMaterial = material;
                renderer.color = Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            Transform existing = all.FirstOrDefault(t => t.name == BackgroundName);
            if (existing == null)
            {
                GameObject background = new GameObject(BackgroundName);
                Undo.RegisterCreatedObjectUndo(background, "Create tower background");
                background.transform.position = new Vector3(area.center.x, area.center.y, 6);
                // Uniform scale keeps the artwork's proportions and covers the camera margins.
                float scale = Mathf.Max(area.size.x / tower.bounds.size.x, area.size.y / tower.bounds.size.y);
                background.transform.localScale = new Vector3(scale, scale, 1);
                SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
                renderer.sprite = tower;
                renderer.color = new Color(0.60f, 0.66f, 0.74f, 1);
                SpriteRenderer courtyard = all.Where(t => t.name == "DistantFortress_Background")
                    .Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault(r => r != null);
                renderer.sortingLayerID = courtyard != null ? courtyard.sortingLayerID : 0;
                renderer.sortingOrder = courtyard != null ? courtyard.sortingOrder - 10 : -110;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("Stage 3 tower art: " + string.Join(", ", targets.Select(r => r.name)) +
                ". No terrain geometry, physics, traps, checkpoints or enemy markers changed.", course);
            EditorUtility.DisplayDialog("Stage 3 art",
                $"Applied tower stone to {targets.Length} blocks. Background added only if missing. Existing backgrounds and all terrain sizes were preserved.", "OK");
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static Sprite ImportTower()
    {
        string path = Art + "FortressUpperTower.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static void BlendEntrance()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        SpriteRenderer[] renderers = scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<SpriteRenderer>()).ToArray();
        SpriteRenderer courtyard = renderers.FirstOrDefault(r => r.name == "DistantFortress_Background");
        SpriteRenderer tower = renderers.FirstOrDefault(r => r.name == BackgroundName);
        Shader shader = Shader.Find("Fortress/Upper Background Fade");
        if (courtyard == null || tower == null || shader == null)
            throw new InvalidOperationException("Active courtyard, tower background or fade shader is missing.");
        float end = courtyard.bounds.max.y;
        float width = Mathf.Min(8, courtyard.bounds.size.y * 0.45f);
        float start = end - width;
        Bounds behind = tower.bounds;
        if (behind.min.y > start || behind.max.y < end ||
            behind.min.x > courtyard.bounds.min.x || behind.max.x < courtyard.bounds.max.x)
        {
            EditorUtility.DisplayDialog("Background blend", "Tower background does not cover the courtyard transition band. No changes made.", "OK");
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Blend Stage 1 into upper tower");
        try
        {
            string path = Art + "Stage1UpperFade.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            Undo.RecordObject(material, "Set vertical background fade");
            material.shader = shader;
            material.SetFloat("_FadeStartY", start);
            material.SetFloat("_FadeEndY", end);
            EditorUtility.SetDirty(material);
            Undo.RecordObject(courtyard, "Fade courtyard upper edge");
            courtyard.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(courtyard);
            // The Stage 2 bridge also samples the courtyard; give that sample the same fade.
            SpriteRenderer bridge = renderers.FirstOrDefault(r => r.name == "Stage2_FortressBackground");
            if (bridge != null && bridge.sharedMaterial != null &&
                bridge.sharedMaterial.shader.name == "Fortress/Background Blend")
            {
                Material bridgeMaterial = bridge.sharedMaterial;
                Undo.RecordObject(bridgeMaterial, "Match courtyard fade in bridge");
                bridgeMaterial.SetVector("_UpperFade", new Vector4(start, end, 1, 0));
                EditorUtility.SetDirty(bridgeMaterial);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log($"Stage 1-3 background blend applied across Y={start:F2}..{end:F2}. Background materials only.");
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
