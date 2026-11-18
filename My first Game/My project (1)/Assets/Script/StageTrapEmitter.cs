using UnityEngine;

public class StageTrapEmitter : MonoBehaviour
{
    public Sprite projectileSprite;
    public Color projectileColor = new Color(1f, 0.65f, 0.25f);
    public Vector2 projectileSize = new Vector2(0.7f, 0.18f);
    public Vector2 direction = Vector2.left;
    public bool fireBothDirections;
    public float speed = 8f;
    [Min(0.1f)] public float interval = 3f;
    [Min(0f)] public float warningTime = 0.7f;
    [Min(0f)] public float initialDelay;
    public float projectileLifetime = 2f;
    [Tooltip("Horizontal arrows travel to the end of the connected, level floor below the launcher.")]
    public bool autoArrowRange = true;
    public LayerMask terrainMask;
    public SpriteRenderer warningMarker;

    public bool IsActive { get; private set; }

    private Color idleColor;
    private float phaseEnds;
    private bool warning;
    private bool rangeResolved;
    private bool hasArrowRange;
    private float floorLeft;
    private float floorRight;
    private SpriteRenderer fallingStonePreview;
    private bool previewOriginallyEnabled;

    private void OnEnable()
    {
        fallingStonePreview = null;
        if (!fireBothDirections && direction.y < 0f && Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            Transform housing = transform.parent != null ? transform.parent.Find(name + "_Housing") : null;
            if (housing != null) fallingStonePreview = housing.GetComponent<SpriteRenderer>();
            if (fallingStonePreview != null) previewOriginallyEnabled = fallingStonePreview.enabled;
        }
        if (warningMarker != null) idleColor = warningMarker.color;
        IsActive = true;
        warning = false;
        rangeResolved = false;
        hasArrowRange = false;
        phaseEnds = Time.time + Mathf.Max(0f, initialDelay);
    }

    private void Update()
    {
        if (!IsActive || Time.time < phaseEnds) return;
        if (!warning)
        {
            warning = true;
            SetFallingStonePreview(true);
            phaseEnds = Time.time + Mathf.Max(0.05f, warningTime);
            return;
        }
        Fire();
        warning = false;
        if (warningMarker != null) warningMarker.color = idleColor;
        phaseEnds = Time.time + Mathf.Max(0.1f, interval - warningTime);
    }

    private void Fire()
    {
        if (projectileSprite == null) return;
        if (fireBothDirections)
        {
            SpawnProjectile(Vector2.left, 0.4f);
            SpawnProjectile(Vector2.right, 0.4f);
        }
        else SpawnProjectile(direction, 0f);
        SetFallingStonePreview(false);
    }

    private void SetFallingStonePreview(bool visible)
    {
        // Hide only the waiting stone, never the emitter or its warning marker.
        if (fallingStonePreview != null)
            fallingStonePreview.enabled = visible && previewOriginallyEnabled;
    }

    private void SpawnProjectile(Vector2 shotDirection, float offset)
    {
        GameObject shot = new GameObject("TrapProjectile");
        shot.transform.position = transform.position + (Vector3)(shotDirection.normalized * offset);
        SpriteRenderer visual = shot.AddComponent<SpriteRenderer>();
        visual.sprite = projectileSprite;
        visual.flipX = shotDirection.x > 0f;
        visual.color = projectileColor;
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
        body.linearVelocity = shotDirection.normalized * speed;
        StageTrapProjectile projectile = shot.AddComponent<StageTrapProjectile>();
        projectile.terrainMask = terrainMask;
        projectile.lifetime = GetLifetime(shotDirection, shot.transform.position.x);
    }

    private float GetLifetime(Vector2 shotDirection, float spawnX)
    {
        if (!autoArrowRange || Mathf.Abs(shotDirection.y) > 0.001f || Mathf.Abs(shotDirection.x) < 0.001f || speed <= 0.01f)
            return projectileLifetime;
        if (!rangeResolved) ResolveArrowRange();
        if (!hasArrowRange) return projectileLifetime;
        float distance = shotDirection.x > 0f ? floorRight - spawnX : spawnX - floorLeft;
        // Let the entire arrow clear the edge before its cleanup timer expires.
        return (Mathf.Max(0f, distance) + projectileSize.x + 0.2f) / speed;
    }

    private void ResolveArrowRange()
    {
        rangeResolved = true;
        Collider2D floor = null;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(transform.position, Vector2.down, 50f, terrainMask))
            if (!hit.collider.isTrigger)
            {
                floor = hit.collider;
                break;
            }
        if (floor == null) return;
        Bounds bounds = floor.bounds;
        floorLeft = bounds.min.x;
        floorRight = bounds.max.x;
        float surface = bounds.max.y;
        Collider2D[] terrain = FindObjectsByType<Collider2D>();
        // A visually continuous runway can consist of several touching floor objects.
        bool expanded;
        do
        {
            expanded = false;
            foreach (Collider2D candidate in terrain)
            {
                if (!candidate.isActiveAndEnabled || candidate.isTrigger ||
                    (terrainMask.value & (1 << candidate.gameObject.layer)) == 0 ||
                    (candidate.attachedRigidbody != null && candidate.attachedRigidbody.bodyType != RigidbodyType2D.Static)) continue;
                Bounds next = candidate.bounds;
                if (Mathf.Abs(next.max.y - surface) > 0.1f || next.min.x > floorRight + 0.05f || next.max.x < floorLeft - 0.05f) continue;
                float left = Mathf.Min(floorLeft, next.min.x);
                float right = Mathf.Max(floorRight, next.max.x);
                if (left == floorLeft && right == floorRight) continue;
                floorLeft = left;
                floorRight = right;
                expanded = true;
            }
        } while (expanded);
        hasArrowRange = true;
    }

    private void OnDisable()
    {
        SetFallingStonePreview(true);
        IsActive = false;
        warning = false;
        if (warningMarker != null) warningMarker.color = idleColor;
    }
}
