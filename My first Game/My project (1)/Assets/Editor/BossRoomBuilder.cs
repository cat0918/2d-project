using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Parts = Stage2CourseBuilder;

public static class BossRoomBuilder
{
    private const string RootName = "BossRoom";

    [MenuItem("Tools/Level Design/Build Boss Room")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] objects = scene.GetRootGameObjects()
            .SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform existing = objects.FirstOrDefault(t => t.name == RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Boss room", "A boss room already exists. No duplicate was created.", "OK");
            return;
        }
        Transform connection = objects.FirstOrDefault(t => t.name == "BossConnectionAnchor");
        Transform approach = connection != null ? connection.parent.Find("LongApproach") : null;
        Collider2D floor = approach != null ? approach.GetComponent<Collider2D>() : null;
        if (connection == null || floor == null)
        {
            EditorUtility.DisplayDialog("Boss room", "Build Stage 3 first. Its BossConnectionAnchor and LongApproach floor are required.", "OK");
            return;
        }
        // Follow the actual floor edge even if the user has resized or moved the approach.
        Vector3 origin = new Vector3(floor.bounds.min.x + 0.05f, floor.bounds.max.y, 0);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build boss room blockout");
        try
        {
            GameObject room = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(room, "Create boss room");
            room.transform.position = origin;
            Parts.PrepareLayout(room.transform, floor.gameObject.layer, null);

            Transform entrance = Parts.Group("01_Entrance");
            Parts.Floor(entrance, "EntryCorridor", -8, 0, 0, 1);
            Parts.Label(entrance, "BOSS APPROACH", -4, 3);
            Point(entrance, "EntranceGateAnchor", origin + new Vector3(-8.5f, 0, 0));

            Transform arena = Parts.Group("02_Arena");
            Parts.Floor(arena, "ArenaFloor", -36, -8, 0, 1);
            Parts.Floor(arena, "BackWall", -37, -36, 13, 14);
            Parts.Floor(arena, "Ceiling", -37, -8, 13, 1);
            // The entrance remains open for free travel; no battle lock is added yet.
            Parts.Floor(arena, "EntranceLintel", -9, -8, 13, 9);
            Parts.Label(arena, "BOSS ARENA / OPEN ENTRANCE", -22, 10.5f);

            Transform markers = Parts.Group("03_PlacementMarkers");
            Transform spawn = Point(markers, "BossSpawnPoint", origin + new Vector3(-27, 1.2f, 0));
            EnemyPlacementMarker marker = spawn.gameObject.AddComponent<EnemyPlacementMarker>();
            marker.markerId = "BOSS";
            marker.placementNote = "Place the existing boss here later. This marker has no collider or spawning behavior.";
            marker.suggestedPatrolWidth = 0;
            Point(markers, "PlayerEntryPoint", origin + new Vector3(-12, 1.2f, 0));
            Point(markers, "ArenaCameraCenter", origin + new Vector3(-22, 5.5f, 0));

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = room;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Frame(new Bounds(origin + new Vector3(-18, 6, 0), new Vector3(43, 18, 1)), false);
            }
            Debug.Log("Boss room built. The boss, camera lock, entrance lock and clear event are not connected yet.", room);
        }
        catch (Exception)
        {
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    [MenuItem("Tools/Level Design/Connect Boss Encounter")]
    public static void ConnectEncounter()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform room = all.FirstOrDefault(t => t.name == RootName);
        Transform spawn = room != null ? room.Find("03_PlacementMarkers/BossSpawnPoint") : null;
        Transform floor = room != null ? room.Find("02_Arena/ArenaFloor") : null;
        Sprite block = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Stage2Blockout/Block.png");
        if (spawn == null || floor == null || block == null)
        {
            EditorUtility.DisplayDialog("Boss encounter", "Build the boss room first.", "OK");
            return;
        }
        BossRoomEncounter existing = room.GetComponentInChildren<BossRoomEncounter>(true);
        if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
        BossController source = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<BossController>() : null;
        if (source == null || source.gameObject.scene != scene)
        {
            BossController[] candidates = all.Select(t => t.GetComponent<BossController>()).Where(b => b != null).ToArray();
            if (candidates.Length != 1)
            {
                EditorUtility.DisplayDialog("Boss encounter", "Select the original Boss object in the Hierarchy, then run this menu again.", "OK");
                return;
            }
            source = candidates[0];
        }
        if (source.GetComponent<EnemyHealth>() == null || source.GetComponent<EnemyAnimation>() == null)
        {
            EditorUtility.DisplayDialog("Boss encounter", "The selected boss needs EnemyHealth and EnemyAnimation.", "OK");
            return;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Connect boss entrance and wake sequence");
        try
        {
            GameObject setup = new GameObject("04_Encounter");
            Undo.RegisterCreatedObjectUndo(setup, "Create encounter");
            setup.transform.SetParent(room, false);
            GameObject actor = UnityEngine.Object.Instantiate(source.gameObject, setup.transform);
            actor.name = "ArenaBoss";
            actor.transform.position = spawn.position;
            actor.SetActive(true);
            BossController controller = actor.GetComponent<BossController>();
            controller.enabled = false;
            controller.detectRange = Mathf.Max(controller.detectRange, 40f);
            actor.GetComponent<EnemyAnimation>().enabled = true;
            foreach (EnemyPatrol patrol in actor.GetComponentsInChildren<EnemyPatrol>(true)) patrol.enabled = false;

            GameObject gate = new GameObject("EntranceGate");
            gate.transform.SetParent(setup.transform, false);
            gate.transform.localPosition = new Vector3(-8.5f, 2.05f, 0);
            gate.transform.localScale = new Vector3(1f, 4.1f, 1f);
            gate.layer = floor.gameObject.layer;
            SpriteRenderer visual = gate.AddComponent<SpriteRenderer>();
            visual.sprite = block;
            visual.color = new Color(0.8f, 0.4f, 0.25f);
            visual.sortingOrder = 4;
            gate.AddComponent<BoxCollider2D>();
            gate.SetActive(false);

            GameObject area = new GameObject("BossEncounterTrigger");
            area.transform.SetParent(setup.transform, false);
            area.transform.localPosition = new Vector3(-22f, 5.5f, 0);
            BoxCollider2D trigger = area.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(24f, 11f);
            BossRoomEncounter encounter = area.AddComponent<BossRoomEncounter>();
            encounter.boss = controller;
            encounter.entranceGate = gate;
            Undo.RecordObject(source.gameObject, "Disable original boss");
            source.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = area;
        }
        catch (Exception)
        {
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static Transform Point(Transform parent, string name, Vector3 position)
    {
        Transform point = new GameObject(name).transform;
        point.SetParent(parent, false);
        point.position = position;
        return point;
    }
}
