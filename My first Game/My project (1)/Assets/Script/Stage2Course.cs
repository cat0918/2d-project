using UnityEngine;

public class Stage2Course : MonoBehaviour
{
    public Transform entryCheckpoint;
    private Transform checkpoint;
    public Transform ActiveCheckpoint => checkpoint;
    private float nextResetTime;

    public void SetCheckpoint(Transform point)
    {
        if (point == null) return;
        checkpoint = point;
        RunCheckpointState.Save(point);
    }

    public void ReturnPlayer(PlayerHealth player)
    {
        if (player == null || player.IsDead || Time.time < nextResetTime) return;
        Transform destination = checkpoint != null ? checkpoint : entryCheckpoint;
        if (destination == null) return;
        nextResetTime = Time.time + 0.3f;
        player.TakeDamage(1, player.transform.position + Vector3.right);
        if (player.IsDead) return;
        PlayerMove movement = player.GetComponent<PlayerMove>();
        if (movement != null) movement.BeginKnockback(0.1f);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = destination.position;
        }
        player.transform.position = destination.position;
    }
}
