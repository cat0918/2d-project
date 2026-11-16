using UnityEngine;

public class BossSlamImpact : MonoBehaviour
{
    private Material material;
    private LineRenderer outer;
    private LineRenderer ripple;
    private float radius;
    private float height;
    private float elapsed;
    private const float Lifetime = 0.32f;

    public static void Play(Vector2 position, float radius, float height)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        GameObject go = new GameObject("BossSlamImpactWave");
        go.transform.position = position;
        BossSlamImpact effect = go.AddComponent<BossSlamImpact>();
        effect.radius = radius;
        effect.height = height;
        effect.material = new Material(shader);
        effect.material.mainTexture = Texture2D.whiteTexture;
        effect.outer = effect.CreateLine("ImpactEdge", 0.07f);
        effect.ripple = effect.CreateLine("ExpandingRipple", 0.045f);
        effect.Draw();
        Destroy(go, Lifetime);
    }

    private LineRenderer CreateLine(string name, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.positionCount = 25;
        line.widthMultiplier = width;
        line.sortingOrder = 95;
        return line;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        Draw();
    }

    private void Draw()
    {
        float fade = Mathf.Clamp01(1f - elapsed / Lifetime);
        DrawArc(outer, 1f, new Color(0.65f, 0.94f, 1f, fade));
        DrawArc(ripple, Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(elapsed / 0.14f)),
            new Color(0.25f, 0.7f, 1f, fade * 0.8f));
    }

    private void DrawArc(LineRenderer line, float scale, Color color)
    {
        line.startColor = line.endColor = color;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI / (line.positionCount - 1);
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius * scale,
                Mathf.Sin(angle) * height * scale + 0.025f, 0f));
        }
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
