using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FortressTrapArtBuilder
{
    private const string SheetPath = "Assets/Art/RuinedFortress/FortressTraps.png";

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] courses = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "Stage2_TrapCourse" || t.name == "Stage3_ExplorationCourse").ToArray();
        if (courses.Length == 0) throw new InvalidOperationException("No Stage 2/3 trap courses found.");
        Sprite[] sprites = ImportSprites();
        Sprite spike = sprites.Single(s => s.name == "FortressSpike");
        Sprite arrow = sprites.Single(s => s.name == "FortressBolt");
        Sprite stone = sprites.Single(s => s.name == "FortressFallingStone");
        Sprite housing = sprites.Single(s => s.name == "FortressLauncher");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply fortress trap artwork");
        int spikeCount = 0, emitterCount = 0;
        try
        {
            foreach (Transform course in courses)
            {
                foreach (SpriteRenderer renderer in course.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.name == "Spike")
                    {
                        ReplaceDecoration(renderer, spike);
                        spikeCount++;
                    }
                foreach (StageTrapEmitter emitter in course.GetComponentsInChildren<StageTrapEmitter>(true))
                {
                    bool falling = Mathf.Abs(emitter.direction.y) > Mathf.Abs(emitter.direction.x) && !emitter.fireBothDirections;
                    Undo.RecordObject(emitter, "Replace trap projectile artwork");
                    emitter.projectileSprite = falling ? stone : arrow;
                    emitter.projectileColor = Color.white;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(emitter);
                    Transform shell = emitter.transform.parent != null
                        ? emitter.transform.parent.Find(emitter.name + "_Housing") : null;
                    if (shell != null && shell.GetComponent<SpriteRenderer>() != null)
                        ReplaceDecoration(shell.GetComponent<SpriteRenderer>(), falling ? stone : housing);
                    emitterCount++;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            EditorUtility.DisplayDialog("Trap artwork",
                $"Updated {spikeCount} spikes and {emitterCount} emitters. Projectile sizes, colliders, timing, damage and warning markers are unchanged.", "OK");
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static void ReplaceDecoration(SpriteRenderer renderer, Sprite sprite)
    {
        // Only resize collider-free leaf artwork, never the hazard or emitter transform.
        if (renderer.sprite == null || renderer.drawMode != SpriteDrawMode.Simple ||
            renderer.transform.childCount != 0 || renderer.GetComponent<Collider2D>() != null ||
            renderer.GetComponent<Rigidbody2D>() != null)
            throw new InvalidOperationException("Expected collider-free leaf decoration: " + renderer.name);
        Vector3 oldSize = renderer.sprite.bounds.size;
        Vector3 newSize = sprite.bounds.size;
        Undo.RecordObjects(new UnityEngine.Object[] { renderer, renderer.transform }, "Replace trap decoration");
        Vector3 scale = renderer.transform.localScale;
        renderer.transform.localScale = new Vector3(scale.x * oldSize.x / newSize.x,
            scale.y * oldSize.y / newSize.y, scale.z);
        renderer.sprite = sprite;
        renderer.color = Color.white;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
    }

    private static Sprite[] ImportSprites()
    {
        AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Trap sprite sheet is missing.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 128;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
        Color32[] pixels = texture.GetPixels32();
        string[] names = { "FortressFallingStone", "FortressLauncher", "FortressSpike", "FortressBolt" };
#pragma warning disable 0618
        SpriteMetaData[] slices = new SpriteMetaData[4];
        for (int i = 0; i < 4; i++)
        {
            int x0 = (i % 2) * texture.width / 2, x1 = (i % 2 + 1) * texture.width / 2;
            int y0 = (i / 2) * texture.height / 2, y1 = (i / 2 + 1) * texture.height / 2;
            int minX = x1, maxX = x0 - 1, minY = y1, maxY = y0 - 1;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (pixels[y * texture.width + x].a > 25)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
            if (maxX < minX) throw new InvalidOperationException("Empty sprite quadrant: " + names[i]);
            slices[i] = new SpriteMetaData
            {
                name = names[i], rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                alignment = (int)SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = slices;
#pragma warning restore 0618
        importer.isReadable = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
    }
}
