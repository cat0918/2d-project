using UnityEngine;

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

    private Rigidbody2D rb;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist > chaseRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        FaceTarget();

        if (dist <= attackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryAttack();
            return;
        }

        float dir = target.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
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

        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage, transform.position);
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
