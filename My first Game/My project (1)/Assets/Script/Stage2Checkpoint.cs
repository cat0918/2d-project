using UnityEngine;

public class Stage2Checkpoint : Interactable
{
    public override string InteractionLabel => "\uD734\uC2DD / \uC800\uC7A5";

    private void OnEnable()
    {
        if (statusLabel != null)
        {
            Renderer labelRenderer = statusLabel.GetComponent<Renderer>();
            if (labelRenderer != null) labelRenderer.enabled = false;
        }
    }
    public Stage2Course course;
    public Transform respawnPoint;
    public SpriteRenderer indicator;
    public TextMesh statusLabel;
    private MaterialPropertyBlock indicatorProperties;
    private float nextRestTime;
    private float restMessageUntil;
    private static readonly int GemActive = Shader.PropertyToID("_GemActive");
    private static readonly int SpriteRect = Shader.PropertyToID("_SpriteRect");

    public override bool CanInteract(PlayerMove player)
    {
        if (!isActiveAndEnabled || player == null || course == null || respawnPoint == null ||
            Time.unscaledTime < nextRestTime) return false;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        return health == null || !health.IsDead;
    }

    public override void Interact(PlayerMove player)
    {
        if (!CanInteract(player)) return;
        nextRestTime = Time.unscaledTime + 0.75f;
        course.SetCheckpoint(respawnPoint);
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) health.RestAtCheckpoint();
        CheckpointEnemyRespawner.RespawnForRest(gameObject.scene);
        restMessageUntil = Time.unscaledTime + 1.5f;
        Debug.Log("Rested: checkpoint saved, health restored and regular enemies respawned.", this);
    }

    private void Update()
    {
        bool selected = course != null && course.ActiveCheckpoint == respawnPoint;
        if (indicator != null)
        {
            if (indicator.sharedMaterial != null && indicator.sharedMaterial.HasProperty(GemActive) && indicator.sprite != null)
            {
                indicator.color = Color.white;
                if (indicatorProperties == null) indicatorProperties = new MaterialPropertyBlock();
                indicator.GetPropertyBlock(indicatorProperties);
                Rect rect = indicator.sprite.rect;
                Texture2D texture = indicator.sprite.texture;
                indicatorProperties.SetVector(SpriteRect, new Vector4(rect.x / texture.width, rect.y / texture.height,
                    rect.width / texture.width, rect.height / texture.height));
                indicatorProperties.SetFloat(GemActive, selected ? 1 : 0);
                indicator.SetPropertyBlock(indicatorProperties);
            }
            else
                indicator.color = selected ? new Color(0.35f, 1f, 0.7f) : new Color(0.5f, 0.65f, 0.85f);
        }
        if (statusLabel != null)
            statusLabel.text = Time.unscaledTime < restMessageUntil ? "RESTED / HEALTH RESTORED" :
                selected ? "E : REST (SAVED)" : "E : REST & SAVE";
    }
}
