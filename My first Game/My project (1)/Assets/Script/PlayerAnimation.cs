using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMove), typeof(Rigidbody2D))]
public class PlayerAnimation : MonoBehaviour
{
    public SPUM_Prefabs visuals;
    [Min(0f)] public float movementThreshold = 0.1f;

    [Header("Hit Feedback")]
    [Min(0)] public int damagedAnimationIndex = 0;
    public bool IsReactingToHit => isActiveAndEnabled && Time.time < hitReactionEnds;

    private PlayerHealth health;
    private float hitReactionEnds;

    private PlayerMove movement;
    private Rigidbody2D body;
    private PlayerState? currentState;
    private bool initialized;
    private PlayerAttack attack;

    private void Awake()
    {
        movement = GetComponent<PlayerMove>();
        body = GetComponent<Rigidbody2D>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
        if (visuals == null) visuals = GetComponentInChildren<SPUM_Prefabs>();
    }

    private void Start()
    {
        if (visuals == null || visuals._anim == null ||
            visuals._anim.runtimeAnimatorController == null)
        {
            Debug.LogError("PlayerAnimation needs SPUM_Prefabs with an Animator controller.", this);
            enabled = false;
            return;
        }

        if (visuals.IDLE_List.Count == 0 || visuals.MOVE_List.Count == 0)
        {
            visuals.PopulateAnimationLists();
        }

        if (visuals.IDLE_List.Count == 0 || visuals.MOVE_List.Count == 0 ||
            visuals.IDLE_List[0] == null || visuals.MOVE_List[0] == null)
        {
            Debug.LogError("PlayerAnimation needs valid IDLE and MOVE clips in SPUM_Prefabs.", this);
            enabled = false;
            return;
        }

        visuals.OverrideControllerInit();
        initialized = true;
        SetState(PlayerState.IDLE);
    }

    private void OnEnable()
    {
        currentState = null;
        if (health != null) health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamaged -= HandleDamaged;
        hitReactionEnds = 0f;
    }

    private void HandleDamaged()
    {
        if (attack != null) attack.CancelAttack();
        if (!initialized) return;
        if (damagedAnimationIndex < 0 || damagedAnimationIndex >= visuals.DAMAGED_List.Count ||
            visuals.DAMAGED_List[damagedAnimationIndex] == null)
        {
            Debug.LogWarning("Select a valid Damaged Animation Index from SPUM DAMAGED_List.", this);
            return;
        }

        visuals._anim.ResetTrigger("2_Attack");
        visuals.PlayAnimation(PlayerState.DAMAGED, damagedAnimationIndex);
        hitReactionEnds = Time.time + visuals.DAMAGED_List[damagedAnimationIndex].length /
            Mathf.Max(0.01f, visuals._anim.speed);
        currentState = null;
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        if (IsReactingToHit) return;
        if (attack != null && attack.IsAttacking) return;

        bool grounded = movement.groundCheck != null &&
            Physics2D.OverlapCircle(movement.groundCheck.position,
                movement.groundCheckRadius, movement.groundLayer) != null;
        bool walking = movement.isActiveAndEnabled && grounded &&
            Mathf.Abs(body.linearVelocity.x) > movementThreshold &&
            Mathf.Abs(body.linearVelocity.y) < 0.1f;

        SetState(walking ? PlayerState.MOVE : PlayerState.IDLE);
    }

    public float PlayAttack(int clipIndex)
    {
        if (!initialized || !isActiveAndEnabled || IsReactingToHit) return 0f;
        if (clipIndex < 0 || clipIndex >= visuals.ATTACK_List.Count ||
            visuals.ATTACK_List[clipIndex] == null)
        {
            Debug.LogWarning("Select a valid Attack Animation Index from SPUM ATTACK_List.", this);
            return 0f;
        }

        visuals.PlayAnimation(PlayerState.ATTACK, clipIndex);
        currentState = null;
        return visuals.ATTACK_List[clipIndex].length / Mathf.Max(0.01f, visuals._anim.speed);
    }

    private void SetState(PlayerState state)
    {
        // Only change clips on transitions so looping animations are not restarted.
        if (currentState == state) return;
        visuals.PlayAnimation(state, 0);
        currentState = state;
    }
}
