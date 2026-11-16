using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RangedFeetColliderFix
{
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        var enemies = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<EnemyRangedAI>(true)).ToArray();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fit ranged colliders to feet");
        int changed = 0;
        foreach (EnemyRangedAI enemy in enemies)
        {
            if (enemy.GetComponent<BossController>() != null) continue;
            CapsuleCollider2D capsule = enemy.GetComponent<CapsuleCollider2D>();
            var feet = enemy.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r => r.enabled && r.sprite != null && r.name.ToLowerInvariant().Contains("foot")).ToArray();
            if (capsule == null || capsule.direction != CapsuleDirection2D.Vertical || feet.Length == 0)
            {
                Debug.LogWarning("Skipped: needs a vertical capsule and foot sprites: " + enemy.name, enemy);
                continue;
            }
            // Transform sprite geometry directly: inactive source templates have no physics bounds.
            float bottom = float.PositiveInfinity;
            foreach (SpriteRenderer foot in feet)
            {
                Bounds bounds = foot.sprite.bounds;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                {
                    Vector3 corner = new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                        y == 0 ? bounds.min.y : bounds.max.y, 0f);
                    bottom = Mathf.Min(bottom, enemy.transform.InverseTransformPoint(foot.transform.TransformPoint(corner)).y);
                }
            }
            float oldBottom = capsule.offset.y - capsule.size.y * 0.5f;
            float top = capsule.offset.y + capsule.size.y * 0.5f;
            float height = top - bottom;
            if (height < capsule.size.x || Mathf.Abs(bottom - oldBottom) > 0.75f)
            {
                Debug.LogWarning("Skipped unusual foot/capsule alignment: " + enemy.name, enemy);
                continue;
            }
            Undo.RecordObject(capsule, "Align capsule bottom");
            capsule.size = new Vector2(capsule.size.x, height);
            capsule.offset = new Vector2(capsule.offset.x, (top + bottom) * 0.5f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(capsule);
            if (enemy.groundCheck != null && enemy.groundCheck.IsChildOf(enemy.transform))
            {
                Undo.RecordObject(enemy.groundCheck, "Align ground probe");
                Vector3 local = enemy.transform.InverseTransformPoint(enemy.groundCheck.position);
                local.y = bottom + 0.04f;
                enemy.groundCheck.position = enemy.transform.TransformPoint(local);
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.groundCheck);
            }
            Debug.Log($"{enemy.name}: collider bottom {oldBottom:F3} -> {bottom:F3}; width/top preserved.", enemy);
            changed++;
        }
        Physics2D.SyncTransforms();
        Undo.CollapseUndoOperations(group);
        if (changed > 0) EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Ranged Collider Fit", $"Adjusted {changed} ranged enemies (including inactive templates). Test in Play mode, then save the scene. Ctrl+Z undoes this adjustment.", "OK");
    }
}
