using UnityEngine;

public class BossSlamWarning : MonoBehaviour
{
    private Material material;
    private LineRenderer line;

    public static BossSlamWarning Create(Vector2 position, float radius)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        GameObject go = new GameObject("BossSlamDangerZone");
        go.transform.position = position;
        BossSlamWarning warning = go.AddComponent<BossSlamWarning>();
        warning.material = new Material(shader);
        warning.material.mainTexture = Texture2D.whiteTexture;
        warning.line = go.AddComponent<LineRenderer>();
        warning.line.sharedMaterial = warning.material;
        warning.line.useWorldSpace = false;
        warning.line.positionCount = 4;
        warning.line.widthMultiplier = 0.06f;
        warning.line.sortingOrder = 95;
        warning.line.SetPositions(new[] { new Vector3(-radius, 0.18f, 0f), new Vector3(-radius, 0f, 0f),
            new Vector3(radius, 0f, 0f), new Vector3(radius, 0.18f, 0f) });
        warning.Update();
        return warning;
    }

    private void Update()
    {
        Color color = new Color(1f, 0.45f, 0.18f, 0.7f + Mathf.Sin(Time.time * 12f) * 0.2f);
        line.startColor = line.endColor = color;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
