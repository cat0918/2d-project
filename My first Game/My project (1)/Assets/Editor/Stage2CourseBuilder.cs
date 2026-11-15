using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2CourseBuilder
{
    private const string RootName = "Stage2_TrapCourse";
    private const string ArtFolder = "Assets/Generated/Stage2Blockout";
    private static Sprite square;
    private static Sprite spike;
    private static Sprite arrow;
    private static Transform root;
    private static int groundLayer;
    private static Stage2Course course;
    private static readonly Color FloorColor = new Color(0.48f, 0.67f, 0.72f);
    private static readonly Color SafeColor = new Color(0.35f, 0.85f, 0.65f);

    [MenuItem("Tools/Level Design/Build Stage 2 Trap Course")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;
        GameObject[] objects = scene.GetRootGameObjects();
        Transform[] transforms = objects.SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform existing = transforms.FirstOrDefault(t => t.name == RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Stage 2", "A generated course already exists. Select and edit it; no duplicate was created.", "OK");
            return;
        }
        Transform stage = transforms.FirstOrDefault(t => t.name == "Stage2" || t.name == "Stage_02");
        Collider2D anchor = null;
        if (stage != null)
            anchor = stage.GetComponentsInChildren<Collider2D>()
                .Where(c => !c.isTrigger && c.attachedRigidbody == null && c.bounds.size.x > c.bounds.size.y)
                .OrderByDescending(c => c.bounds.max.x).FirstOrDefault();
        if (anchor == null && Selection.activeGameObject != null)
            anchor = Selection.activeGameObject.GetComponent<Collider2D>();
        if (anchor == null || anchor.isTrigger || anchor.attachedRigidbody != null || anchor.gameObject.scene != scene)
        {
            EditorUtility.DisplayDialog("Stage 2", "Select the last Stage 2 floor (with a solid Collider2D), then run this menu again.", "OK");
            return;
        }
        PlayerMove player = objects.SelectMany(o => o.GetComponentsInChildren<PlayerMove>())
            .FirstOrDefault(p => p.isActiveAndEnabled);
        if (player == null || (player.groundLayer.value & (1 << anchor.gameObject.layer)) == 0)
        {
            EditorUtility.DisplayDialog("Stage 2", "The anchor must use a layer included in the active PlayerMove Ground Layer.", "OK");
            return;
        }
        // Read bounds from the live scene: parent transforms and unsaved edits are respected.
        Vector3 origin = new Vector3(anchor.bounds.max.x - 0.05f, anchor.bounds.max.y, 0f);
        groundLayer = anchor.gameObject.layer;
        square = CreateSprite("Block", (x, y) => true);
        spike = CreateSprite("Spike", (x, y) => Mathf.Abs(x - 15.5f) <= (31f - y) * 0.5f);
        arrow = CreateSprite("Arrow", (x, y) => (x < 12 && Mathf.Abs(y - 15.5f) <= x) || (x >= 8 && y >= 13 && y <= 18));
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Stage 2 Trap Course");
        try
        {
            GameObject courseObject = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(courseObject, "Create trap course");
            root = courseObject.transform;
            root.position = origin;
            if (stage != null) Undo.SetTransformParent(root, stage, "Parent trap course");
            course = Undo.AddComponent<Stage2Course>(courseObject);

            Transform entry = Group("01_Entry");
            Floor(entry, "SafeEntry", 0, 5, 0);
            course.entryCheckpoint = Checkpoint(entry, 2.5f, 0);
            Label(entry, "ENTRY / RETURN TO STAGE 1", 2.5f, 3);

            Transform arrows = Group("02_ArrowTiming");
            Floor(arrows, "ArrowRunway", 5, 19, 0);
            Emitter(arrows, "ArrowLauncher", new Vector2(18.5f, 0.65f), Vector2.left,
                arrow, new Vector2(0.8f, 0.22f), 8, 2.8f, 0.8f, 0, 1.7f,
                new Vector2(18.5f, 1.2f));
            Label(arrows, "01  ARROWS / WATCH THE RED WARNING", 12, 4);

            Transform restA = Group("03_SafeLanding");
            Floor(restA, "SafeFloor", 19, 24, 0);

            Transform falling = Group("04_FallingObjects");
            Floor(falling, "Landing_A", 24, 28, 0);
            Floor(falling, "Landing_B", 29.5f, 33, 0.7f);
            Floor(falling, "Landing_C", 34.5f, 39, 0);
            Spikes(falling, 28, 29.5f, -1.8f);
            Spikes(falling, 33, 34.5f, -1.8f);
            for (int i = 0; i < 3; i++)
            {
                float x = new[] { 26f, 31.25f, 36.5f }[i];
                float top = i == 1 ? 0.7f : 0f;
                Emitter(falling, "FallingBlock_" + (i + 1), new Vector2(x, 6.5f), Vector2.down,
                    square, new Vector2(0.8f, 0.8f), 14, 3.8f, 0.9f, i * 1.1f, 1.5f,
                    new Vector2(x, top + 0.12f));
            }
            Label(falling, "02  FALLING BLOCKS / WAIT THEN CROSS", 31.5f, 8);

            Transform restB = Group("05_SafeLanding");
            Floor(restB, "SafeFloor", 39, 44, 0);
            Checkpoint(restB, 41.5f, 0);

            Transform jumps = Group("06_SpikeJumps");
            Spikes(jumps, 44, 60, -1.8f);
            Floor(jumps, "Step_A", 45, 47, 0.5f);
            Floor(jumps, "Step_B", 49, 51, 1f);
            Floor(jumps, "Step_C", 53, 55, 0.5f);
            Floor(jumps, "Step_D", 57, 60, 0);
            Label(jumps, "03  SPIKES / SINGLE JUMPS", 52, 4);

            Transform reward = Group("07_DoubleJumpReward");
            Floor(reward, "ShrineFloor", 60, 68, 0);
            Visual(reward, "ShrinePedestal", new Vector2(65, 0.4f), new Vector2(1.2f, 0.8f), square, SafeColor);
            GameObject shrine = Visual(reward, "DoubleJumpShrine", new Vector2(65, 1.2f), new Vector2(0.65f, 0.85f), square, new Color(0.65f, 1f, 0.9f));
            BoxCollider2D shrineCollider = shrine.AddComponent<BoxCollider2D>();
            shrineCollider.isTrigger = true;
            shrine.AddComponent<DoubleJumpPickup>();
            // Use a layer the player's existing interaction filter can see.
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            if (interaction != null)
                for (int layer = 0; layer < 32; layer++)
                    if ((interaction.interactLayers.value & (1 << layer)) != 0)
                    { shrine.layer = layer; break; }
            Floor(reward, "EndWall", 68, 69, 5, 6);
            Label(reward, "E : DOUBLE JUMP\nRETURN TO STAGE 1 > STAGE 3", 64, 3.5f);

            GameObject safety = Visual(root, "FallReturnZone", new Vector2(35, -5), new Vector2(72, 2), square, new Color(1, 0.2f, 0.1f, 0.08f));
            safety.AddComponent<BoxCollider2D>().isTrigger = true;
            safety.AddComponent<StageTrapHazard>().course = course;

            // Keep the original shrine for undo, but prevent an early ability pickup.
            foreach (DoubleJumpPickup old in objects.SelectMany(o => o.GetComponentsInChildren<DoubleJumpPickup>(true)))
            {
                if (old.transform.IsChildOf(root)) continue;
                Undo.RecordObject(old.gameObject, "Disable earlier shrine");
                old.gameObject.SetActive(false);
            }
            Undo.RecordObject(player, "Start without double jump");
            player.startWithDoubleJump = false;
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = courseObject;
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.in2DMode = true;
                view.Frame(new Bounds(origin + new Vector3(34, 1, 0), new Vector3(74, 16, 1)), false);
            }
            Debug.Log("Stage 2 course built. Save the scene, then test both directions. Ctrl+Z undoes scene placement.", courseObject);
        }
        catch (Exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
        finally { Undo.CollapseUndoOperations(undoGroup); }
    }

    [MenuItem("Tools/Level Design/Tune Stage 2 Traps")]
    public static void TuneTraps()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Stage2Course found = scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<Stage2Course>(true)).FirstOrDefault();
        if (found == null)
        {
            EditorUtility.DisplayDialog("Stage 2", "Build the Stage 2 course first.", "OK");
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tune Stage 2 arrow and falling traps");
        try
        {
            Transform arrows = found.transform.Find("02_ArrowTiming");
            if (arrows != null)
                foreach (string name in new[] { "CoverStep_A", "CoverStep_B" })
                {
                    Transform step = arrows.Find(name);
                    if (step == null || !step.gameObject.activeSelf) continue;
                    Undo.RecordObject(step.gameObject, "Disable arrow blockers");
                    step.gameObject.SetActive(false);
                }
            Transform falling = found.transform.Find("04_FallingObjects");
            if (falling != null)
                foreach (StageTrapEmitter emitter in falling.GetComponentsInChildren<StageTrapEmitter>(true))
                {
                    if (!emitter.name.StartsWith("FallingBlock_")) continue;
                    Undo.RecordObject(emitter, "Speed up falling blocks");
                    emitter.speed = 14f;
                }
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = found.gameObject;
            Debug.Log("Arrow blockers disabled; falling speed set to 14. Warning times and save points are unchanged.", found);
        }
        catch (Exception)
        {
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static Transform Group(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        return go.transform;
    }

    private static GameObject Visual(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = root.TransformPoint(position);
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 1;
        return go;
    }

    private static void Floor(Transform parent, string name, float left, float right, float top, float height = 0.5f)
    {
        GameObject go = Visual(parent, name, new Vector2((left + right) / 2, top - height / 2), new Vector2(right - left, height), square, FloorColor);
        go.layer = groundLayer;
        go.AddComponent<BoxCollider2D>();
    }

    private static Transform Checkpoint(Transform parent, float x, float top)
    {
        Visual(parent, "SafePad", new Vector2(x, top + 0.03f), new Vector2(2, 0.06f), square, SafeColor);
        GameObject point = new GameObject("RespawnPoint");
        point.transform.SetParent(parent, false);
        point.transform.position = root.TransformPoint(new Vector3(x, top + 1.2f));
        GameObject trigger = new GameObject("Checkpoint");
        trigger.transform.SetParent(parent, false);
        trigger.transform.position = root.TransformPoint(new Vector3(x, top + 0.5f));
        BoxCollider2D collider = trigger.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2, 1.2f);
        Stage2Checkpoint checkpoint = trigger.AddComponent<Stage2Checkpoint>();
        checkpoint.course = course;
        checkpoint.respawnPoint = point.transform;
        GameObject pedestal = Visual(parent, "SavePedestal", new Vector2(x, top + 0.3f), new Vector2(0.9f, 0.6f), square, SafeColor);
        GameObject beacon = Visual(parent, "SaveBeacon", new Vector2(x, top + 0.95f), new Vector2(0.4f, 0.7f), square, SafeColor);
        checkpoint.indicator = beacon.GetComponent<SpriteRenderer>();
        GameObject labelObject = new GameObject("SavePrompt");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = root.TransformPoint(new Vector3(x, top + 1.8f, 0));
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "E : SAVE CHECKPOINT";
        label.fontSize = 40;
        label.characterSize = 0.06f;
        label.anchor = TextAnchor.MiddleCenter;
        checkpoint.statusLabel = label;
        PlayerInteraction interaction = root.gameObject.scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<PlayerInteraction>()).FirstOrDefault(p => p.isActiveAndEnabled);
        if (interaction != null)
            for (int layer = 0; layer < 32; layer++)
                if ((interaction.interactLayers.value & (1 << layer)) != 0)
                { trigger.layer = layer; break; }
        return point.transform;
    }

    [MenuItem("Tools/Level Design/Upgrade Stage 2 Save Points")]
    public static void UpgradeSavePoints()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Stage2Course found = scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<Stage2Course>(true)).FirstOrDefault();
        if (found == null)
        {
            EditorUtility.DisplayDialog("Save points", "Build the Stage 2 course first.", "OK");
            return;
        }
        root = found.transform;
        if (root.Find("ManualSavePoints") != null)
        {
            Selection.activeGameObject = root.Find("ManualSavePoints").gameObject;
            return;
        }
        Transform entry = root.Find("01_Entry/RespawnPoint");
        Transform middle = root.Find("05_SafeLanding/RespawnPoint");
        if (entry == null || middle == null)
        {
            EditorUtility.DisplayDialog("Save points", "Could not find the entry and middle respawn points. The existing map has not been changed.", "OK");
            return;
        }
        Vector3 entryPosition = root.InverseTransformPoint(entry.position);
        Vector3 middlePosition = root.InverseTransformPoint(middle.position);
        square = CreateSprite("Block", (x, y) => true);
        course = found;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Replace automatic checkpoints with save structures");
        try
        {
            foreach (Stage2Checkpoint old in root.GetComponentsInChildren<Stage2Checkpoint>(true))
            {
                Undo.RecordObject(old.gameObject, "Disable old checkpoint");
                old.gameObject.SetActive(false);
            }
            foreach (Transform old in root.GetComponentsInChildren<Transform>(true))
                if (old.name == "SafePad" || old.name == "SavePedestal" || old.name == "SaveBeacon" || old.name == "SavePrompt")
                {
                    Undo.RecordObject(old.gameObject, "Hide old checkpoint marker");
                    old.gameObject.SetActive(false);
                }
            Transform container = Group("ManualSavePoints");
            Undo.RegisterCreatedObjectUndo(container.gameObject, "Create save structures");
            Transform startGroup = new GameObject("StartSavePoint").transform;
            startGroup.SetParent(container, false);
            Transform midGroup = new GameObject("MiddleSavePoint").transform;
            midGroup.SetParent(container, false);
            Undo.RecordObject(course, "Set fallback entry");
            course.entryCheckpoint = Checkpoint(startGroup, entryPosition.x, entryPosition.y - 1.2f);
            Checkpoint(midGroup, middlePosition.x, middlePosition.y - 1.2f);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = container.gameObject;
        }
        catch (Exception)
        {
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static void Spikes(Transform parent, float left, float right, float bottom)
    {
        for (float x = left + 0.25f; x < right; x += 0.5f)
            Visual(parent, "Spike", new Vector2(x, bottom + 0.25f), new Vector2(0.5f, 0.5f), spike, new Color(1f, 0.4f, 0.35f));
        GameObject trigger = new GameObject("SpikeHazard");
        trigger.transform.SetParent(parent, false);
        trigger.transform.position = root.TransformPoint(new Vector3((left + right) / 2, bottom + 0.18f));
        BoxCollider2D hit = trigger.AddComponent<BoxCollider2D>();
        hit.isTrigger = true;
        hit.size = new Vector2(right - left, 0.36f);
        trigger.AddComponent<StageTrapHazard>().course = course;
    }

    private static void Emitter(Transform parent, string name, Vector2 position, Vector2 direction, Sprite sprite,
        Vector2 size, float speed, float interval, float warning, float delay, float lifetime, Vector2 markerPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = root.TransformPoint(position);
        Visual(parent, name + "_Housing", position, new Vector2(0.6f, 0.6f), square, new Color(0.8f, 0.6f, 0.3f));
        GameObject marker = Visual(parent, name + "_Warning", markerPosition, new Vector2(1f, 0.12f), square, new Color(0.45f, 0.28f, 0.2f));
        StageTrapEmitter emitter = go.AddComponent<StageTrapEmitter>();
        emitter.projectileSprite = sprite;
        emitter.projectileSize = size;
        emitter.direction = direction;
        emitter.speed = speed;
        emitter.interval = interval;
        emitter.warningTime = warning;
        emitter.initialDelay = delay;
        emitter.projectileLifetime = lifetime;
        emitter.terrainMask = 1 << groundLayer;
        emitter.warningMarker = marker.GetComponent<SpriteRenderer>();
    }

    private static void Label(Transform parent, string value, float x, float y)
    {
        GameObject go = new GameObject("Guide_" + value.Split('\n')[0]);
        go.transform.SetParent(parent, false);
        go.transform.position = root.TransformPoint(new Vector3(x, y, 0));
        TextMesh text = go.AddComponent<TextMesh>();
        text.text = value;
        text.fontSize = 40;
        text.characterSize = 0.07f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.8f, 0.95f, 1f);
    }

    private static Sprite CreateSprite(string name, Func<int, int, bool> visible)
    {
        Directory.CreateDirectory(ArtFolder);
        string path = ArtFolder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++) texture.SetPixel(x, y, visible(x, y) ? Color.white : Color.clear);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
