using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StageTrapHazard : MonoBehaviour
{
    public Stage2Course course;

    private void OnTriggerEnter2D(Collider2D other) { Touch(other); }
    private void OnTriggerStay2D(Collider2D other) { Touch(other); }

    private void Touch(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null && course != null) course.ReturnPlayer(player);
    }
}
