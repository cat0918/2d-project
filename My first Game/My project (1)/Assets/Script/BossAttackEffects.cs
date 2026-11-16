using UnityEngine;

public class BossAttackEffects : MonoBehaviour
{
    public static readonly Color Frost = new Color(0.35f, 0.85f, 1f, 0.85f);
    private Material ownedMaterial;
    private Transform anchor;
    private Collider2D body;
    private LineRenderer ring;
    private float duration;
    private float elapsed;
    private float radius;
    private bool atFeet;

    public static BossAttackEffects Prepare(Transform anchor, Collider2D body, float duration, bool atFeet)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null || duration <= 0f) return null;
        GameObject go = new GameObject("BossAttackPreparation");
        BossAttackEffects effect = go.AddComponent<BossAttackEffects>();
        effect.anchor = anchor;
        effect.body = body;
        effect.duration = duration;
        effect.atFeet = atFeet;
        effect.radius = atFeet && body != null ? Mathf.Max(0.35f, body.bounds.extents.x * 1.3f) : 0.35f;
        effect.ownedMaterial = new Material(shader);
        effect.ownedMaterial.mainTexture = Texture2D.whiteTexture;
        effect.ring = go.AddComponent<LineRenderer>();
        effect.ring.sharedMaterial = effect.ownedMaterial;
        effect.ring.useWorldSpace = true;
        effect.ring.loop = true;
        effect.ring.positionCount = 24;
        effect.ring.widthMultiplier = 0.035f;
        effect.ring.sortingOrder = 90;
        effect.Draw();
        return effect;
    }

    private void Update()
    {
        if (ring == null) return;
        if (anchor == null || !anchor.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }
        elapsed += Time.deltaTime;
        if (elapsed >= duration) { Destroy(gameObject); return; }
        Draw();
    }

    private void Draw()
    {
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        Vector3 center = atFeet ? Feet(anchor, body) : anchor.position;
        float size = radius * Mathf.Lerp(1.5f, 0.6f, progress);
        Color color = Frost;
        color.a = Mathf.Lerp(0.25f, 0.9f, progress);
        ring.startColor = ring.endColor = color;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * size,
                Mathf.Sin(angle) * size * (atFeet ? 0.22f : 1f), 0f));
        }
    }

    public static Vector3 Feet(Transform actor, Collider2D body)
    {
        return body != null ? new Vector3(body.bounds.center.x, body.bounds.min.y + 0.04f, actor.position.z) : actor.position;
    }

    public static void Burst(Vector2 position)
    {
        CombatHitBurst.Play(position, -1f, 7, Frost);
        CombatHitBurst.Play(position, 1f, 7, new Color(0.8f, 0.95f, 1f));
    }

    public static void AttachAxeTrail(GameObject axe)
    {
        if (axe.GetComponent<BossAttackEffects>() != null) return;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        BossAttackEffects effect = axe.AddComponent<BossAttackEffects>();
        effect.ownedMaterial = new Material(shader);
        effect.ownedMaterial.mainTexture = Texture2D.whiteTexture;
        GameObject go = new GameObject("AxeFrostTrail");
        go.transform.SetParent(axe.transform, false);
        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = effect.ownedMaterial;
        trail.time = 0.14f;
        trail.minVertexDistance = 0.035f;
        SpriteRenderer sprite = axe.GetComponentInChildren<SpriteRenderer>();
        trail.widthMultiplier = sprite != null ? Mathf.Clamp(sprite.bounds.size.y * 0.4f, 0.08f, 0.35f) : 0.15f;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.startColor = Frost;
        trail.endColor = new Color(0.1f, 0.4f, 0.8f, 0f);
        trail.sortingLayerID = sprite != null ? sprite.sortingLayerID : 0;
        trail.sortingOrder = sprite != null ? sprite.sortingOrder - 1 : 0;
    }

    public void StopEffect()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
    }
}
