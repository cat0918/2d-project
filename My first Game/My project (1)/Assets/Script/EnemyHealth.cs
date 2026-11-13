using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    [Min(0f)] public float deathDisappearDelay = 1f;

    private int currentHealth;
    public bool IsDead => currentHealth <= 0;
    public event System.Action OnDamaged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
        else OnDamaged?.Invoke();
    }

    private void Die()
    {
        currentHealth = 0;
        EnemyMeleeAI melee = GetComponent<EnemyMeleeAI>();
        if (melee != null) { melee.CancelAttack(); melee.enabled = false; }
        EnemyRangedAI ranged = GetComponent<EnemyRangedAI>();
        if (ranged != null) { ranged.CancelShot(); ranged.enabled = false; }
        BossController boss = GetComponent<BossController>();
        if (boss != null) { boss.CancelAttack(); boss.enabled = false; }
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();
        if (patrol != null) patrol.enabled = false;
        foreach (EnemyContactDamage contact in GetComponentsInChildren<EnemyContactDamage>())
            contact.enabled = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        EnemyAnimation animationDriver = GetComponent<EnemyAnimation>();
        float duration = animationDriver != null ? animationDriver.PlayDeath() : 0f;
        if (duration > 0f) yield return new WaitForSeconds(duration);
        if (animationDriver != null) animationDriver.HoldDeathPose();
        if (deathDisappearDelay > 0f) yield return new WaitForSeconds(deathDisappearDelay);
        Destroy(gameObject);
    }
}
