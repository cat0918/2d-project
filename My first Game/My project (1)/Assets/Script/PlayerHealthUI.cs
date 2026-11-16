using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Image[] heartImages;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    private GameObject hud;
    private RectTransform panel;
    private RectTransform healthFill;
    private RectTransform dashFill;
    private Text healthValue;
    private Image dashImage;
    private PlayerMove movement;

    private void OnEnable()
    {
        HideHearts();
        if (hud == null) BuildHud();
        hud.SetActive(playerHealth != null);
    }

    private void OnDisable()
    {
        if (hud != null) hud.SetActive(false);
    }

    private void HideHearts()
    {
        if (heartImages == null) return;
        foreach (Image heart in heartImages)
            if (heart != null) heart.enabled = false;
    }

    private void LateUpdate()
    {
        if (hud == null) return;
        if (playerHealth == null) { hud.SetActive(false); return; }
        hud.SetActive(true);
        HideHearts();
        if (movement == null || movement.gameObject != playerHealth.gameObject)
            movement = playerHealth.GetComponent<PlayerMove>();
        float ratio = playerHealth.maxHealth > 0 ? (float)playerHealth.CurrentHealth / playerHealth.maxHealth : 0f;
        SetFill(healthFill, ratio);
        healthValue.text = playerHealth.CurrentHealth + " / " + playerHealth.maxHealth;
        bool alive = !playerHealth.IsDead;
        SetFill(dashFill, alive && movement != null ? movement.DashRecharge01 : 0f);
        bool ready = alive && movement != null && movement.IsDashReady;
        dashImage.color = ready ? new Color(0.35f, 0.87f, 0.88f) : new Color(0.2f, 0.48f, 0.57f);
        if (Screen.width > 0 && Screen.height > 0)
        {
            Rect safe = Screen.safeArea;
            panel.anchorMin = panel.anchorMax = new Vector2(safe.xMin / Screen.width, safe.yMax / Screen.height);
        }
    }

    private void BuildHud()
    {
        hud = new GameObject("PlayerStatusHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        panel = Rect("StatusPanel", hud.transform, new Vector2(24f, -24f), new Vector2(264f, 98f));
        AddImage(panel, new Color(0.035f, 0.05f, 0.065f, 0.9f));
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.57f, 0.5f, 0.32f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);
        Label("HealthLabel", "HP", new Vector2(16f, -8f), new Vector2(70f, 20f), TextAnchor.MiddleLeft);
        healthValue = Label("HealthValue", "", new Vector2(158f, -8f), new Vector2(90f, 20f), TextAnchor.MiddleRight);
        healthFill = Bar("Health", new Vector2(16f, -32f), new Vector2(232f, 17f), new Color(0.78f, 0.19f, 0.2f));
        Label("DashLabel", "DASH", new Vector2(16f, -55f), new Vector2(155f, 18f), TextAnchor.MiddleLeft);
        dashFill = Bar("Dash", new Vector2(16f, -78f), new Vector2(232f, 7f), Color.cyan);
        dashImage = dashFill.GetComponent<Image>();
    }

    private RectTransform Bar(string name, Vector2 position, Vector2 size, Color color)
    {
        RectTransform background = Rect(name + "Track", panel, position, size);
        AddImage(background, new Color(0.15f, 0.17f, 0.19f));
        RectTransform fill = Rect(name + "Fill", background, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        AddImage(fill, color);
        return fill;
    }

    private static void SetFill(RectTransform fill, float amount)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
    }

    private Text Label(string name, string value, Vector2 position, Vector2 size, TextAnchor alignment)
    {
        Text text = Rect(name, panel, position, size).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.text = value;
        text.alignment = alignment;
        text.color = new Color(0.91f, 0.88f, 0.77f);
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (hud != null) Destroy(hud);
    }
}
