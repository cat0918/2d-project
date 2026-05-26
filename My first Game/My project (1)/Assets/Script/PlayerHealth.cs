using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    public float invincibleDuration = 0.7f;

    [Header("Knockback")]
    public float knockbackForceX = 8f;
    public float knockbackForceY = 4f;

    private int currentHealth;
    private float invincibleTimer;
    private Rigidbody2D rb;

    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int damage, Vector2 hitSourcePosition)
    {
        if (IsDead) return;
        if (invincibleTimer > 0f) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        invincibleTimer = invincibleDuration;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        ApplyKnockback(hitSourcePosition);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void ApplyKnockback(Vector2 hitSourcePosition)
    {
        float dirX = transform.position.x >= hitSourcePosition.x ? 1f : -1f;
        Vector2 knockback = new Vector2(dirX * knockbackForceX, knockbackForceY);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    private void Die()
    {
        Debug.Log("Player Died");
        gameObject.SetActive(false);
    }
}
