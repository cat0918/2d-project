using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastleEntranceGate : Interactable
{
    public Transform movingGate;
    public Collider2D gateCollider;
    public string gateId = "CastleEntrance";
    [Min(0.1f)] public float openDuration = 1.6f;
    public float liftHeight = 6.3f;
    private Vector3 closedPosition;
    private bool opened;
    private static readonly HashSet<string> OpenGates = new HashSet<string>();
    private string SaveKey => gameObject.scene.path + ":" + gateId;
    public override string InteractionLabel => "\uC131\uBB38 \uC5F4\uAE30";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { OpenGates.Clear(); }

    private void Awake()
    {
        if (movingGate == null) return;
        closedPosition = movingGate.localPosition;
        if (OpenGates.Contains(SaveKey))
        {
            opened = true;
            FinishOpening();
        }
    }

    public override bool CanInteract(PlayerMove player)
    {
        if (!isActiveAndEnabled || opened || movingGate == null || player == null) return false;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        return health == null || !health.IsDead;
    }

    public override void Interact(PlayerMove player)
    {
        if (!CanInteract(player)) return;
        GameSfx.Play(GameSound.Gate, transform.position);
        opened = true;
        OpenGates.Add(SaveKey);
        StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        float elapsed = 0f;
        float nextDust = 0f;
        CameraFollow2D follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        if (follow != null) follow.PlayHitShake();
        while (elapsed < Mathf.Max(0.1f, openDuration))
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.1f, openDuration));
            movingGate.localPosition = closedPosition + Vector3.up * liftHeight * t;
            if (elapsed >= nextDust && gateCollider != null)
            {
                Bounds bounds = gateCollider.bounds;
                MovementDustBurst.Play(new Vector2(bounds.center.x, bounds.min.y), true, false, 1.2f,
                    new Color(0.6f, 0.65f, 0.66f, 0.6f));
                nextDust = elapsed + 0.3f;
            }
            yield return null;
        }
        FinishOpening();
    }

    private void FinishOpening()
    {
        if (movingGate != null) movingGate.localPosition = closedPosition + Vector3.up * liftHeight;
        if (gateCollider != null) gateCollider.enabled = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (opened) FinishOpening();
    }
}
