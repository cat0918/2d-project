using UnityEngine;
using System.Collections;

public class EnemyMeleeAI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Move")]
    public float chaseRange = 4f;
    public float attackRange = 1.2f;
    public float moveSpeed = 2.2f;
    public bool invertFacing = false;

    [Header("Attack")]
    public int attackDamage = 1;
    public float attackCooldown = 0.8f;
    [Range(0f, 1f)] public float hitTimeNormalized = 0.35f;
    [Min(0.05f)] public float fallbackAttackDuration = 0.5f;
    public bool IsAttacking { get; private set; }

    private Rigidbody2D rb;
    [Header("Patrol")]
    public bool patrolEnabled = true;
    [Min(0f)] public float patrolSpeed = 1f;
    [Min(0.1f)] public float patrolTurnInterval = 2f;
    public bool patrolStartMovingRight = false;
    private float patrolDirection;
    private float patrolTimeLeft;

    [Header("Checks")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.2f;
    public LayerMask groundLayer;
    private float nextAttackTime;
    private EnemyAnimation animationDriver;
    private EnemyHealth health;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animationDriver = GetComponent<EnemyAnimation>();
        health = GetComponent<EnemyHealth>();
        patrolDirection = patrolStartMovingRight ? 1f : -1f;
        patrolTimeLeft = Mathf.Max(0.1f, patrolTurnInterval);
    }

    private void FixedUpdate()
    {
        if ((health != null && health.IsDead) || IsAttacking ||
            (animationDriver != null && animationDriver.IsReactingToHit))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dist = target != null && target.gameObject.activeInHierarchy
            ? Vector2.Distance(transform.position, target.position)
            : float.PositiveInfinity;
        if (dist > chaseRange)
        {
            Patrol();
            return;
        }

        patrolTimeLeft = Mathf.Max(0.1f, patrolTurnInterval);
        FaceTarget();

        if (dist <= attackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryAttack();
            return;
        }

        float dir = target.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(CanMove(dir) ? dir * moveSpeed : 0f, rb.linearVelocity.y);
    }

    private void Patrol()
    {
        if (!patrolEnabled)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        patrolTimeLeft -= Time.fixedDeltaTime;
        if (patrolTimeLeft <= 0f)
        {
            patrolDirection *= -1f;
            patrolTimeLeft = Mathf.Max(0.1f, patrolTurnInterval);
        }
        if (!CanMove(patrolDirection))
        {
            if (!CanMove(-patrolDirection))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }
            patrolDirection *= -1f;
            patrolTimeLeft = Mathf.Max(0.1f, patrolTurnInterval);
        }
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (invertFacing ? -patrolDirection : patrolDirection);
        transform.localScale = scale;
        rb.linearVelocity = new Vector2(patrolDirection * Mathf.Max(0f, patrolSpeed), rb.linearVelocity.y);
    }

    private bool CanMove(float dir)
    {
        // Probe toward travel even when the sprite faces the opposite direction.
        if (groundCheck != null)
        {
            Vector2 origin = groundCheck.position;
            origin.x = transform.position.x + Mathf.Abs(origin.x - transform.position.x) * dir;
            if (!Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer))
                return false;
        }
        if (wallCheck != null)
        {
            Vector2 origin = wallCheck.position;
            origin.x = transform.position.x + Mathf.Abs(origin.x - transform.position.x) * dir;
            if (Physics2D.Raycast(origin, Vector2.right * dir, wallCheckDistance, groundLayer))
                return false;
        }
        return true;
    }

    private void FaceTarget()
    {
        float dir = target.position.x > transform.position.x ? 1f : -1f;
        if (invertFacing) dir *= -1f;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;
        Transform victim = target;
        float facing = victim.position.x >= transform.position.x ? 1f : -1f;
        float duration = animationDriver != null ? animationDriver.PlayAttack() : 0f;
        if (duration <= 0f) duration = Mathf.Max(0.05f, fallbackAttackDuration);
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, duration);
        float delay = duration * Mathf.Clamp01(hitTimeNormalized);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // Recheck at impact so the player can dodge the windup.
        if (victim != null && victim.gameObject.activeInHierarchy &&
            (health == null || !health.IsDead) &&
            Vector2.Distance(transform.position, victim.position) <= attackRange &&
            (victim.position.x - transform.position.x) * facing >= 0f)
        {
            PlayerHealth playerHealth = victim.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage, transform.position);
        }
        if (duration > delay) yield return new WaitForSeconds(duration - delay);
        IsAttacking = false;
    }

    public void CancelAttack()
    {
        StopAllCoroutines();
        IsAttacking = false;
    }

    private void OnDisable()
    {
        CancelAttack();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
