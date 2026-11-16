using UnityEngine;
using System;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    public float invincibleDuration = 1.2f;

    [Header("Knockback")]
    public float knockbackForceX = 8f;
    [Min(0f)] public float knockbackDuration = 0.18f;

    private int currentHealth;
    private float invincibleTimer;
    private Rigidbody2D rb;
    private PlayerMove movement;

    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvincible => invincibleTimer > 0f;
    public event Action OnDamaged;
    public bool DeathSequenceFinished { get; private set; }
    [Min(0f)] public float deathScreenDelay = 0.5f;
    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMove>();
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

    public void RestAtCheckpoint()
    {
        if (IsDead) return;
        currentHealth = maxHealth;
        invincibleTimer = Mathf.Max(invincibleTimer, invincibleDuration);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage, Vector2 hitSourcePosition)
    {
        if (damage <= 0) return;
        if (IsDead) return;
        if (invincibleTimer > 0f) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        invincibleTimer = invincibleDuration;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        ApplyKnockback(hitSourcePosition);
        if (!IsDead) OnDamaged?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void ApplyKnockback(Vector2 hitSourcePosition)
    {
        float dirX = transform.position.x >= hitSourcePosition.x ? 1f : -1f;
        if (movement != null) movement.BeginKnockback(knockbackDuration);
        Vector2 knockback = new Vector2(dirX * knockbackForceX, 0f);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    private void Die()
    {
        if (movement != null) movement.enabled = false;
        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;
        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;
        PlayerJumpPose pose = GetComponent<PlayerJumpPose>();
        if (pose != null) pose.enabled = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        if (GetComponent<PlayerDeathScreen>() == null)
            gameObject.AddComponent<PlayerDeathScreen>();
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        PlayerAnimation animationDriver = GetComponent<PlayerAnimation>();
        float duration = animationDriver != null ? animationDriver.PlayDeath() : 0f;
        if (duration > 0f) yield return new WaitForSeconds(duration);
        if (animationDriver != null) animationDriver.HoldDeathPose();
        if (deathScreenDelay > 0f) yield return new WaitForSeconds(deathScreenDelay);
        DeathSequenceFinished = true;
    }
}
