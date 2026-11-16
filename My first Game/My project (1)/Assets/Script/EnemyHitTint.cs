using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class EnemyHitTint : MonoBehaviour
{
    private SpriteRenderer[] sprites;
    private Color[] colors;
    private float endTime;
    private bool pulsing;

    public static void Pulse(GameObject target, float duration)
    {
        EnemyHitTint tint = target.GetComponent<EnemyHitTint>();
        if (tint == null) tint = target.AddComponent<EnemyHitTint>();
        if (!tint.pulsing)
        {
            tint.sprites = target.GetComponentsInChildren<SpriteRenderer>();
            tint.colors = new Color[tint.sprites.Length];
            for (int i = 0; i < tint.sprites.Length; i++)
                tint.colors[i] = tint.sprites[i].color;
        }
        tint.endTime = Time.time + duration;
        tint.pulsing = true;
    }

    private void LateUpdate()
    {
        if (!pulsing) return;
        if (Time.time >= endTime) { Restore(); return; }
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null || sprites[i].name.ToLowerInvariant().Contains("shadow")) continue;
            Color color = Color.Lerp(colors[i], new Color(1f, 0.5f, 0.25f), 0.65f);
            color.a = colors[i].a;
            sprites[i].color = color;
        }
    }

    private void Restore()
    {
        if (!pulsing) return;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = colors[i];
        pulsing = false;
    }

    private void OnDisable() { Restore(); }
}
