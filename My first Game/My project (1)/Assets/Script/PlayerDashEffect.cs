using UnityEngine;

public class PlayerDashEffect : MonoBehaviour
{
    private PlayerMove owner;
    private Collider2D body;
    private ParticleSystem particles;
    private Material material;
    private float direction;
    private float scale;
    private Color color;
    private float emissionTime;
    private bool stopped;

    public static PlayerDashEffect Play(PlayerMove owner, Collider2D body, float direction, float scale, Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        GameObject go = new GameObject("PlayerDashStreaks");
        PlayerDashEffect effect = go.AddComponent<PlayerDashEffect>();
        effect.owner = owner;
        effect.body = body;
        effect.direction = direction;
        effect.scale = Mathf.Clamp(scale, 0.3f, 2f);
        effect.color = color;
        effect.particles = go.AddComponent<ParticleSystem>();
        effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = effect.particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 10f;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize3D = true;
        var emission = effect.particles.emission;
        emission.enabled = false;
        var shape = effect.particles.shape;
        shape.enabled = false;
        var size = effect.particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var fade = effect.particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        effect.material = new Material(shader);
        effect.material.mainTexture = Texture2D.whiteTexture;
        var renderer = effect.particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = effect.material;
        renderer.sortingOrder = 4;
        effect.particles.Play();
        effect.Emit(4);
        return effect;
    }

    private void LateUpdate()
    {
        if (stopped) return;
        if (owner == null || !owner.isActiveAndEnabled || !owner.dashEffectsEnabled)
        {
            StopEffect();
            return;
        }
        emissionTime += Time.deltaTime;
        if (emissionTime >= 0.025f)
        {
            emissionTime %= 0.025f;
            Emit(2);
        }
    }

    private void Emit(int count)
    {
        Vector3 center = body != null ? body.bounds.center : owner.transform.position;
        float halfWidth = body != null ? body.bounds.extents.x : 0.25f;
        float halfHeight = body != null ? body.bounds.extents.y * 0.65f : 0.3f;
        for (int i = 0; i < count; i++)
            particles.Emit(new ParticleSystem.EmitParams
            {
                position = center + new Vector3(-direction * halfWidth, Random.Range(-halfHeight, halfHeight), 0f),
                velocity = new Vector3(-direction * Random.Range(1f, 2.5f), Random.Range(-0.15f, 0.15f), 0f),
                startSize3D = new Vector3(Random.Range(0.18f, 0.35f), Random.Range(0.025f, 0.05f), 1f) * scale,
                startLifetime = Random.Range(0.1f, 0.18f),
                startColor = color
            }, 1);
    }

    public void StopEffect()
    {
        if (stopped) return;
        stopped = true;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(gameObject, 0.22f);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
