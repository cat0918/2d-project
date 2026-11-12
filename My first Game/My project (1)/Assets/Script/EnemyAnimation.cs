using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
public class EnemyAnimation : MonoBehaviour
{
    public SPUM_Prefabs visuals;
    [Min(0)] public int attackAnimationIndex;
    [Min(0)] public int damagedAnimationIndex;
    public bool IsReactingToHit => isActiveAndEnabled && Time.time < reactionEnds;

    private EnemyHealth health;
    private Rigidbody2D body;
    private EnemyMeleeAI melee;
    private bool ready;
    private float reactionEnds;
    private PlayerState? state;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        body = GetComponent<Rigidbody2D>();
        melee = GetComponent<EnemyMeleeAI>();
        if (visuals == null) visuals = GetComponentInChildren<SPUM_Prefabs>();
    }

    private void OnEnable()
    {
        state = null;
        health.OnDamaged += HandleDamage;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamage;
        reactionEnds = 0f;
    }

    private void Start()
    {
        if (visuals == null || visuals._anim == null || visuals._anim.runtimeAnimatorController == null)
        {
            Debug.LogError("EnemyAnimation needs SPUM_Prefabs and its Animator controller.", this);
            enabled = false;
            return;
        }
        if (visuals.IDLE_List.Count == 0 || visuals.MOVE_List.Count == 0)
            visuals.PopulateAnimationLists();
        if (visuals.IDLE_List.Count == 0 || visuals.MOVE_List.Count == 0 ||
            visuals.IDLE_List[0] == null || visuals.MOVE_List[0] == null)
        {
            Debug.LogError("EnemyAnimation needs IDLE and MOVE clips.", this);
            enabled = false;
            return;
        }
        visuals.OverrideControllerInit();
        ready = true;
    }

    private void LateUpdate()
    {
        if (!ready || health.IsDead || IsReactingToHit || (melee != null && melee.IsAttacking)) return;
        PlayerState next = Mathf.Abs(body.linearVelocity.x) > 0.1f ? PlayerState.MOVE : PlayerState.IDLE;
        if (state == next) return;
        visuals.PlayAnimation(next, 0);
        state = next;
    }

    public float PlayAttack()
    {
        if (!ready || !isActiveAndEnabled || IsReactingToHit) return 0f;
        return PlayAction(PlayerState.ATTACK, attackAnimationIndex);
    }

    private void HandleDamage()
    {
        if (melee != null) melee.CancelAttack();
        if (!ready) return;
        visuals._anim.ResetTrigger("2_Attack");
        reactionEnds = Time.time + PlayAction(PlayerState.DAMAGED, damagedAnimationIndex);
    }

    private float PlayAction(PlayerState action, int index)
    {
        var clips = action == PlayerState.ATTACK ? visuals.ATTACK_List : visuals.DAMAGED_List;
        if (index < 0 || index >= clips.Count || clips[index] == null)
        {
            Debug.LogWarning("EnemyAnimation has an invalid clip index for " + action, this);
            return 0f;
        }
        visuals.PlayAnimation(action, index);
        state = null;
        return clips[index].length / Mathf.Max(0.01f, visuals._anim.speed);
    }
}
