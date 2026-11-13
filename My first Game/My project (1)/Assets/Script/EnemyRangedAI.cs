using UnityEngine;
using System.Collections;

public class EnemyRangedAI : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Move")]
    public float detectRange = 6f;
    public float keepDistance = 3f;
    public float moveSpeed = 1.8f;
    public bool invertFacing = false;

    [Header("Checks")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.2f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("Shoot")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public int projectileDamage = 1;
    public float projectileSpeed = 6f;
    public float shootCooldown = 1.2f;
    [Range(0f, 1f)] public float fireTimeNormalized = 0.45f;
    [Min(0.05f)] public float fallbackShootDuration = 0.6f;
    public bool IsShooting { get; private set; }

    private Rigidbody2D rb;
    private float nextShootTime;
    private EnemyAnimation animationDriver;
    private EnemyHealth health;

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

        if (target == null || !target.gameObject.activeInHierarchy ||
            (health != null && health.IsDead) || IsShooting ||
            (animationDriver != null && animationDriver.IsReactingToHit))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > detectRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        FaceTarget();

        float horizontalDistance = Mathf.Abs(target.position.x - transform.position.x);
        if (horizontalDistance < keepDistance * 0.8f)
        {
            MoveAwayFromTarget();
        }
        else if (horizontalDistance > keepDistance * 1.2f)
        {
            MoveTowardTarget();
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        TryShoot();
    }

    private void MoveTowardTarget()
    {
        float dir = target.position.x > transform.position.x ? 1f : -1f;
        if (!CanMove(dir))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    private void MoveAwayFromTarget()
    {
        float dir = target.position.x > transform.position.x ? -1f : 1f;
        if (!CanMove(dir))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    private bool CanMove(float dir)
    {
        if (groundCheck == null || wallCheck == null) return true;

        Vector2 groundOrigin = groundCheck.position;
        groundOrigin.x = transform.position.x + Mathf.Abs(groundCheck.localPosition.x) * Mathf.Sign(dir);

        bool hasGroundAhead = Physics2D.Raycast(groundOrigin, Vector2.down, groundCheckDistance, groundLayer);
        bool hitsWall = Physics2D.Raycast(wallCheck.position, Vector2.right * Mathf.Sign(dir), wallCheckDistance, groundLayer);

        return hasGroundAhead && !hitsWall;
    }

    private void FaceTarget()
    {
        float dir = target.position.x > transform.position.x ? 1f : -1f;
        if (invertFacing) dir *= -1f;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }

    private void TryShoot()
    {
        if (Time.time < nextShootTime) return;
        if (firePoint == null || projectilePrefab == null) return;
        if (projectilePrefab.GetComponent<EnemyProjectile>() == null)
        {
            Debug.LogWarning("Projectile Prefab needs EnemyProjectile.", this);
            nextShootTime = Time.time + Mathf.Max(0.1f, shootCooldown);
            return;
        }
        StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        IsShooting = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        Transform victim = target;
        Vector2 direction = victim.position.x >= transform.position.x ? Vector2.right : Vector2.left;
        float duration = animationDriver != null ? animationDriver.PlayAttack() : 0f;
        if (duration <= 0f) duration = Mathf.Max(0.05f, fallbackShootDuration);
        nextShootTime = Time.time + Mathf.Max(duration, shootCooldown);
        float delay = duration * Mathf.Clamp01(fireTimeNormalized);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (victim != null && victim.gameObject.activeInHierarchy &&
            (health == null || !health.IsDead) && firePoint != null && projectilePrefab != null)
        {
            GameObject shot = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            EnemyProjectile projectile = shot.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                // A moving weapon may place the muzzle inside the shooter's collider.
                foreach (Collider2D bullet in shot.GetComponentsInChildren<Collider2D>())
                    foreach (Collider2D owner in GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(bullet, owner);
                projectile.Launch(direction, projectileDamage, projectileSpeed);
            }
            else Destroy(shot);
        }
        if (duration > delay) yield return new WaitForSeconds(duration - delay);
        IsShooting = false;
    }

    public void CancelShot()
    {
        StopAllCoroutines();
        IsShooting = false;
    }

    private void OnDisable()
    {
        CancelShot();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, keepDistance);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
        }
    }
}
