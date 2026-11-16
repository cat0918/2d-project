using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInteraction))]
public class InteractionPromptUI : MonoBehaviour
{
    private PlayerInteraction interaction;
    private Collider2D body;
    private GameObject uiRoot;
    private Canvas canvas;
    private RectTransform panel;
    private Text keyText;
    private Text actionText;
    private Font font;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        body = GetComponent<Collider2D>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 20);
        uiRoot = new GameObject("PlayerInteractionPrompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = uiRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = MakeRect("Prompt", uiRoot.transform, new Vector2(230f, 44f));
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.055f, 0.075f, 0.93f);
        background.raycastTarget = false;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.7f, 0.6f, 0.35f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform badge = MakeRect("Key", panel, new Vector2(32f, 30f));
        badge.anchoredPosition = new Vector2(-89f, 0f);
        Image keyBackground = badge.gameObject.AddComponent<Image>();
        keyBackground.color = new Color(0.88f, 0.79f, 0.55f);
        keyBackground.raycastTarget = false;
        keyText = MakeText("KeyText", badge, new Vector2(30f, 28f), 17, new Color(0.08f, 0.1f, 0.12f));
        actionText = MakeText("Action", panel, new Vector2(174f, 36f), 17, new Color(0.96f, 0.94f, 0.87f));
        actionText.rectTransform.anchoredPosition = new Vector2(22f, 0f);
        uiRoot.SetActive(false);
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    private Text MakeText(string name, Transform parent, Vector2 size, int fontSize, Color color)
    {
        Text text = MakeRect(name, parent, size).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 11;
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private void LateUpdate()
    {
        Interactable target = interaction.CurrentTarget;
        Camera camera = Camera.main;
        if (!interaction.isActiveAndEnabled || target == null || !target.isActiveAndEnabled || camera == null || Time.timeScale <= 0f)
        {
            uiRoot.SetActive(false);
            return;
        }
        Vector3 head = body != null ? new Vector3(body.bounds.center.x, body.bounds.max.y, transform.position.z)
            : transform.position + Vector3.up;
        Vector3 screen = camera.WorldToScreenPoint(head);
        if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height)
        {
            uiRoot.SetActive(false);
            return;
        }
        uiRoot.SetActive(true);
        float scale = Mathf.Max(0.01f, canvas.scaleFactor);
        screen.y += 38f * scale;
        screen.x = Mathf.Clamp(screen.x, 120f * scale, Mathf.Max(120f * scale, Screen.width - 120f * scale));
        screen.y = Mathf.Clamp(screen.y, 26f * scale, Mathf.Max(26f * scale, Screen.height - 26f * scale));
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out Vector2 local);
        panel.anchoredPosition = local;
        keyText.text = interaction.interactKey.ToString();
        actionText.text = target.InteractionLabel;
    }

    private void OnDisable()
    {
        if (uiRoot != null) uiRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (uiRoot != null) Destroy(uiRoot);
        if (font != null) Destroy(font);
    }
}
