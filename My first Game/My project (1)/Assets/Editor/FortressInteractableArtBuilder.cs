using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FortressInteractableArtBuilder
{
    private const string Sheet = "Assets/Art/RuinedFortress/FortressInteractables.png";

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Sprite[] sprites = ImportSprites();
        Sprite boot = sprites.Single(s => s.name == "WingedBoot");
        Sprite monument = sprites.Single(s => s.name == "CheckpointMonument");
        DoubleJumpPickup[] pickups = scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<DoubleJumpPickup>())
            .Where(p => p.isActiveAndEnabled).ToArray();
        Stage2Checkpoint[] checkpoints = scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<Stage2Checkpoint>())
            .Where(p => p.isActiveAndEnabled).ToArray();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Add checkpoint and winged boot artwork");
        try
        {
            foreach (DoubleJumpPickup pickup in pickups)
            {
                if (pickup.transform.Find("WingedBootVisual") != null) continue;
                HideRenderer(pickup.GetComponent<SpriteRenderer>());
                Transform pedestal = pickup.transform.parent != null ? pickup.transform.parent.Find("ShrinePedestal") : null;
                if (pedestal != null) HideRenderer(pedestal.GetComponent<SpriteRenderer>());
                CreateVisual(pickup.transform, "WingedBootVisual", boot, pickup.transform.position, 0.95f);
                GameObject prompt = new GameObject("WingedBootPrompt");
                Undo.RegisterCreatedObjectUndo(prompt, "Create item prompt");
                prompt.transform.SetParent(pickup.transform, true);
                prompt.transform.position = pickup.transform.position + Vector3.up * 0.85f;
                Vector3 parentScale = pickup.transform.lossyScale;
                prompt.transform.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);
                TextMesh text = prompt.AddComponent<TextMesh>();
                text.text = "E : DOUBLE JUMP";
                text.fontSize = 40;
                text.characterSize = 0.045f;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = new Color(0.95f, 0.90f, 0.65f);
                prompt.GetComponent<MeshRenderer>().sortingOrder = 12;
            }
            foreach (Stage2Checkpoint checkpoint in checkpoints)
            {
                Transform existing = checkpoint.transform.Find("CheckpointMonumentVisual");
                if (existing != null)
                {
                    ConfigureGem(existing.GetComponent<SpriteRenderer>());
                    continue;
                }
                Transform parent = checkpoint.transform.parent;
                Transform pedestal = parent != null ? parent.Find("SavePedestal") : null;
                SpriteRenderer oldPedestal = pedestal != null ? pedestal.GetComponent<SpriteRenderer>() : null;
                // Use the existing pedestal's base, not a guessed scene coordinate.
                if (oldPedestal == null) continue;
                float baseY = oldPedestal.bounds.min.y;
                Vector3 position = new Vector3(oldPedestal.bounds.center.x, baseY + 0.9f, oldPedestal.transform.position.z);
                SpriteRenderer visual = CreateVisual(checkpoint.transform, "CheckpointMonumentVisual", monument, position, 1.8f);
                ConfigureGem(visual);
                foreach (string name in new[] { "SavePedestal", "SaveBeacon", "SafePad" })
                {
                    Transform old = parent.Find(name);
                    if (old != null) HideRenderer(old.GetComponent<SpriteRenderer>());
                }
                Undo.RecordObject(checkpoint, "Connect checkpoint indicator");
                checkpoint.indicator = visual;
                PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint);
                if (checkpoint.statusLabel != null)
                {
                    Undo.RecordObject(checkpoint.statusLabel.transform, "Place prompt above monument");
                    Vector3 labelPosition = checkpoint.statusLabel.transform.position;
                    labelPosition.y = baseY + 2.15f;
                    checkpoint.statusLabel.transform.position = labelPosition;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint.statusLabel.transform);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            EditorUtility.DisplayDialog("Interaction art",
                $"Processed {pickups.Length} active double-jump pickups and {checkpoints.Length} active checkpoints. Existing visuals are hidden, not deleted. Colliders, interaction layers and save locations are unchanged. No healing or enemy respawn added.", "OK");
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static void HideRenderer(SpriteRenderer renderer)
    {
        if (renderer == null) return;
        Undo.RecordObject(renderer, "Hide old placeholder artwork");
        renderer.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }

    private static void ConfigureGem(SpriteRenderer renderer)
    {
        if (renderer == null) return;
        Shader shader = Shader.Find("Fortress/Checkpoint Gem");
        if (shader == null) throw new InvalidOperationException("Checkpoint gem shader is still importing.");
        const string path = "Assets/Art/RuinedFortress/CheckpointGem.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        Undo.RecordObject(renderer, "Use gem-only checkpoint color");
        renderer.sharedMaterial = material;
        renderer.color = Color.white;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }

    private static SpriteRenderer CreateVisual(Transform parent, string name, Sprite sprite, Vector3 position, float height)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create interaction artwork");
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        float scale = height / sprite.bounds.size.y;
        Vector3 parentScale = parent.lossyScale;
        go.transform.localScale = new Vector3(scale / parentScale.x, scale / parentScale.y, 1 / parentScale.z);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 6;
        return renderer;
    }

    private static Sprite[] ImportSprites()
    {
        AssetDatabase.ImportAsset(Sheet, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(Sheet) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Interaction sprite sheet missing.");
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
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Sheet);
        Color32[] pixels = texture.GetPixels32();
        string[] names = { "WingedBoot", "CheckpointMonument" };
#pragma warning disable 0618
        SpriteMetaData[] slices = new SpriteMetaData[2];
        for (int i = 0; i < 2; i++)
        {
            int x0 = i * texture.width / 2, x1 = (i + 1) * texture.width / 2;
            int minX = x1, maxX = -1, minY = texture.height, maxY = -1;
            for (int y = 0; y < texture.height; y++)
                for (int x = x0; x < x1; x++)
                    if (pixels[y * texture.width + x].a > 25)
                    {
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
            if (maxX < minX) throw new InvalidOperationException("Empty sprite: " + names[i]);
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
        return AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToArray();
    }
}
