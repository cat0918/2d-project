using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class StageTrapProjectile : MonoBehaviour
{
    public LayerMask terrainMask;
    public float lifetime = 3f;
    private bool spent;

    private void Start() { Destroy(gameObject, lifetime); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (spent) return;
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            if (player.IsDead) return;
            player.TakeDamage(1, transform.position);
        }
        else if (other.isTrigger || (terrainMask.value & (1 << other.gameObject.layer)) == 0)
            return;
        spent = true;
        Destroy(gameObject);
    }
}
