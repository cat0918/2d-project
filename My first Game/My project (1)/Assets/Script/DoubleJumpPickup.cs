using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoubleJumpPickup : Interactable
{
    private bool collected;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public override bool CanInteract(PlayerMove player)
    {
        return !collected && player != null && !player.HasDoubleJump;
    }

    public override void Interact(PlayerMove player)
    {
        if (!CanInteract(player)) return;
        collected = true;
        player.UnlockDoubleJump();
        Debug.Log("Double jump unlocked!", player);
    }
}
