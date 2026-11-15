using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Parts = Stage2CourseBuilder;

public static class Stage3CourseBuilder
{
    private const string RootName = "Stage3_ExplorationCourse";

    [MenuItem("Tools/Level Design/Build Stage 3 Exploration Course")]
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
            EditorUtility.DisplayDialog("Stage 3", "The Stage 3 course already exists. No duplicate was created.", "OK");
            return;
        }
        Transform gate = objects.FirstOrDefault(t => t.name == "DoubleJumpGate" || t.name == "Platform_DoubleJumpGate");
        Collider2D anchor = gate != null ? gate.GetComponent<Collider2D>() : null;
        if (anchor == null && Selection.activeGameObject != null)
            anchor = Selection.activeGameObject.GetComponent<Collider2D>();
        PlayerMove player = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<PlayerMove>())
            .FirstOrDefault(p => p.isActiveAndEnabled);
        if (anchor == null || anchor.isTrigger || anchor.attachedRigidbody != null || anchor.gameObject.scene != scene ||
            player == null || (player.groundLayer.value & (1 << anchor.gameObject.layer)) == 0)
        {
            EditorUtility.DisplayDialog("Stage 3", "Select the double-jump entrance platform with a solid Ground collider, then run this menu again.", "OK");
            return;
        }
        // Build upward, then left: keep the right-hand Stage 2 branch untouched.
        Vector3 origin = new Vector3(anchor.bounds.center.x, anchor.bounds.max.y, 0);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Stage 3 exploration course");
        try
        {
            GameObject layout = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(layout, "Create Stage 3");
            layout.transform.position = origin;
            Stage2Course course = Undo.AddComponent<Stage2Course>(layout);
            Parts.PrepareLayout(layout.transform, anchor.gameObject.layer, course);

            Transform entry = Parts.Group("01_Entry_Save");
            Parts.Floor(entry, "EntryShelf", 3, 9, 3.4f);
            course.entryCheckpoint = Parts.Checkpoint(entry, 5.5f, 3.4f);
            Parts.Label(entry, "STAGE 3 / DOUBLE JUMP\nRETURN VIA THE ENTRANCE BELOW", 6, 6);

            Transform tower = Parts.Group("02_First_Ascent");
            Parts.Floor(tower, "Climb_A", 11, 15, 6.5f);
            Parts.Floor(tower, "Climb_B", 3, 8, 9.6f);
            Parts.Floor(tower, "CombatShelf_A", 10, 17, 12.7f);
            Parts.Floor(tower, "Climb_C", 3, 8, 15.8f);
            Parts.Floor(tower, "CombatShelf_B", 10, 17, 18.9f);
            Parts.Floor(tower, "Summit", -6, 8, 22);
            Marker(layout.transform, tower, "E01", 14, 12.7f, 3, "Wide ascent shelf. Leave the left landing clear; choose an enemy later.");
            Marker(layout.transform, tower, "E02", 15.5f, 18.9f, 1.5f, "Upper shelf. Falling trap is on the left; keep this enemy on the right.");
            Drop(tower, "TowerDrop", 11.5f, 18.9f, 0);
            Parts.Label(tower, "01 / VERTICAL ASCENT", 5, 25);

            Transform gallery = Parts.Group("03_Long_Gallery");
            Parts.Floor(gallery, "CombatRunway", -20, -6, 22);
            Marker(layout.transform, gallery, "E03", -10, 22, 4, "First gallery encounter; no trap directly under the landing.");
            Marker(layout.transform, gallery, "E04", -18, 22, 3, "Second encounter. Tune range so both enemies do not engage at once.");
            Parts.Floor(gallery, "ArrowRunway", -30, -20, 22);
            Parts.Emitter(gallery, "GalleryArrow", new Vector2(-20.6f, 22.65f), Vector2.left,
                Parts.ArrowSprite, new Vector2(0.8f, 0.22f), 8, 3, 0.8f, 0.5f, 1.2f, new Vector2(-20.6f, 23.2f));
            Parts.Spikes(gallery, -34, -30, 20);
            Parts.Label(gallery, "02 / LONG GALLERY + ARROWS", -18, 26);

            Transform middle = Parts.Group("04_Middle_Save");
            Parts.Floor(middle, "RestLanding", -41, -34, 22.8f);
            Parts.Checkpoint(middle, -37.5f, 22.8f);

            Transform gauntlet = Parts.Group("05_Mixed_Hazards");
            Parts.Floor(gauntlet, "DropRunway", -51, -41, 22.8f);
            Drop(gauntlet, "Drop_A", -44.5f, 22.8f, 0);
            Drop(gauntlet, "Drop_B", -49, 22.8f, 1.5f);
            Parts.Spikes(gauntlet, -54, -51, 20.8f);
            Parts.Floor(gauntlet, "CombatLanding", -62, -54, 23.4f);
            Marker(layout.transform, gauntlet, "E05", -59, 23.4f, 3, "Encounter after the spike gap; keep the right landing edge clear.");
            Parts.Label(gauntlet, "03 / FALLING TRAPS + SPIKES", -49, 31);

            Transform upper = Parts.Group("06_Second_Ascent");
            Parts.Floor(upper, "Climb_A", -70, -65, 26.6f);
            Parts.Floor(upper, "Climb_B", -62, -55, 29.8f);
            Parts.Floor(upper, "CombatShelf", -71, -64, 33);
            Parts.Floor(upper, "Climb_C", -61, -54, 36.2f);
            Parts.Floor(upper, "UpperSummit", -74, -63, 39.4f);
            Marker(layout.transform, upper, "E06", -58, 29.8f, 2, "Small upper encounter. Select a type that does not cover the entire climb.");
            Marker(layout.transform, upper, "E07", -68.5f, 33, 2, "Wide shelf for a stronger enemy variant, away from the landing edge.");
            Parts.Label(upper, "04 / UPPER ASCENT", -63, 43);

            Transform approach = Parts.Group("07_Boss_Approach");
            Parts.Floor(approach, "LongApproach", -91, -74, 39.4f);
            Marker(layout.transform, approach, "E08", -79, 39.4f, 4, "Final corridor encounter. Keep the far left clear for the future boss room.");
            Parts.Label(approach, "BOSS AREA CONNECTS LEFT\nNO BOSS SPAWN YET", -86, 42.5f);
            Transform bossAnchor = new GameObject("BossConnectionAnchor").transform;
            bossAnchor.SetParent(approach, false);
            bossAnchor.position = origin + new Vector3(-91, 39.4f, 0);

            // Catch falls only in the far-left upper course, never across Stage 1 or its gate.
            GameObject catchZone = Parts.Visual(layout.transform, "UpperFallReturnZone", new Vector2(-55, 15),
                new Vector2(76, 2), Parts.BlockSprite, new Color(1, 0.2f, 0.1f, 0.05f));
            catchZone.AddComponent<BoxCollider2D>().isTrigger = true;
            catchZone.AddComponent<StageTrapHazard>().course = course;

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = layout;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Frame(new Bounds(origin + new Vector3(-37, 23, 0), new Vector3(116, 52, 1)), false);
            }
            Debug.Log("Stage 3 built: two save structures, eight editor-only enemy markers, three falling traps and one arrow trap. Save and playtest both directions.", layout);
        }
        catch (Exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
        finally { Undo.CollapseUndoOperations(undoGroup); }
    }

    private static void Drop(Transform parent, string name, float x, float floor, float delay)
    {
        Parts.Emitter(parent, name, new Vector2(x, floor + 6.5f), Vector2.down, Parts.BlockSprite,
            new Vector2(0.8f, 0.8f), 14, 3.8f, 0.9f, delay, 1.5f, new Vector2(x, floor + 0.12f));
    }

    private static void Marker(Transform layout, Transform parent, string id, float x, float floor, float patrolWidth, string note)
    {
        GameObject go = new GameObject(id + "_EnemyPosition");
        go.transform.SetParent(parent, false);
        go.transform.position = layout.TransformPoint(new Vector3(x, floor + 0.6f, 0));
        EnemyPlacementMarker marker = go.AddComponent<EnemyPlacementMarker>();
        marker.markerId = id;
        marker.placementNote = note;
        marker.suggestedPatrolWidth = patrolWidth;
    }
}
