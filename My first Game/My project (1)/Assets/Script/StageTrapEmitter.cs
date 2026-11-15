using System.Collections;
using UnityEngine;

public class StageTrapEmitter : MonoBehaviour
{
    public Sprite projectileSprite;
    public Vector2 projectileSize = new Vector2(0.7f, 0.18f);
    public Vector2 direction = Vector2.left;
    public float speed = 8f;
    [Min(0.1f)] public float interval = 3f;
    [Min(0f)] public float warningTime = 0.7f;
    [Min(0f)] public float initialDelay;
    public float projectileLifetime = 2f;
    public LayerMask terrainMask;
    public SpriteRenderer warningMarker;
    private Color idleColor;

    private void OnEnable()
    {
        if (warningMarker != null) idleColor = warningMarker.color;
        StartCoroutine(Cycle());
    }

    private IEnumerator Cycle()
    {
        yield return new WaitForSeconds(initialDelay);
        while (true)
        {
            if (warningMarker != null) warningMarker.color = new Color(1f, 0.25f, 0.08f);
            yield return new WaitForSeconds(Mathf.Max(0.05f, warningTime));
            Fire();
            if (warningMarker != null) warningMarker.color = idleColor;
            yield return new WaitForSeconds(Mathf.Max(0.1f, interval - warningTime));
        }
    }

    private void Fire()
    {
        if (projectileSprite == null) return;
        GameObject shot = new GameObject("TrapProjectile");
        shot.transform.position = transform.position;
        SpriteRenderer visual = shot.AddComponent<SpriteRenderer>();
        visual.sprite = projectileSprite;
        visual.color = new Color(1f, 0.65f, 0.25f);
        visual.sortingOrder = 8;
        Vector2 spriteSize = projectileSprite.bounds.size;
        shot.transform.localScale = new Vector3(projectileSize.x / spriteSize.x, projectileSize.y / spriteSize.y, 1f);
        BoxCollider2D hit = shot.AddComponent<BoxCollider2D>();
        hit.isTrigger = true;
        hit.size = spriteSize;
        Rigidbody2D body = shot.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.linearVelocity = direction.normalized * speed;
        StageTrapProjectile projectile = shot.AddComponent<StageTrapProjectile>();
        projectile.terrainMask = terrainMask;
        projectile.lifetime = projectileLifetime;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (warningMarker != null) warningMarker.color = idleColor;
    }
}
