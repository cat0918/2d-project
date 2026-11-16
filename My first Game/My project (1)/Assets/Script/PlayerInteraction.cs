using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMove))]
public class PlayerInteraction : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    [Min(0.1f)] public float interactRadius = 1.5f;
    public LayerMask interactLayers = ~0;
    public Interactable CurrentTarget { get; private set; }

    private PlayerMove player;
    private PlayerHealth health;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    private void Awake()
    {
        player = GetComponent<PlayerMove>();
        health = GetComponent<PlayerHealth>();
        if (GetComponent<InteractionPromptUI>() == null) gameObject.AddComponent<InteractionPromptUI>();
    }

    private void Update()
    {
        CurrentTarget = null;
        if (Time.timeScale <= 0f || GameFlowScreen.BlocksInput || !player.isActiveAndEnabled || (health != null && health.IsDead)) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(interactLayers);
        filter.useTriggers = true;
        Physics2D.OverlapCircle(transform.position, interactRadius, filter, hits);

        Interactable nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Collider2D hit in hits)
        {
            Interactable candidate = hit.GetComponentInParent<Interactable>();
            if (candidate == null || !candidate.isActiveAndEnabled || !candidate.CanInteract(player)) continue;

            float distance = ((Vector2)transform.position - hit.ClosestPoint(transform.position)).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearest = candidate;
            nearestDistance = distance;
        }

        CurrentTarget = nearest;
        if (nearest != null && Input.GetKeyDown(interactKey))
        {
            nearest.Interact(player);
            CurrentTarget = null;
        }
    }

    private void OnDisable() { CurrentTarget = null; }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
