using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
public class EnemyAnimation : MonoBehaviour
{
    public SPUM_Prefabs visuals;
    [Min(0)] public int attackAnimationIndex;
    [Min(0)] public int damagedAnimationIndex;
    [Min(0)] public int deathAnimationIndex;
    public bool IsReactingToHit => isActiveAndEnabled && Time.time < reactionEnds;

    private EnemyHealth health;
    private Rigidbody2D body;
    private EnemyMeleeAI melee;
    private EnemyRangedAI ranged;
    private BossController boss;
    private bool ready;
    private float reactionEnds;
    private PlayerState? state;
    private float defaultAnimationSpeed = 1f;
    private bool deathPlaying;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        body = GetComponent<Rigidbody2D>();
        melee = GetComponent<EnemyMeleeAI>();
        ranged = GetComponent<EnemyRangedAI>();
        boss = GetComponent<BossController>();
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
        RestoreAnimationSpeed();
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
        defaultAnimationSpeed = visuals._anim.speed;
        ready = true;
    }

    private void LateUpdate()
    {
        if (!ready || health.IsDead || IsReactingToHit || (melee != null && melee.IsAttacking)) return;
        if (ranged != null && ranged.IsShooting) return;
        if (boss != null && boss.IsAttacking) return;
        PlayerState next = Mathf.Abs(body.linearVelocity.x) > 0.1f ? PlayerState.MOVE : PlayerState.IDLE;
        if (state == next) return;
        RestoreAnimationSpeed();
        visuals.PlayAnimation(next, 0);
        state = next;
    }

    public float PlayAttack()
    {
        if (!ready || !isActiveAndEnabled || health.IsDead || IsReactingToHit) return 0f;
        return PlayAction(PlayerState.ATTACK, attackAnimationIndex);
    }

    private void HandleDamage()
    {
        if (boss != null) boss.CancelAttack();
        if (melee != null) melee.CancelAttack();
        if (ranged != null) ranged.CancelShot();
        if (!ready) return;
        visuals._anim.ResetTrigger("2_Attack");
        reactionEnds = Time.time + PlayAction(PlayerState.DAMAGED, damagedAnimationIndex);
    }

    private float PlayAction(PlayerState action, int index, float speedMultiplier = 1f)
    {
        var clips = action == PlayerState.OTHER ? visuals.OTHER_List :
            action == PlayerState.ATTACK ? visuals.ATTACK_List : visuals.DAMAGED_List;
        if (index < 0 || index >= clips.Count || clips[index] == null)
        {
            Debug.LogWarning("EnemyAnimation has an invalid clip index for " + action, this);
            return 0f;
        }
        visuals._anim.speed = defaultAnimationSpeed * Mathf.Clamp(speedMultiplier, 0.1f, 2f);
        visuals.PlayAnimation(action, index);
        state = null;
        return clips[index].length / Mathf.Max(0.01f, visuals._anim.speed);
    }

    public float PlayBossAction(PlayerState action, int index, float speedMultiplier = 1f)
    {
        if (!ready || !isActiveAndEnabled || health.IsDead || IsReactingToHit) return 0f;
        return PlayAction(action, index, speedMultiplier);
    }

    public void PlayRecovery()
    {
        if (!ready || !isActiveAndEnabled || health.IsDead) return;
        RestoreAnimationSpeed();
        visuals._anim.ResetTrigger("2_Attack");
        visuals._anim.ResetTrigger("6_Other");
        visuals.PlayAnimation(PlayerState.IDLE, 0);
        visuals._anim.Play("IDLE", 0, 0f);
        state = PlayerState.IDLE;
    }

    public float PlayDeath()
    {
        if (!ready || !isActiveAndEnabled) return 0f;
        if (deathAnimationIndex < 0 || deathAnimationIndex >= visuals.DEATH_List.Count ||
            visuals.DEATH_List[deathAnimationIndex] == null)
        {
            Debug.LogWarning("Select a valid Death Animation Index from SPUM DEATH_List.", this);
            return 0f;
        }
        RestoreAnimationSpeed();
        foreach (AnimatorControllerParameter parameter in visuals._anim.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                visuals._anim.ResetTrigger(parameter.name);
        visuals.PlayAnimation(PlayerState.DEATH, deathAnimationIndex);
        visuals._anim.ResetTrigger("4_Death");
        visuals._anim.Play("DEATH", 0, 0f);
        reactionEnds = 0f;
        state = PlayerState.DEATH;
        deathPlaying = true;
        return visuals.DEATH_List[deathAnimationIndex].length / Mathf.Max(0.01f, visuals._anim.speed);
    }

    public void HoldDeathPose()
    {
        if (!deathPlaying || !isActiveAndEnabled || visuals == null || visuals._anim == null) return;
        visuals._anim.Play("DEATH", 0, 0.999f);
        visuals._anim.Update(0f);
        visuals._anim.speed = 0f;
    }

    private void RestoreAnimationSpeed()
    {
        if (ready && visuals != null && visuals._anim != null)
            visuals._anim.speed = defaultAnimationSpeed;
    }
}
