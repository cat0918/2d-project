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

    [Header("Animation")]
    [Min(0)] public int prepareAnimationIndex = 0;
    [Min(0)] public int dashAnimationIndex = 0;
    [Min(0)] public int axeAnimationIndex = 0;
    [Range(0.1f, 2f)] public float dashAnimationSpeed = 1f;
    [Range(0.1f, 2f)] public float axeAnimationSpeed = 1f;

    [Header("Timing")]
    public float attackCooldown = 1.4f;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        rb.freezeRotation = true;
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
            StartCoroutine(ChooseAttack(distance));
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

    private IEnumerator ChooseAttack(float distance)
    {
        isAttacking = true;
        StopMoving();

        attackDirection = target.position.x >= transform.position.x ? 1f : -1f;
        if (animationDriver != null)
            animationDriver.PlayBossAction(PlayerState.OTHER, prepareAnimationIndex);

        if (distance <= stopDistance + 1f)
        {
            yield return DashAttack();
        }
        else
        {
            yield return AxeAttack();
        }

        StopMoving();
        if (animationDriver != null) animationDriver.PlayRecovery();
        yield return new WaitForSeconds(Mathf.Max(0f, attackCooldown));
        nextAttackTime = Time.time;
        isAttacking = false;
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

    private IEnumerator AxeAttack()
    {
        if (attackEffectsEnabled)
            preparationEffect = BossAttackEffects.Prepare(axeFirePoint != null ? axeFirePoint : transform,
                bodyCollider, axePrepareTime, false);
        yield return new WaitForSeconds(Mathf.Max(0f, axePrepareTime));
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
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
}
