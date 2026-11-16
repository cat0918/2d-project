using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CastleEntranceBuilder
{
    [MenuItem("Tools/Scene Setup/Build Castle Entrance")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform existing = all.FirstOrDefault(t => t.name == "CastleEntrance");
        if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
        Transform stage = all.SingleOrDefault(t => t.name == "Stage1");
        BoxCollider2D floor = stage != null ? stage.Find("RoomHorizon/Floor_Horizon")?.GetComponent<BoxCollider2D>() : null;
        Transform oldWall = stage != null ? stage.Find("RoomHorizon/Wall_Left_Horizon") : null;
        PlayerMove player = all.Select(t => t.GetComponent<PlayerMove>()).FirstOrDefault(p => p != null && p.isActiveAndEnabled);
        Stage2Checkpoint source = all.Select(t => t.GetComponent<Stage2Checkpoint>()).FirstOrDefault(c =>
            c != null && c.isActiveAndEnabled && c.respawnPoint != null && c.indicator != null &&
            c.transform.parent != null && c.respawnPoint.parent == c.transform.parent);
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Stage2Blockout/Block.png");
        Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/RuinedFortress/FortressStone.mat");
        SpriteRenderer courtyard = all.Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault(r =>
            r != null && r.sprite != null && r.sprite.texture.name == "FortressCourtyard" && r.enabled);
        if (floor == null || oldWall == null || player == null || source == null || square == null || stone == null || courtyard == null)
            throw new InvalidOperationException("Entrance needs Stage1 floor/left wall, player, a configured checkpoint, courtyard artwork and stone material.");
        Physics2D.SyncTransforms();
        float top = floor.bounds.max.y;
        float gateX = oldWall.position.x;
        float left = Mathf.Min(gateX - 14f, floor.bounds.min.x - 12f);
        float right = Mathf.Max(gateX + 2f, floor.bounds.min.x + 1f);
        int groundLayer = floor.gameObject.layer;
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        int interactLayer = source.gameObject.layer;
        if (interaction != null && (interaction.interactLayers.value & (1 << interactLayer)) == 0)
            throw new InvalidOperationException("The source checkpoint layer is not included in the player's interaction mask.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build castle entrance");
        try
        {
            GameObject root = new GameObject("CastleEntrance");
            Undo.RegisterCreatedObjectUndo(root, "Create castle entrance");
            Stage2Course course = root.AddComponent<Stage2Course>();
            Block(root.transform, "ApproachFloor", new Vector2((left + right) * 0.5f, top - 0.5f),
                new Vector2(right - left, 1f), square, stone, Color.white, groundLayer, true);
            Block(root.transform, "OuterBoundary", new Vector2(left - 0.4f, top + 5f),
                new Vector2(0.8f, 10f), square, stone, Color.white, groundLayer, true);

            // Repeat existing exterior art at its original scale, rather than stretching the map.
            float bgWidth = courtyard.bounds.size.x;
            int copies = Mathf.Clamp(Mathf.CeilToInt((courtyard.bounds.min.x - left + 10f) / Mathf.Max(1f, bgWidth)), 1, 5);
            for (int i = 1; i <= copies; i++)
            {
                GameObject bg = new GameObject("ExteriorBackground_" + i);
                bg.transform.SetParent(root.transform, false);
                bg.transform.position = courtyard.transform.position + Vector3.left * bgWidth * i;
                bg.transform.rotation = courtyard.transform.rotation;
                bg.transform.localScale = courtyard.transform.lossyScale;
                SpriteRenderer renderer = bg.AddComponent<SpriteRenderer>();
                renderer.sprite = courtyard.sprite;
                renderer.sharedMaterial = courtyard.sharedMaterial;
                renderer.color = courtyard.color;
                renderer.sortingLayerID = 0;
                renderer.sortingOrder = -200;
            }

            Block(root.transform, "GatehouseUpperWall", new Vector2(gateX, top + 8f),
                new Vector2(2.2f, 4f), square, stone, Color.white, groundLayer, true).GetComponent<SpriteRenderer>().sortingOrder = 5;
            Block(root.transform, "GateLintel", new Vector2(gateX, top + 6.1f),
                new Vector2(2.8f, 0.4f), square, stone, Color.white, groundLayer, false).GetComponent<SpriteRenderer>().sortingOrder = 6;
            GameObject gate = new GameObject("RisingIronGate");
            gate.transform.SetParent(root.transform, false);
            gate.transform.position = new Vector3(gateX, top + 3f, 0f);
            gate.layer = groundLayer;
            BoxCollider2D gateCollider = gate.AddComponent<BoxCollider2D>();
            gateCollider.size = new Vector2(1.1f, 6f);
            for (int i = 0; i < 5; i++)
                Block(gate.transform, "IronBar_" + i, new Vector2(gateX - 0.5f + i * 0.25f, top + 3f),
                    new Vector2(0.12f, 6f), square, null, new Color(0.16f, 0.2f, 0.23f), groundLayer, false);
            for (int i = 0; i < 4; i++)
                Block(gate.transform, "IronBrace_" + i, new Vector2(gateX, top + 0.55f + i * 1.5f),
                    new Vector2(1.3f, 0.16f), square, null, new Color(0.3f, 0.34f, 0.35f), groundLayer, false);

            GameObject trigger = new GameObject("GateInteraction");
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.position = new Vector3(gateX, top + 1f, 0f);
            trigger.layer = interactLayer;
            BoxCollider2D range = trigger.AddComponent<BoxCollider2D>();
            range.isTrigger = true;
            range.size = new Vector2(1.8f, 2f);
            CastleEntranceGate controller = trigger.AddComponent<CastleEntranceGate>();
            controller.movingGate = gate.transform;
            controller.gateCollider = gateCollider;

            GameObject checkpoint = UnityEngine.Object.Instantiate(source.transform.parent.gameObject, root.transform);
            checkpoint.name = "EntranceRestPoint";
            Stage2Checkpoint save = checkpoint.GetComponentInChildren<Stage2Checkpoint>(true);
            Physics2D.SyncTransforms();
            float checkpointX = left + 5f;
            checkpoint.transform.position += new Vector3(checkpointX - save.indicator.bounds.center.x,
                top - save.indicator.bounds.min.y, 0f);
            save.course = course;
            foreach (TextMesh text in checkpoint.GetComponentsInChildren<TextMesh>(true))
                text.GetComponent<Renderer>().enabled = false;
            Collider2D playerBody = player.GetComponent<Collider2D>();
            float footOffset = playerBody != null ? player.transform.position.y - playerBody.bounds.min.y : 0.5f;
            save.respawnPoint.position = new Vector3(checkpointX - 1.5f, top + footOffset + 0.08f, player.transform.position.z);
            course.entryCheckpoint = save.respawnPoint;

            Undo.RecordObject(oldWall.gameObject, "Open original stage boundary");
            oldWall.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(oldWall.gameObject);
            Undo.RecordObject(player.transform, "Move starting player outside castle");
            player.transform.position = new Vector3(left + 2.5f, top + footOffset + 0.08f, player.transform.position.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            foreach (CameraFollow2D camera in all.Select(t => t.GetComponent<CameraFollow2D>()).Where(c => c != null && c.useBounds))
            {
                Undo.RecordObject(camera, "Expand camera bounds for entrance");
                camera.minBounds.x = Mathf.Min(camera.minBounds.x, left - 1f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            Physics2D.SyncTransforms();
            ApplyEntranceSorting(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorUtility.DisplayDialog("Castle Entrance", "Entrance, resting checkpoint and interactive gate created. Save the scene and enter Play mode. The gate stays open after death reloads within this Play session. Ctrl+Z undoes the setup.", "OK");
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    [MenuItem("Tools/Scene Setup/Fix Castle Entrance Sorting")]
    public static void FixSorting()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform entrance = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "CastleEntrance");
        if (entrance == null)
        {
            EditorUtility.DisplayDialog("Entrance sorting", "CastleEntrance was not found in the current scene.", "OK");
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fix castle entrance sorting");
        ApplyEntranceSorting(entrance);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        Selection.activeGameObject = entrance.gameObject;
        Debug.Log("Entrance background -200; floor 3; iron gate 20; upper wall 25; lintel 26. Geometry and colliders unchanged.", entrance);
    }

    private static void ApplyEntranceSorting(Transform entrance)
    {
        foreach (SpriteRenderer renderer in entrance.GetComponentsInChildren<SpriteRenderer>(true))
        {
            int order;
            if (renderer.name.StartsWith("ExteriorBackground_", StringComparison.Ordinal)) order = -200;
            else if (renderer.name.StartsWith("IronBar_", StringComparison.Ordinal) ||
                renderer.name.StartsWith("IronBrace_", StringComparison.Ordinal)) order = 20;
            else if (renderer.name == "GatehouseUpperWall") order = 25;
            else if (renderer.name == "GateLintel") order = 26;
            else if (renderer.name == "ApproachFloor" || renderer.name == "OuterBoundary") order = 3;
            else continue;
            Undo.RecordObject(renderer, "Set entrance draw order");
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = order;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }

    private static GameObject Block(Transform parent, string name, Vector2 position, Vector2 size,
        Sprite sprite, Material material, Color color, int layer, bool solid)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        go.layer = layer;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        if (material != null) renderer.sharedMaterial = material;
        renderer.color = color;
        renderer.sortingOrder = 3;
        if (solid) go.AddComponent<BoxCollider2D>();
        return go;
    }
}
