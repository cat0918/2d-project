using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StageEnemyPlacementBuilder
{
    private const string RootName = "StageEnemies_FirstPass";
    private class Placement
    {
        public string name;
        public bool ranged;
        public Collider2D floor;
        public float x;
        public float range;
    }

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform existing = all.FirstOrDefault(t => t.name == RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Enemies", "The first-pass group already exists. No enemies were duplicated.", "OK");
            return;
        }
        EnemyMeleeAI melee = all.Where(t => t.name == "EnemyMelee").Select(t => t.GetComponent<EnemyMeleeAI>()).FirstOrDefault(a => a != null);
        EnemyRangedAI ranged = all.Where(t => t.name == "EnemyRanged").Select(t => t.GetComponent<EnemyRangedAI>()).FirstOrDefault(a => a != null);
        PlayerMove player = all.Select(t => t.GetComponent<PlayerMove>()).FirstOrDefault(p => p != null && p.isActiveAndEnabled);
        if (melee == null || ranged == null || player == null)
            throw new InvalidOperationException("Expected configured EnemyMelee, EnemyRanged and an active PlayerMove.");
        ValidateSource(melee.gameObject, melee.groundCheck, melee.wallCheck);
        ValidateSource(ranged.gameObject, ranged.groundCheck, ranged.wallCheck);
        if (ranged.projectilePrefab == null || ranged.firePoint == null || !ranged.firePoint.IsChildOf(ranged.transform))
            throw new InvalidOperationException("EnemyRanged needs its projectile prefab and an internal fire point.");
        Transform stage1 = all.Single(t => t.name == "Stage1");
        Transform stage2 = all.Single(t => t.name == "Stage2_TrapCourse");
        Transform stage3 = all.Single(t => t.name == "Stage3_ExplorationCourse");
        var plan = new List<Placement>();
        Collider2D first = Floor(stage1, "RoomHorizon/Floor_Horizon");
        Add(plan, "S1_Melee_01", false, first, 0.28f, 3.5f);
        Add(plan, "S1_Melee_02", false, first, 0.57f, 3.5f);
        Add(plan, "S1_Melee_03", false, first, 0.84f, 3.5f);
        Add(plan, "S2_Melee_01", false, Floor(stage2, "03_SafeLanding/SafeFloor"), 0.55f, 3f);
        Add(plan, "S2_Ranged_01", true, Floor(stage2, "07_DoubleJumpReward/ShrineFloor"), 0.28f, 3.5f);
        AddMarker(plan, stage3, "E01", false, "02_First_Ascent/CombatShelf_A", 3f);
        AddMarker(plan, stage3, "E03", true, "03_Long_Gallery/CombatRunway", 4.5f);
        AddMarker(plan, stage3, "E05", false, "05_Mixed_Hazards/CombatLanding", 3.5f);
        AddMarker(plan, stage3, "E06", true, "06_Second_Ascent/Climb_B", 3.5f);
        AddMarker(plan, stage3, "E07", false, "06_Second_Ascent/CombatShelf", 3f);
        AddMarker(plan, stage3, "E08", false, "07_Boss_Approach/LongApproach", 4f);
        string[] testNames = { "EnemyMelee", "EnemyMelee_Stage1", "EnemyRanged", "EnemyNew" };
        GameObject[] oldTests = all.Where(t => testNames.Contains(t.name) &&
            (t.GetComponent<EnemyMeleeAI>() != null || t.GetComponent<EnemyRangedAI>() != null))
            .Select(t => t.gameObject).Distinct().ToArray();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place stage enemies first pass");
        try
        {
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create enemy placement group");
            foreach (Placement placement in plan)
            {
                GameObject source = placement.ranged ? ranged.gameObject : melee.gameObject;
                GameObject actor = UnityEngine.Object.Instantiate(source, root.transform, true);
                Undo.RegisterCreatedObjectUndo(actor, "Place enemy");
                actor.name = placement.name;
                actor.SetActive(true);
                actor.transform.position = new Vector3(placement.x, placement.floor.bounds.max.y + 2, 0);
                actor.transform.rotation = Quaternion.identity;
                foreach (EnemyPatrol legacy in actor.GetComponents<EnemyPatrol>()) legacy.enabled = false;
                Rigidbody2D body = actor.GetComponent<Rigidbody2D>();
                body.simulated = true;
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0;
                EnemyMeleeAI m = actor.GetComponent<EnemyMeleeAI>();
                EnemyRangedAI r = actor.GetComponent<EnemyRangedAI>();
                float patrolTime = Mathf.Clamp((placement.floor.bounds.size.x - 2) / 4, 0.5f, 1.6f);
                if (m != null)
                {
                    m.enabled = !placement.ranged;
                    m.target = player.transform;
                    m.groundLayer = player.groundLayer;
                    m.chaseRange = placement.range;
                    m.patrolEnabled = true;
                    m.patrolSpeed = 0.7f;
                    m.patrolTurnInterval = patrolTime;
                }
                if (r != null)
                {
                    r.enabled = placement.ranged;
                    r.target = player.transform;
                    r.groundLayer = player.groundLayer;
                    r.detectRange = placement.range;
                    r.shootCooldown = Mathf.Max(r.shootCooldown, 1.6f);
                    r.patrolEnabled = true;
                    r.patrolSpeed = 0.55f;
                    r.patrolTurnInterval = patrolTime;
                }
                Physics2D.SyncTransforms();
                Collider2D[] solids = actor.GetComponentsInChildren<Collider2D>().Where(c => c.enabled && !c.isTrigger).ToArray();
                if (solids.Length == 0) throw new InvalidOperationException("Enemy has no active solid collider.");
                float bottom = solids.Min(c => c.bounds.min.y);
                actor.transform.position += Vector3.up * (placement.floor.bounds.max.y + 0.04f - bottom);
            }
            foreach (GameObject old in oldTests)
            {
                Undo.RecordObject(old, "Disable original test enemy");
                old.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(old);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            Debug.Log("Placed 11 enemies: Stage 1 melee 3; Stage 2 melee 1/ranged 1; Stage 3 melee 4/ranged 2. Test sources disabled, not deleted. Boss unchanged.", root);
        }
        catch
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally { Undo.CollapseUndoOperations(group); }
    }

    private static void ValidateSource(GameObject source, Transform ground, Transform wall)
    {
        if (source.GetComponent<Rigidbody2D>() == null || source.GetComponent<EnemyHealth>() == null ||
            source.GetComponent<EnemyAnimation>() == null || ground == null || wall == null ||
            !ground.IsChildOf(source.transform) || !wall.IsChildOf(source.transform))
            throw new InvalidOperationException("Incomplete enemy template: " + source.name);
    }

    private static Collider2D Floor(Transform root, string path)
    {
        Collider2D floor = root.Find(path)?.GetComponent<Collider2D>();
        if (floor == null || !floor.isActiveAndEnabled || floor.isTrigger || floor.bounds.size.x < 3)
            throw new InvalidOperationException("Missing or too narrow terrain: " + root.name + "/" + path);
        return floor;
    }

    private static void Add(List<Placement> plan, string name, bool ranged, Collider2D floor, float fraction, float range)
    {
        plan.Add(new Placement { name = name, ranged = ranged, floor = floor,
            x = Mathf.Lerp(floor.bounds.min.x + 1, floor.bounds.max.x - 1, fraction), range = range });
    }

    private static void AddMarker(List<Placement> plan, Transform stage, string id, bool ranged, string floorPath, float range)
    {
        EnemyPlacementMarker marker = stage.GetComponentsInChildren<EnemyPlacementMarker>(true).Single(m => m.markerId == id);
        Collider2D floor = Floor(stage, floorPath);
        plan.Add(new Placement { name = "S3_" + (ranged ? "Ranged_" : "Melee_") + id, ranged = ranged,
            floor = floor, x = Mathf.Clamp(marker.transform.position.x, floor.bounds.min.x + 1, floor.bounds.max.x - 1), range = range });
    }
}
