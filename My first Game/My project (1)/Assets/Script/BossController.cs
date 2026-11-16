using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Move")]
    public float detectRange = 8f;
    public float moveSpeed = 1.6f;
    public float stopDistance = 3f;
    public bool invertFacing = false;

    [Header("Basic Melee Attack")]
    public bool basicMeleeEnabled = true;
    [Min(0.1f)] public float meleeReach = 2f;
    [Min(0.1f)] public float meleeHitHeight = 1.6f;
    [Min(1)] public int meleeDamage = 1;
    [Min(0f)] public float meleePrepareTime = 0.25f;
    [Min(0)] public int meleeAnimationIndex = 0;
    [Range(0.1f, 2f)] public float meleeAnimationSpeed = 1f;
    [Range(0f, 1f)] public float meleeHitTimeNormalized = 0.4f;

    [Header("Dash Attack")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.25f;
    public float dashPrepareTime = 0.45f;

    [Header("Axe Attack")]
    public Transform axeFirePoint;
    public GameObject axeProjectilePrefab;
    public int axeDamage = 1;
    public float axeSpeed = 7f;
    [Min(0f)] public float axePrepareTime = 0.45f;
    [Range(0f, 1f)] public float axeReleaseTimeNormalized = 0.45f;

    [Header("Axe Volley")]
    public bool axeVolleyEnabled = true;
    [Range(2, 5)] public int axeVolleyCount = 3;
    [Min(0f)] public float volleyFirstPrepareTime = 0.75f;
    [Min(0f)] public float volleyFollowupPrepareTime = 0.2f;
    [Min(0f)] public float volleyShotPause = 0.12f;
    [Min(0f)] public float volleyExtraRecovery = 0.6f;

    [Header("Jump Slam")]
    public bool jumpSlamEnabled = true;
    [Range(1f, 2.5f)] public float slamTempo = 1.5f;
    [Min(0f)] public float slamPrepareTime = 0.65f;
    [Min(0.5f)] public float slamJumpHeight = 3f;
    [Min(0f)] public float slamHoverTime = 0.25f;
    [Min(1f)] public float slamFallSpeed = 16f;
    [Min(0.2f)] public float slamRadius = 2f;
    [Min(0.1f)] public float slamHitHeight = 0.8f;
    [Min(1)] public int slamDamage = 1;
    [Min(0f)] public float slamExtraRecovery = 0.8f;
    public LayerMask slamGroundLayers;
    [Min(0)] public int slamAnimationIndex = 0;

    [Header("Animation")]
    [Min(0)] public int prepareAnimationIndex = 0;
    [Min(0)] public int dashAnimationIndex = 0;
    [Min(0)] public int axeAnimationIndex = 0;
    [Range(0.1f, 2f)] public float dashAnimationSpeed = 1f;
    [Range(0.1f, 2f)] public float axeAnimationSpeed = 1f;

    [Header("Timing")]
    public float attackCooldown = 1.4f;

    [Header("Unified Attack Weights")]
    [Min(0f)] public float meleeWeight = 35f;
    [Min(0f)] public float dashWeight = 35f;
    [Min(0f)] public float slamWeight = 15f;
    [Min(0f)] public float axeWeight = 10f;
    [Min(0f)] public float axeVolleyWeight = 5f;

    [Header("Hit Reaction")]
    public bool ignoreHitStagger = true;

    [Header("Attack Effects")]
    public bool attackEffectsEnabled = true;
    private BossAttackEffects preparationEffect;
    private Collider2D bodyCollider;

    private Rigidbody2D rb;
    private float nextAttackTime;
    private bool isAttacking;
    public bool IsAttacking => isAttacking;
    private EnemyAnimation animationDriver;
    private EnemyHealth health;
    private float attackDirection;
    private enum AttackPattern { None, Melee, Dash, Slam, Axe, Volley }
    private bool slamPhysicsActive;
    private float preSlamGravity;
    private readonly RaycastHit2D[] slamHits = new RaycastHit2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        SetHorizontalMotionAllowed(false);
        animationDriver = GetComponent<EnemyAnimation>();
        health = GetComponent<EnemyHealth>();
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.identity;

        if (!HasTarget() || (health != null && health.IsDead))
        {
            CancelAttack();
            return;
        }

        if (animationDriver != null && animationDriver.IsReactingToHit)
        {
            StopMoving();
            return;
        }

        if (isAttacking)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > detectRange)
        {
            StopMoving();
            return;
        }

        FaceTarget();

        if (Time.time >= nextAttackTime)
        {
            StartCoroutine(ChooseAttack());
            return;
        }

        if (distance > stopDistance)
        {
            MoveTowardTarget();
        }
        else
        {
            StopMoving();
        }
    }

    private AttackPattern RollAttack()
    {
        // Only physical/setup requirements filter the pool, never distance to the player.
        bool canThrow = axeFirePoint != null && axeProjectilePrefab != null &&
            axeProjectilePrefab.GetComponent<BossAxeProjectile>() != null;
        float melee = basicMeleeEnabled ? Mathf.Max(0f, meleeWeight) : 0f;
        float dash = Mathf.Max(0f, dashWeight);
        float slam = jumpSlamEnabled && slamWeight > 0f && TrySlamGround(0.18f, out _)
            ? Mathf.Max(0f, slamWeight) : 0f;
        float axe = canThrow ? Mathf.Max(0f, axeWeight) : 0f;
        float volley = canThrow && axeVolleyEnabled ? Mathf.Max(0f, axeVolleyWeight) : 0f;
        float total = melee + dash + slam + axe + volley;
        if (total <= 0f) return AttackPattern.None;
        float roll = Random.value * total;
        if (melee > 0f && roll < melee) return AttackPattern.Melee;
        roll -= melee;
        if (dash > 0f && roll < dash) return AttackPattern.Dash;
        roll -= dash;
        if (slam > 0f && roll < slam) return AttackPattern.Slam;
        roll -= slam;
        if (axe > 0f && roll < axe) return AttackPattern.Axe;
        if (volley > 0f) return AttackPattern.Volley;
        // Random.value can equal one: choose the last eligible entry at that endpoint.
        if (axe > 0f) return AttackPattern.Axe;
        if (slam > 0f) return AttackPattern.Slam;
        return dash > 0f ? AttackPattern.Dash : AttackPattern.Melee;
    }

    private IEnumerator ChooseAttack()
    {
        AttackPattern pattern = RollAttack();
        if (pattern == AttackPattern.None)
        {
            nextAttackTime = Time.time + Mathf.Max(0.1f, attackCooldown);
            yield break;
        }
        isAttacking = true;
        StopMoving();
        bool usedVolley = pattern == AttackPattern.Volley;
        bool usedSlam = pattern == AttackPattern.Slam;

        attackDirection = target.position.x >= transform.position.x ? 1f : -1f;
        if (animationDriver != null)
            animationDriver.PlayBossAction(PlayerState.OTHER, prepareAnimationIndex);

        switch (pattern)
        {
            case AttackPattern.Melee: yield return BasicMeleeAttack(); break;
            case AttackPattern.Dash: yield return DashAttack(); break;
            case AttackPattern.Slam: yield return JumpSlam(); break;
            case AttackPattern.Axe: yield return AxeAttack(); break;
            case AttackPattern.Volley: yield return AxeVolley(); break;
        }

        StopMoving();
        if (animationDriver != null) animationDriver.PlayRecovery();
        yield return new WaitForSeconds(Mathf.Max(0f, attackCooldown) +
            (usedVolley ? Mathf.Max(0f, volleyExtraRecovery) : 0f) +
            (usedSlam ? Mathf.Max(0f, slamExtraRecovery) / Mathf.Clamp(slamTempo, 1f, 2.5f) : 0f));
        nextAttackTime = Time.time;
        isAttacking = false;
    }

    private Bounds MeleeBounds()
    {
        Vector3 center = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
        float bodyHalfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 0.4f;
        float reach = Mathf.Max(0.1f, meleeReach);
        // Slightly overlap the body's front half so point-blank targets are not missed.
        center.x += attackDirection * (bodyHalfWidth * 0.5f + reach * 0.5f);
        return new Bounds(center, new Vector3(reach, Mathf.Max(0.1f, meleeHitHeight), 4f));
    }

    private IEnumerator BasicMeleeAttack()
    {
        // Facing was locked by ChooseAttack; do not turn toward a dodging player mid-swing.
        yield return new WaitForSeconds(Mathf.Max(0f, meleePrepareTime));
        if (!HasTarget() || (health != null && health.IsDead)) yield break;
        float duration = animationDriver != null
            ? animationDriver.PlayBossAction(PlayerState.ATTACK, meleeAnimationIndex, meleeAnimationSpeed) : 0f;
        if (duration <= 0f) duration = 0.6f;
        float delay = duration * Mathf.Clamp01(meleeHitTimeNormalized);
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (!HasTarget() || (health != null && health.IsDead)) yield break;

        Bounds area = MeleeBounds();
        GameSfx.Play(GameSound.Swing, transform.position);
        var damaged = new System.Collections.Generic.HashSet<PlayerHealth>();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(area.center, area.size, 0f))
        {
            PlayerHealth victim = hit.GetComponentInParent<PlayerHealth>();
            if (victim == null || victim.IsDead || !damaged.Add(victim)) continue;
            if ((hit.bounds.center.x - transform.position.x) * attackDirection < 0f) continue;
            bool accepted = !victim.IsInvincible;
            victim.TakeDamage(meleeDamage, transform.position);
            if (accepted && attackEffectsEnabled)
                CombatHitBurst.Play(hit.ClosestPoint(area.center), attackDirection, 8, BossAttackEffects.Frost);
        }
        if (duration > delay) yield return new WaitForSeconds(duration - delay);
    }

    private int SlamGroundMask()
    {
        if (slamGroundLayers.value != 0) return slamGroundLayers.value;
        PlayerMove movement = target != null ? target.GetComponent<PlayerMove>() : null;
        return movement != null ? movement.groundLayer.value : LayerMask.GetMask("Ground");
    }

    private bool TrySlamGround(float distance, out RaycastHit2D ground)
    {
        ground = default;
        if (bodyCollider == null || !bodyCollider.enabled) return false;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(SlamGroundMask());
        filter.useTriggers = false;
        int count = bodyCollider.Cast(Vector2.down, filter, slamHits, distance);
        float closest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = slamHits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.5f || hit.distance >= closest) continue;
            ground = hit;
            closest = hit.distance;
        }
        return ground.collider != null;
    }

    private IEnumerator JumpSlam()
    {
        if (!TrySlamGround(0.18f, out RaycastHit2D initialGround)) yield break;
        Vector2 markerPosition = new Vector2(bodyCollider.bounds.center.x, initialGround.point.y + 0.025f);
        float tempo = Mathf.Clamp(slamTempo, 1f, 2.5f);
        yield return new WaitForSeconds(Mathf.Max(0f, slamPrepareTime) / tempo);
        if (!HasTarget()) { RestoreSlamPhysics(); yield break; }

        // Snapshot at takeoff, never chase the player after the leap has begun.
        float landingX = target.position.x;
        Collider2D targetBody = target.GetComponent<Collider2D>();
        if (targetBody != null) landingX = targetBody.bounds.center.x;
        preSlamGravity = rb.gravityScale;
        slamPhysicsActive = true;
        rb.gravityScale = preSlamGravity * tempo * tempo;
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        if (gravity < 0.01f) { RestoreSlamPhysics(); yield break; }
        float jumpVelocity = Mathf.Sqrt(2f * gravity * Mathf.Max(0.5f, slamJumpHeight));
        float ascentDuration = jumpVelocity / gravity;
        float travelSpeed = Mathf.Abs(landingX - bodyCollider.bounds.center.x) / Mathf.Max(0.05f, ascentDuration);
        SetHorizontalMotionAllowed(true);
        rb.linearVelocity = new Vector2(SlamTravelVelocity(landingX, travelSpeed), jumpVelocity);
        if (attackEffectsEnabled)
            MovementDustBurst.Play(BossAttackEffects.Feet(transform, bodyCollider), false, false, 1.5f, BossAttackEffects.Frost);
        // A ceiling collision ends ascent naturally; the timeout prevents a stalled pattern.
        float elapsed = 0f;
        do
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(SlamTravelVelocity(landingX, travelSpeed), rb.linearVelocity.y);
        } while (rb.linearVelocity.y > 0.05f && elapsed < 3f);

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        // Finish any small horizontal remainder during the hover, still respecting walls.
        float hoverRemaining = Mathf.Max(0f, slamHoverTime) / tempo;
        while (hoverRemaining > 0f)
        {
            rb.linearVelocity = new Vector2(SlamTravelVelocity(landingX, travelSpeed), 0f);
            yield return new WaitForFixedUpdate();
            hoverRemaining -= Time.fixedDeltaTime;
        }
        rb.linearVelocity = Vector2.zero;
        if (!HasTarget()) { RestoreSlamPhysics(); yield break; }
        SetHorizontalMotionAllowed(false);
        if (animationDriver != null)
            animationDriver.PlayBossAction(PlayerState.ATTACK, slamAnimationIndex);
        float fallSpeed = Mathf.Max(1f, slamFallSpeed) * tempo;
        elapsed = 0f;
        bool landed = false;
        Vector2 impact = markerPosition;
        while (elapsed < 3f)
        {
            // Sweep the next fall step so fast descent cannot tunnel through a thin floor.
            if (TrySlamGround(fallSpeed * Time.fixedDeltaTime + 0.03f, out RaycastHit2D ground))
            {
                rb.position += Vector2.down * Mathf.Max(0f, ground.distance - 0.02f);
                rb.linearVelocity = Vector2.zero;
                impact = new Vector2(bodyCollider.bounds.center.x, ground.point.y);
                landed = true;
                break;
            }
            rb.linearVelocity = Vector2.down * fallSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        RestoreSlamPhysics();
        if (!landed) yield break;
        GameSfx.Play(GameSound.Slam, impact);
        BossSlamImpact.Play(impact, Mathf.Max(0.2f, slamRadius), Mathf.Max(0.1f, slamHitHeight));
        if (attackEffectsEnabled)
        {
            BossAttackEffects.Burst(impact);
            MovementDustBurst.Play(impact, true, false, 2f, BossAttackEffects.Frost);
            CameraFollow2D camera = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
            if (camera != null) camera.PlayHitShake();
        }
        var damaged = new System.Collections.Generic.HashSet<PlayerHealth>();
        Vector2 size = new Vector2(Mathf.Max(0.2f, slamRadius) * 2f, Mathf.Max(0.1f, slamHitHeight));
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(impact + Vector2.up * size.y * 0.5f, size, 0f))
        {
            PlayerHealth victim = hit.GetComponentInParent<PlayerHealth>();
            if (victim != null && damaged.Add(victim)) victim.TakeDamage(slamDamage, impact);
        }
    }

    private float SlamTravelVelocity(float landingX, float speed)
    {
        float difference = landingX - bodyCollider.bounds.center.x;
        float step = Mathf.Min(Mathf.Abs(difference), speed * Time.fixedDeltaTime);
        if (step <= 0.001f) return 0f;
        Vector2 direction = Vector2.right * Mathf.Sign(difference);
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(SlamGroundMask());
        filter.useTriggers = false;
        int count = bodyCollider.Cast(direction, filter, slamHits, step + 0.02f);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = slamHits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
            if (Vector2.Dot(hit.normal, direction) < -0.5f)
                step = Mathf.Min(step, Mathf.Max(0f, hit.distance - 0.02f));
        }
        return Mathf.Sign(difference) * step / Time.fixedDeltaTime;
    }

    private void RestoreSlamPhysics()
    {
        if (!slamPhysicsActive) return;
        rb.gravityScale = preSlamGravity;
        rb.linearVelocity = Vector2.zero;
        SetHorizontalMotionAllowed(false);
        slamPhysicsActive = false;
    }

    private IEnumerator DashAttack()
    {
        if (attackEffectsEnabled)
            preparationEffect = BossAttackEffects.Prepare(transform, bodyCollider, dashPrepareTime, true);
        yield return new WaitForSeconds(dashPrepareTime);
        ClearPreparationEffect();

        if (!HasTarget()) yield break;
        float clipDuration = animationDriver != null ?
            animationDriver.PlayBossAction(PlayerState.ATTACK, dashAnimationIndex, dashAnimationSpeed) : 0f;
        float direction = attackDirection;
        float timer = dashDuration;
        GameSfx.Play(GameSound.Dash, transform.position);
        SetHorizontalMotionAllowed(true);
        float dustTimer = 0f;
        if (attackEffectsEnabled) BossAttackEffects.Burst(BossAttackEffects.Feet(transform, bodyCollider));

        while (timer > 0f)
        {
            rb.linearVelocity = new Vector2(direction * dashSpeed, rb.linearVelocity.y);
            if (attackEffectsEnabled && dustTimer <= 0f)
            {
                MovementDustBurst.Play(BossAttackEffects.Feet(transform, bodyCollider), false, false, 1.2f,
                    new Color(0.55f, 0.7f, 0.8f, 0.65f));
                dustTimer = 0.07f;
            }
            dustTimer -= Time.fixedDeltaTime;
            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        StopMoving();
        if (clipDuration > dashDuration)
            yield return new WaitForSeconds(clipDuration - dashDuration);
    }

    private IEnumerator AxeVolley()
    {
        int count = Mathf.Clamp(axeVolleyCount, 2, 5);
        for (int shot = 0; shot < count; shot++)
        {
            if (!HasTarget() || (health != null && health.IsDead)) yield break;
            // Lock each throw before its tell; never turn at the release frame.
            attackDirection = target.position.x >= transform.position.x ? 1f : -1f;
            FaceTarget();
            if (animationDriver != null)
                animationDriver.PlayBossAction(PlayerState.OTHER, prepareAnimationIndex);
            yield return AxeAttack(shot == 0 ? volleyFirstPrepareTime : volleyFollowupPrepareTime);
            if (shot + 1 < count)
            {
                if (animationDriver != null) animationDriver.PlayRecovery();
                yield return new WaitForSeconds(Mathf.Max(0f, volleyShotPause));
            }
        }
    }

    private IEnumerator AxeAttack(float prepareOverride = -1f)
    {
        float prepareTime = Mathf.Max(0f, prepareOverride < 0f ? axePrepareTime : prepareOverride);
        if (attackEffectsEnabled)
            preparationEffect = BossAttackEffects.Prepare(axeFirePoint != null ? axeFirePoint : transform,
                bodyCollider, prepareTime, false);
        yield return new WaitForSeconds(prepareTime);
        ClearPreparationEffect();
        if (!HasTarget()) yield break;
        float duration = animationDriver != null ?
            animationDriver.PlayBossAction(PlayerState.ATTACK, axeAnimationIndex, axeAnimationSpeed) : 0f;
        if (duration <= 0f) duration = 0.6f;
        float delay = duration * Mathf.Clamp01(axeReleaseTimeNormalized);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (HasTarget() && axeFirePoint != null && axeProjectilePrefab != null)
        {
            Vector2 direction = Vector2.right * attackDirection;
            Vector3 spawnPosition = GetAxeSpawnPosition();
            GameObject axeObject = Instantiate(axeProjectilePrefab, spawnPosition, Quaternion.identity);

            BossAxeProjectile axe = axeObject.GetComponent<BossAxeProjectile>();
            if (axe != null)
            {
                foreach (Collider2D projectile in axeObject.GetComponentsInChildren<Collider2D>())
                    foreach (Collider2D owner in GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(projectile, owner);
                axe.Launch(direction, axeDamage, axeSpeed);
                axe.impactEffectsEnabled = attackEffectsEnabled;
                if (attackEffectsEnabled)
                {
                    BossAttackEffects.AttachAxeTrail(axeObject);
                    CombatHitBurst.Play(spawnPosition, attackDirection, 7, BossAttackEffects.Frost);
                }
            }
            else
            {
                Debug.LogWarning("Boss axe prefab is missing BossAxeProjectile on its root.", this);
                Destroy(axeObject);
            }
        }
        else if (HasTarget()) Debug.LogWarning("Boss needs Axe Fire Point and Axe Projectile Prefab assigned.", this);
        if (duration > delay) yield return new WaitForSeconds(duration - delay);
    }

    private Vector3 GetAxeSpawnPosition()
    {
        Vector3 position = axeFirePoint.position;
        if (bodyCollider == null) return position;
        Bounds body = bodyCollider.bounds;
        float radius = 0.2f;
        BoxCollider2D axeBox = axeProjectilePrefab.GetComponent<BoxCollider2D>();
        if (axeBox != null)
        {
            Vector2 scale = axeBox.transform.lossyScale;
            Vector2 corner = new Vector2(Mathf.Abs(axeBox.offset.x) + axeBox.size.x * 0.5f,
                Mathf.Abs(axeBox.offset.y) + axeBox.size.y * 0.5f);
            radius = Vector2.Scale(corner, scale).magnitude;
        }
        // Keep the entire rotating collider clear of the floor, not just its initial pose.
        if (Vector2.Distance(position, body.center) > body.size.magnitude + radius)
            position = body.center;
        position.x = body.center.x + attackDirection * (body.extents.x + radius + 0.05f);
        position.y = Mathf.Max(position.y, body.min.y + radius + 0.05f);
        position.z = transform.position.z;
        return position;
    }

    private bool HasTarget()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        PlayerHealth player = target.GetComponent<PlayerHealth>();
        return player == null || !player.IsDead;
    }

    public void CancelAttack()
    {
        ClearPreparationEffect();
        RestoreSlamPhysics();
        bool wasAttacking = isAttacking;
        StopAllCoroutines();
        isAttacking = false;
        if (rb != null) StopMoving();
        if (wasAttacking)
        {
            nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
            if (animationDriver != null) animationDriver.PlayRecovery();
        }
    }

    private void ClearPreparationEffect()
    {
        if (preparationEffect != null) preparationEffect.StopEffect();
        preparationEffect = null;
    }

    private void OnDisable()
    {
        CancelAttack();
    }

    private void MoveTowardTarget()
    {
        SetHorizontalMotionAllowed(true);
        float direction = target.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    private void FaceTarget()
    {
        float direction = target.position.x > transform.position.x ? 1f : -1f;
        if (invertFacing) direction *= -1f;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    private void StopMoving()
    {
        SetHorizontalMotionAllowed(false);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void SetHorizontalMotionAllowed(bool allowed)
    {
        if (rb == null) return;
        // Preserve vertical physics and rotation constraints while bracing idle/attack poses.
        if (allowed) rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        else rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
    }
}
