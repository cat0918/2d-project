using UnityEngine;

public class BossAxeProjectile : MonoBehaviour
{
    public int damage = 1;
    public float speed = 7f;
    public float rotateSpeed = 720f;
    public float lifeTime = 4f;
    public bool impactEffectsEnabled;
    private bool impacted;

    private Vector2 direction = Vector2.right;

    public void Launch(Vector2 newDirection, int newDamage, float newSpeed)
    {
        direction = newDirection.normalized;
        damage = newDamage;
        speed = newSpeed;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            Impact(other);
            playerHealth.TakeDamage(damage, transform.position);
            return;
        }

        if (!other.isTrigger)
        {
            Impact(other);
        }
    }

    private void Impact(Collider2D other)
    {
        impacted = true;
        if (impactEffectsEnabled) BossAttackEffects.Burst(other.ClosestPoint(transform.position));
        foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) hitbox.enabled = false;
        Destroy(gameObject);
    }
}
