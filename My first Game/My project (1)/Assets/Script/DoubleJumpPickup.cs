using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoubleJumpPickup : Interactable
{
    public override string InteractionLabel => "\uB354\uBE14\uC810\uD504 \uD68D\uB4DD";
    private bool collected;

    private void OnEnable()
    {
        foreach (TextMesh label in GetComponentsInChildren<TextMesh>(true))
        {
            Renderer labelRenderer = label.GetComponent<Renderer>();
            if (labelRenderer != null) labelRenderer.enabled = false;
        }
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public override bool CanInteract(PlayerMove player)
    {
        return isActiveAndEnabled && !collected && player != null && !player.HasDoubleJump;
    }

    public override void Interact(PlayerMove player)
    {
        if (!CanInteract(player)) return;
        GameSfx.Play(GameSound.Pickup, transform.position);
        collected = true;
        player.UnlockDoubleJump();
        Debug.Log("Double jump unlocked!", player);
        gameObject.SetActive(false);
    }
}
