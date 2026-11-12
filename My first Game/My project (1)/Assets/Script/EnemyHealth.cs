using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;

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
        Destroy(gameObject);
    }
}
