using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public Transform attackPoint;
    public float attackRange = 0.8f;
    public int attackDamage = 1;
    public float attackCooldown = 0.25f;
    public LayerMask enemyLayer;

    [Header("Hit Feedback")]
    public bool hitFeedbackEnabled = true;
    [Range(1, 16)] public int hitSparkCount = 7;
    [Range(0.02f, 0.2f)] public float enemyTintDuration = 0.08f;
    public Color hitSparkColor = new Color(1f, 0.82f, 0.42f, 1f);

    [Header("Animation Timing")]
    [Min(0)] public int attackAnimationIndex = 0;
    [Range(0f, 1f)] public float hitTimeNormalized = 0.35f;
    [Min(0.05f)] public float fallbackAttackDuration = 0.5f;

    public bool IsAttacking { get; private set; }
    private PlayerAnimation animationDriver;
    private PlayerHealth health;
    private readonly HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

    private float nextAttackTime;

    private void Awake()
    {
        animationDriver = GetComponent<PlayerAnimation>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || GameFlowScreen.BlocksInput) return;
        bool attackPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
        if (!attackPressed) return;
        if (IsAttacking || Time.time < nextAttackTime) return;
        if (health != null && health.IsDead) return;
        if (animationDriver != null && animationDriver.IsReactingToHit) return;
        if (attackPoint == null)
        {
            Debug.LogWarning("Assign Attack Point before attacking.", this);
            return;
        }

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;
        float duration = animationDriver != null ? animationDriver.PlayAttack(attackAnimationIndex) : 0f;
        if (duration <= 0f) duration = Mathf.Max(0.05f, fallbackAttackDuration);
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, duration);

        float hitDelay = duration * Mathf.Clamp01(hitTimeNormalized);
        if (hitDelay > 0f) yield return new WaitForSeconds(hitDelay);
        if (attackPoint != null && (health == null || !health.IsDead)) PerformAttack();
        float recovery = duration - hitDelay;
        if (recovery > 0f) yield return new WaitForSeconds(recovery);
        IsAttacking = false;
    }

    private void OnDisable()
    {
        CancelAttack();
    }

    public void CancelAttack()
    {
        StopAllCoroutines();
        IsAttacking = false;
    }

    private void PerformAttack()
    {
        GameSfx.Play(GameSound.Swing, transform.position);
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        damagedEnemies.Clear();
        bool landedHit = false;
        for (int i = 0; i < hitEnemies.Length; i++)
        {
            EnemyHealth enemyHealth = hitEnemies[i].GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && damagedEnemies.Add(enemyHealth))
            {
                Vector2 impact = hitEnemies[i].ClosestPoint(attackPoint.position);
                float direction = enemyHealth.transform.position.x >= transform.position.x ? 1f : -1f;
                if (enemyHealth.TryTakeDamage(attackDamage) && hitFeedbackEnabled)
                {
                    landedHit = true;
                    CombatHitBurst.Play(impact, direction, hitSparkCount, hitSparkColor);
                    EnemyHitTint.Pulse(enemyHealth.gameObject, enemyTintDuration);
                }
            }
        }
        if (landedHit && Camera.main != null)
        {
            CameraFollow2D follow = Camera.main.GetComponent<CameraFollow2D>();
            if (follow != null) follow.PlayHitShake();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
