using UnityEngine;

public class MovementDustBurst : MonoBehaviour
{
    private Material ownedMaterial;

    public static void Play(Vector2 feet, bool landing, bool airJump, float scale, Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        scale = Mathf.Clamp(scale, 0.3f, 2f);
        GameObject effect = new GameObject(landing ? "LandingDust" : "JumpDust");
        effect.transform.position = feet;
        MovementDustBurst owner = effect.AddComponent<MovementDustBurst>();
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.4f;
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        owner.ownedMaterial = new Material(shader);
        owner.ownedMaterial.mainTexture = Texture2D.whiteTexture;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = owner.ownedMaterial;
        renderer.sortingOrder = 100;
        if (airJump) color = new Color(0.65f, 0.85f, 1f, color.a);
        particles.Play();
        int count = landing ? 10 : 6;
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            particles.Emit(new ParticleSystem.EmitParams
            {
                position = feet + new Vector2(Random.Range(-0.18f, 0.18f) * scale, 0f),
                velocity = new Vector3(side * Random.Range(0.5f, landing ? 2f : 1.2f),
                    airJump ? Random.Range(-0.7f, -0.2f) : Random.Range(0.15f, 0.65f), 0f) * scale,
                startSize = Random.Range(0.055f, 0.12f) * scale,
                startLifetime = Random.Range(0.2f, 0.38f),
                startColor = color,
                rotation = 0f
            }, 1);
        }
        Destroy(effect, 0.6f);
    }

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
    }
}
