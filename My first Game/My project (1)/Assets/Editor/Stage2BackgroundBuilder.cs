using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2BackgroundBuilder
{
    private const string Art = "Assets/Art/RuinedFortress/";
    private const string RootName = "Stage2_FortressBackground";

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform existing = all.FirstOrDefault(t => t.name == RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Background", "Background already exists. No duplicate or geometry changes made.", "OK");
            return;
        }
        SpriteRenderer exterior = all.Where(t => t.name == "DistantFortress_Background")
            .Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault(r => r != null && r.sprite != null && r.gameObject.activeInHierarchy);
        Transform course = all.FirstOrDefault(t => t.name == "Stage2_TrapCourse");
        if (exterior == null || course == null)
            throw new InvalidOperationException("Expected the Stage 1 background and Stage2_TrapCourse.");
        BoxCollider2D[] floors = course.GetComponentsInChildren<BoxCollider2D>()
            .Where(c => c.enabled && !c.isTrigger && c.attachedRigidbody == null).ToArray();
        if (floors.Length == 0) return;
        Sprite block = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Stage2Blockout/Block.png");
        Shader shader = Shader.Find("Fortress/Background Blend");
        Texture2D gallery = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "FortressGallery.png");
        if (block == null || shader == null || gallery == null)
            throw new InvalidOperationException("Background assets are still importing or missing.");
        TextureImporter importer = AssetImporter.GetAtPath(Art + "FortressGallery.png") as TextureImporter;
        if (importer != null)
        {
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            gallery = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "FortressGallery.png");
        }
        Bounds old = exterior.bounds;
        float floorTop = floors.Where(c => c.bounds.size.x > c.bounds.size.y)
            .Select(c => c.bounds.max.y).DefaultIfEmpty(course.position.y).Max();
        Camera camera = Camera.main;
        float viewHeight = camera != null && camera.orthographic ? camera.orthographicSize * 2 : 12;
        float bottom = Mathf.Min(old.min.y, floors.Min(c => c.bounds.min.y) - viewHeight);
        float top = Mathf.Max(old.max.y, floorTop + viewHeight + 4);
        float left = old.max.x - Mathf.Min(10, old.size.x * 0.25f);
        float right = floors.Max(c => c.bounds.max.x) + viewHeight * 2;
        if (right <= left) throw new InvalidOperationException("Stage 2 must be to the right of Stage 1.");
        float galleryHeight = top - bottom;
        float galleryWidth = galleryHeight * gallery.width / gallery.height;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Connect fortress backgrounds only");
        try
        {
            string materialPath = Art + "Stage2BackgroundBlend.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Undo.RecordObject(material, "Configure background transition");
            material.shader = shader;
            material.SetTexture("_Exterior", exterior.sprite.texture);
            material.SetTexture("_Interior", gallery);
            material.SetColor("_ExteriorTint", exterior.color);
            material.SetVector("_ExteriorRect", new Vector4(old.min.x, old.min.y, old.size.x, old.size.y));
            material.SetVector("_GalleryRect", new Vector4(old.max.x, bottom, galleryWidth, galleryHeight));
            material.SetVector("_Transition", new Vector4(left + 2, old.max.x + 12, left, left + 2));
            EditorUtility.SetDirty(material);
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Stage 2 background");
            root.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, exterior.transform.position.z);
            root.transform.localScale = new Vector3((right - left) / block.bounds.size.x, (top - bottom) / block.bounds.size.y, 1);
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = block;
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = exterior.sortingLayerID;
            renderer.sortingOrder = exterior.sortingOrder + 1;
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            SceneView.RepaintAll();
            Debug.Log("Stage 1-2 background connected. Terrain, physics, camera and traps unchanged.", root);
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
