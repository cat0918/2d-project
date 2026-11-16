using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossRoomArtBuilder
{
    private const string Art = "Assets/Art/RuinedFortress/";
    private const string BackgroundName = "BossRoom_HallBackground";
    private static readonly string[] Paths =
    {
        "01_Entrance/EntryCorridor", "02_Arena/ArenaFloor", "02_Arena/BackWall",
        "02_Arena/Ceiling", "02_Arena/EntranceLintel", "04_Encounter/EntranceGate"
    };

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform[] rooms = all.Where(t => t.name == "BossRoom").ToArray();
        if (rooms.Length != 1) throw new InvalidOperationException("Expected one BossRoom.");
        Transform room = rooms[0];
        Collider2D floor = room.Find("02_Arena/ArenaFloor")?.GetComponent<Collider2D>();
        Collider2D ceiling = room.Find("02_Arena/Ceiling")?.GetComponent<Collider2D>();
        Collider2D corridor = room.Find("01_Entrance/EntryCorridor")?.GetComponent<Collider2D>();
        if (floor == null || ceiling == null || corridor == null ||
            !floor.gameObject.activeInHierarchy || !floor.enabled)
            throw new InvalidOperationException("Active boss arena geometry is required.");
        SpriteRenderer[] targets = Paths.Select(p => room.Find(p)).Where(t => t != null)
            .Select(t => t.GetComponent<SpriteRenderer>())
            .Where(r => r != null && r.sprite != null && r.GetComponent<BoxCollider2D>() != null &&
                !r.GetComponent<BoxCollider2D>().isTrigger).ToArray();
        Shader stoneShader = Shader.Find("Fortress/Damp Stone");
        Shader backgroundShader = Shader.Find("Fortress/Boss Backdrop");
        Texture2D stone = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "FortressStone.png");
        Sprite hall = ImportHall();
        if (stoneShader == null || backgroundShader == null || stone == null || hall == null)
            throw new InvalidOperationException("Boss art assets are missing or still importing.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply boss room visual materials");
        try
        {
            string path = Art + "BossRoomStone.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(stoneShader);
                material.SetTexture("_StoneTex", stone);
                material.SetFloat("_TileWorldSize", 4);
                material.SetColor("_StoneTint", new Color(0.78f, 0.82f, 0.90f, 1));
                material.SetFloat("_MossAmount", 0.03f);
                AssetDatabase.CreateAsset(material, path);
            }
            foreach (SpriteRenderer renderer in targets)
            {
                // Includes the inactive entrance gate without changing its enabled state.
                Undo.RecordObject(renderer, "Apply boss terrain material");
                renderer.sharedMaterial = material;
                renderer.color = Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            if (!all.Any(t => t.name == BackgroundName))
            {
                string backgroundPath = Art + "BossHallBackdrop.mat";
                Material backdrop = AssetDatabase.LoadAssetAtPath<Material>(backgroundPath);
                if (backdrop == null)
                {
                    backdrop = new Material(backgroundShader);
                    AssetDatabase.CreateAsset(backdrop, backgroundPath);
                }
                Undo.RecordObject(backdrop, "Set boss corridor transition");
                backdrop.shader = backgroundShader;
                backdrop.SetFloat("_FadeStartX", corridor.bounds.min.x);
                backdrop.SetFloat("_FadeEndX", corridor.bounds.max.x);
                EditorUtility.SetDirty(backdrop);
                float bottom = floor.bounds.min.y - 4;
                float top = ceiling.bounds.max.y + 4;
                float width = floor.bounds.size.x + corridor.bounds.size.x + 8;
                float scale = Mathf.Max(width / hall.bounds.size.x, (top - bottom) / hall.bounds.size.y);
                GameObject background = new GameObject(BackgroundName);
                Undo.RegisterCreatedObjectUndo(background, "Create boss hall backdrop");
                background.transform.position = new Vector3(floor.bounds.center.x, (bottom + top) * 0.5f, 5.5f);
                background.transform.localScale = new Vector3(scale, scale, 1);
                SpriteRenderer visual = background.AddComponent<SpriteRenderer>();
                visual.sprite = hall;
                visual.sharedMaterial = backdrop;
                visual.color = new Color(0.68f, 0.71f, 0.78f, 1);
                SpriteRenderer tower = all.Where(t => t.name == "Stage3_UpperTowerBackground")
                    .Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault(r => r != null);
                visual.sortingLayerID = tower != null ? tower.sortingLayerID : 0;
                visual.sortingOrder = tower != null ? tower.sortingOrder + 1 : -109;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("Boss room art applied: " + string.Join(", ", targets.Select(r => r.name)) +
                ". Geometry, gate state and encounter scripts unchanged.", room);
            EditorUtility.DisplayDialog("Boss room art", "Boss stone and hall background applied. Room dimensions and boss encounter behavior were not modified.", "OK");
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static Sprite ImportHall()
    {
        string path = Art + "FortressBossHall.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
