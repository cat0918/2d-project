using UnityEngine;

public class Stage2Checkpoint : Interactable
{
    public Stage2Course course;
    public Transform respawnPoint;
    public SpriteRenderer indicator;
    public TextMesh statusLabel;

    public override bool CanInteract(PlayerMove player)
    {
        if (!isActiveAndEnabled || player == null || course == null || respawnPoint == null) return false;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        return health == null || !health.IsDead;
    }

    public override void Interact(PlayerMove player)
    {
        if (!CanInteract(player)) return;
        course.SetCheckpoint(respawnPoint);
        Debug.Log("Checkpoint saved for this run.", this);
    }

    private void Update()
    {
        bool selected = course != null && course.ActiveCheckpoint == respawnPoint;
        if (indicator != null)
            indicator.color = selected ? new Color(0.35f, 1f, 0.7f) : new Color(0.5f, 0.65f, 0.85f);
        if (statusLabel != null)
            statusLabel.text = selected ? "CHECKPOINT SAVED" : "E : SAVE CHECKPOINT";
    }
}
