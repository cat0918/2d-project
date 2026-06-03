using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 1;
    public float speed = 6f;
    public float lifeTime = 3f;

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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
