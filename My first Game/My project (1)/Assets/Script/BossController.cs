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
        yield return new WaitForSeconds(dashPrepareTime);

        if (!HasTarget()) yield break;
        float clipDuration = animationDriver != null ?
            animationDriver.PlayBossAction(PlayerState.ATTACK, dashAnimationIndex, dashAnimationSpeed) : 0f;
        float direction = attackDirection;
        float timer = dashDuration;

        while (timer > 0f)
        {
            rb.linearVelocity = new Vector2(direction * dashSpeed, rb.linearVelocity.y);
            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        StopMoving();
        if (clipDuration > dashDuration)
            yield return new WaitForSeconds(clipDuration - dashDuration);
    }

    private IEnumerator AxeAttack()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, axePrepareTime));
        if (!HasTarget()) yield break;
        float duration = animationDriver != null ?
            animationDriver.PlayBossAction(PlayerState.ATTACK, axeAnimationIndex, axeAnimationSpeed) : 0f;
        if (duration <= 0f) duration = 0.6f;
        float delay = duration * Mathf.Clamp01(axeReleaseTimeNormalized);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (HasTarget() && axeFirePoint != null && axeProjectilePrefab != null)
        {
            Vector2 direction = Vector2.right * attackDirection;
            GameObject axeObject = Instantiate(axeProjectilePrefab, axeFirePoint.position, Quaternion.identity);

            BossAxeProjectile axe = axeObject.GetComponent<BossAxeProjectile>();
            if (axe != null)
            {
                foreach (Collider2D projectile in axeObject.GetComponentsInChildren<Collider2D>())
                    foreach (Collider2D owner in GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(projectile, owner);
                axe.Launch(direction, axeDamage, axeSpeed);
            }
            else Destroy(axeObject);
        }
        if (duration > delay) yield return new WaitForSeconds(duration - delay);
    }

    private bool HasTarget()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        PlayerHealth player = target.GetComponent<PlayerHealth>();
        return player == null || !player.IsDead;
    }

    public void CancelAttack()
    {
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
