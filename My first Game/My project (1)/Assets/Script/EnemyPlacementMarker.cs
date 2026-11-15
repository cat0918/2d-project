using UnityEngine;

// Authoring guide only: no enemy, collider, or runtime spawning is created.
public class EnemyPlacementMarker : MonoBehaviour
{
    public string markerId;
    [TextArea] public string placementNote;
    [Min(0f)] public float suggestedPatrolWidth = 3f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.65f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(0.65f, 1.1f, 0.1f));
        Vector3 feet = transform.position + Vector3.down * 0.55f;
        Gizmos.DrawLine(feet + Vector3.left * suggestedPatrolWidth / 2f,
            feet + Vector3.right * suggestedPatrolWidth / 2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, markerId);
#endif
    }
}
