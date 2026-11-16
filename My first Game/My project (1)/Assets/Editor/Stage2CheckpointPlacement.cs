using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2CheckpointPlacement
{
    public static void Move()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform course = all.SingleOrDefault(t => t.name == "Stage2_TrapCourse");
        Transform destination = all.SingleOrDefault(t => t.name == "Floor_Stage2_Start");
        Transform group = course != null ? course.Find("ManualSavePoints/StartSavePoint") : null;
        BoxCollider2D floor = destination != null ? destination.GetComponent<BoxCollider2D>() : null;
        Stage2Checkpoint checkpoint = group != null ? group.GetComponentInChildren<Stage2Checkpoint>() : null;
        if (floor == null || !floor.isActiveAndEnabled || floor.isTrigger || checkpoint == null ||
            checkpoint.respawnPoint == null || !checkpoint.respawnPoint.IsChildOf(group) ||
            checkpoint.course == null || checkpoint.course.transform != course)
            throw new InvalidOperationException("Expected Stage 2 StartSavePoint with its own respawn point and Floor_Stage2_Start.");
        Transform visual = checkpoint.transform.Find("CheckpointMonumentVisual");
        if (visual == null) visual = group.Find("SavePedestal");
        SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (renderer == null) throw new InvalidOperationException("Checkpoint base artwork was not found.");
        Bounds target = floor.bounds;
        if (target.size.x < renderer.bounds.size.x + 0.5f)
            throw new InvalidOperationException("Destination floor is too narrow for the checkpoint.");
        Vector3 delta = new Vector3(target.center.x - renderer.bounds.center.x,
            target.max.y - renderer.bounds.min.y, 0);
        Undo.RecordObject(group, "Move Stage 2 start checkpoint to lower floor");
        // All artwork, prompt, interaction collider and respawn point travel together.
        group.position += delta;
        PrefabUtility.RecordPrefabInstancePropertyModifications(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = checkpoint.gameObject;
        SceneView.RepaintAll();
        Debug.Log("Stage 2 start checkpoint moved to the middle of Floor_Stage2_Start. Respawn reference retained; traps and other checkpoints unchanged.", checkpoint);
    }
}
