using UnityEngine;

[DisallowMultipleComponent]
public class FireballVisual : MonoBehaviour
{
    private Material flameMaterial;
    private Transform core;
    private float phase;

    public static void Attach(GameObject shot)
    {
        if (shot.GetComponent<FireballVisual>() != null) return;
        SpriteRenderer sprite = shot.GetComponentInChildren<SpriteRenderer>();
        Shader shader = Shader.Find("Sprites/Default");
        if (sprite == null || sprite.sprite == null || shader == null) return;
        FireballVisual visual = shot.AddComponent<FireballVisual>();
        visual.Build(sprite, shader);
    }

    private void Build(SpriteRenderer sprite, Shader shader)
    {
        phase = Random.value * Mathf.PI * 2f;
        flameMaterial = new Material(shader);
        flameMaterial.mainTexture = Texture2D.whiteTexture;
        sprite.color = new Color(1f, 0.25f, 0.025f);
        core = AddLayer(sprite, "FlameCore", 0.68f, new Color(1f, 0.75f, 0.12f), 1);
        AddLayer(sprite, "FlameCenter", 0.32f, new Color(1f, 0.97f, 0.65f), 2);

        float diameter = Mathf.Max(0.08f, sprite.bounds.size.y);
        GameObject tail = new GameObject("FlameTail");
        tail.transform.SetParent(transform, false);
        TrailRenderer trail = tail.AddComponent<TrailRenderer>();
        trail.sharedMaterial = flameMaterial;
        trail.time = 0.16f;
        trail.minVertexDistance = 0.025f;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.widthMultiplier = diameter * 0.85f;
        trail.colorGradient = FlameGradient();
        trail.sortingLayerID = sprite.sortingLayerID;
        trail.sortingOrder = sprite.sortingOrder - 1;
        trail.numCapVertices = 2;

        GameObject embers = new GameObject("FireEmbers");
        embers.transform.SetParent(transform, false);
        ParticleSystem particles = embers.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.maxParticles = 16;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(diameter * 0.12f, diameter * 0.3f);
        main.startSpeed = 0f;
        var shape = particles.shape;
        shape.enabled = false;
        var emission = particles.emission;
        emission.rateOverTime = 24f;
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = 0.35f;
        var color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = FlameGradient();
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = flameMaterial;
        renderer.sortingLayerID = sprite.sortingLayerID;
        renderer.sortingOrder = sprite.sortingOrder;
        particles.Play();
    }

    private static Transform AddLayer(SpriteRenderer source, string name, float scale, Color color, int order)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(source.transform, false);
        layer.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.color = color;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + order;
        return layer.transform;
    }

    private static Gradient FlameGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] {
            new GradientColorKey(new Color(1f, 0.8f, 0.15f), 0f),
            new GradientColorKey(new Color(1f, 0.3f, 0.02f), 0.4f),
            new GradientColorKey(new Color(0.7f, 0.08f, 0.01f), 1f)
        }, new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    private void Update()
    {
        if (core != null)
            core.localScale = Vector3.one * (0.68f + Mathf.Sin(Time.time * 28f + phase) * 0.05f);
    }

    public void PlayImpact(Vector2 point)
    {
        // Independent bursts survive the projectile; cleanup alone never triggers them.
        CombatHitBurst.Play(point, 1f, 8, new Color(1f, 0.55f, 0.06f));
        CombatHitBurst.Play(point, -1f, 8, new Color(1f, 0.85f, 0.25f));
    }

    private void OnDestroy()
    {
        if (flameMaterial != null) Destroy(flameMaterial);
    }
}
