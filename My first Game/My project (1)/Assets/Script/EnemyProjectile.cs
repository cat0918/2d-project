using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 1;
    public float speed = 6f;
    public float lifeTime = 3f;

    private Vector2 direction = Vector2.right;
    private bool impacted;
    public bool ResetOnCheckpointRest { get; private set; }

    public void Launch(Vector2 newDirection, int newDamage, float newSpeed, bool resetOnCheckpointRest = false)
    {
        direction = newDirection.normalized;
        damage = newDamage;
        speed = newSpeed;
        ResetOnCheckpointRest = resetOnCheckpointRest;
        GameSfx.Play(GameSound.Cast, transform.position);
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
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
        GameSfx.Play(GameSound.Impact, transform.position);
        FireballVisual fireball = GetComponent<FireballVisual>();
        if (fireball != null) fireball.PlayImpact(other.ClosestPoint(transform.position));
        foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) hitbox.enabled = false;
        Destroy(gameObject);
    }
}
