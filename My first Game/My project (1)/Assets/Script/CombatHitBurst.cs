using UnityEngine;

public class CombatHitBurst : MonoBehaviour
{
    private Material ownedMaterial;

    public static void Play(Vector2 point, float direction, int count, Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        GameObject effect = new GameObject("CombatHitSparks");
        effect.transform.position = point;
        CombatHitBurst owner = effect.AddComponent<CombatHitBurst>();
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.25f;
        main.maxParticles = 16;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        owner.ownedMaterial = new Material(shader);
        owner.ownedMaterial.mainTexture = Texture2D.whiteTexture;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = owner.ownedMaterial;
        renderer.sortingOrder = 100;
        particles.Play();
        for (int i = 0; i < Mathf.Clamp(count, 1, 16); i++)
        {
            float angle = Random.Range(-1.15f, 1.15f);
            var spark = new ParticleSystem.EmitParams
            {
                position = point,
                velocity = new Vector3(Mathf.Cos(angle) * direction, Mathf.Sin(angle), 0f) * Random.Range(1.8f, 4f),
                startSize = Random.Range(0.04f, 0.085f),
                startLifetime = Random.Range(0.12f, 0.22f),
                startColor = color,
                rotation = Random.Range(0f, 180f)
            };
            particles.Emit(spark, 1);
        }
        Destroy(effect, 0.4f);
    }

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
    }
}
