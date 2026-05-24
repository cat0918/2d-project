using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public Transform attackPoint;
    public float attackRange = 0.8f;
    public int attackDamage = 1;
    public float attackCooldown = 0.25f;
    public LayerMask enemyLayer;

    private float nextAttackTime;

    private void Update()
    {
        bool attackPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
        if (!attackPressed) return;
        if (Time.time < nextAttackTime) return;

        PerformAttack();
        nextAttackTime = Time.time + attackCooldown;
    }

    private void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        for (int i = 0; i < hitEnemies.Length; i++)
        {
            EnemyHealth enemyHealth = hitEnemies[i].GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
