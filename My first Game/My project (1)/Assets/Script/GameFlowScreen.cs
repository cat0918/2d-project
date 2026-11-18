using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Session flags survive death reloads, but reset for a fresh Play session.
[DefaultExecutionOrder(-1000)]
public class GameFlowScreen : MonoBehaviour
{
    private const string VolumePreference = "Audio.MasterVolume";
    private static GameFlowScreen instance;
    private static bool started;
    private static bool completed;
    private static int resumeFrame;
    private GameObject screen;
    private CanvasGroup group;
    private Font font;
    private bool visible;
    private bool endingPending;
    private float previousTimeScale = 1f;
    private float revealTime;
    private RawImage titleBackground;
    private bool pauseMenu;
    private bool loading;
    private bool previousAudioPause;
    private Text volumeLabel;
    private Text menuMessage;

    public static bool BlocksInput => (instance != null && (instance.visible || instance.loading)) || Time.frameCount <= resumeFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
        started = completed = false;
        resumeFrame = -1;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePreference, 1f));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register() { SceneManager.sceneLoaded += OnSceneLoaded; }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != null || FindAnyObjectByType<PlayerMove>() == null) return;
        instance = new GameObject("GameFlowScreen").AddComponent<GameFlowScreen>();
        GameBackgroundMusic.EnsurePlaying();
        if (!started)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            instance.Show(false);
        }
    }

    public static void BossDefeated(EnemyHealth bossHealth)
    {
        if (completed || instance == null || instance.endingPending) return;
        instance.endingPending = true;
        instance.StartCoroutine(instance.WaitForEnding(bossHealth));
    }

    private IEnumerator WaitForEnding(EnemyHealth bossHealth)
    {
        // Let the death animation and disappearance finish before pausing the world.
        while (bossHealth != null) yield return null;
        yield return new WaitForSeconds(0.6f);
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        endingPending = false;
        if (player != null && !player.IsDead)
        {
            completed = true;
            Show(true);
        }
    }

    private void Show(bool ending, bool paused = false)
    {
        pauseMenu = paused;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        visible = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        revealTime = Time.unscaledTime;
        font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 32);
        screen = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        screen.transform.SetParent(transform, false);
        Canvas canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        group = screen.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        RectTransform background = Box("Backdrop", screen.transform, Vector2.zero, Vector2.zero, new Color(0.025f, 0.04f, 0.06f));
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.sizeDelta = Vector2.zero;
        if (paused)
        {
            background.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.04f, 0.88f);
            Box("PauseRule", screen.transform, new Vector2(0f, 190f), new Vector2(230f, 2f), new Color(0.72f, 0.62f, 0.4f));
            Button continueButton = MakeButton("계속하기", 42f, Resume);
            CreateVolumeSlider();
            MakeButton("게임 종료", -135f, ReturnToTitle);
            menuMessage = Label("PauseFooter", "게임 종료 시 시작 화면으로 돌아갑니다.\n다시 시작하면 마지막 체크포인트에서 이어집니다.", 16, -230f, new Color(0.67f, 0.73f, 0.75f));
            SelectButton(continueButton);
            return;
        }
        if (ending || !CreateTitleBackground())
        {
            // Retain the gate silhouette for the ending and missing-asset fallback.
            Color stone = new Color(0.055f, 0.085f, 0.11f);
            Box("LeftTower", screen.transform, new Vector2(-425f, -30f), new Vector2(130f, 470f), stone);
            Box("RightTower", screen.transform, new Vector2(425f, -30f), new Vector2(130f, 470f), stone);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 3; i++)
                    Box("Battlement", screen.transform, new Vector2(side * 425f + (i - 1) * 46f, 220f), new Vector2(34f, 40f), stone);
        }
        Color gold = new Color(0.72f, 0.62f, 0.4f);
        Box("TopRule", screen.transform, new Vector2(0f, 240f), new Vector2(230f, 2f), gold);
        Label("Eyebrow", ending ? "THE SILENCE RETURNS" : "BEYOND THE FORGOTTEN GATE", 16, 200f, gold);
        Label("Title", ending ? "성채의 침묵" : "RUINED CITADEL", ending ? 48 : 54, 115f, new Color(0.94f, 0.9f, 0.8f));
        Label("Subtitle", ending ? "성채의 수호자를 쓰러뜨렸습니다." : "잊힌 성문 너머로", 20, 48f, new Color(0.57f, 0.66f, 0.7f));
        Button primary = MakeButton(ending ? "계속 탐험하기" : "게임 시작", -65f, Resume);
        MakeButton("게임 종료", -131f, Quit);
        Label("Footer", ending ? "플레이해 주셔서 감사합니다." : "이동 A / D · 점프 SPACE · 공격 J / 클릭 · 대시 SHIFT · 상호작용 E", 15, -265f, new Color(0.52f, 0.58f, 0.61f));
        SelectButton(primary);
    }

    private void Update()
    {
        if (!loading && Input.GetKeyDown(KeyCode.Escape))
        {
            if (visible && pauseMenu) Resume();
            else if (!visible && started && !BlocksInput && Time.timeScale > 0f)
                Show(false, true);
        }
        if (!visible) return;
        group.alpha = Mathf.Clamp01((Time.unscaledTime - revealTime) / 0.4f);
        FitTitleBackground();
    }

    private bool CreateTitleBackground()
    {
        Texture2D texture = Resources.Load<Texture2D>("UI/TitleFortress");
        if (texture == null) return false;
        texture.filterMode = FilterMode.Point;
        titleBackground = new GameObject("FortressLandscape", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        titleBackground.transform.SetParent(screen.transform, false);
        titleBackground.texture = texture;
        titleBackground.raycastTarget = false;
        Stretch(titleBackground.rectTransform);
        FitTitleBackground();
        RectTransform shade = Box("LandscapeShade", screen.transform, Vector2.zero, Vector2.zero, new Color(0.015f, 0.025f, 0.045f, 0.38f));
        Stretch(shade);
        return true;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void FitTitleBackground()
    {
        if (titleBackground == null || Screen.height <= 0) return;
        // Center-crop rather than stretching the artwork or leaving empty borders.
        float imageAspect = (float)titleBackground.texture.width / titleBackground.texture.height;
        float screenAspect = (float)Screen.width / Screen.height;
        float width = Mathf.Min(1f, screenAspect / imageAspect);
        float height = Mathf.Min(1f, imageAspect / screenAspect);
        titleBackground.uvRect = new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height);
    }

    private void Resume()
    {
        if (loading) return;
        PlayerPrefs.Save();
        started = true;
        visible = false;
        pauseMenu = false;
        resumeFrame = Time.frameCount + 1;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        Destroy(screen);
        if (font != null) Destroy(font);
    }

    private static void SelectButton(Button button)
    {
        if (EventSystem.current == null)
            new GameObject("MenuEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void CreateVolumeSlider()
    {
        volumeLabel = Label("VolumeLabel", "", 18, -24f, new Color(0.85f, 0.86f, 0.8f));
        RectTransform track = Box("MasterVolume", screen.transform, new Vector2(0f, -65f), new Vector2(310f, 24f), new Color(0.1f, 0.15f, 0.18f));
        track.GetComponent<Image>().raycastTarget = true;
        Slider slider = track.gameObject.AddComponent<Slider>();
        RectTransform fill = Box("Fill", track, Vector2.zero, Vector2.zero, new Color(0.62f, 0.73f, 0.72f));
        Stretch(fill);
        fill.offsetMin = new Vector2(0f, 8f);
        fill.offsetMax = new Vector2(0f, -8f);
        RectTransform handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(track, false);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);
        RectTransform handle = Box("Handle", handleArea, Vector2.zero, new Vector2(16f, 30f), new Color(0.87f, 0.78f, 0.56f));
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(AudioListener.volume);
        UpdateVolume(AudioListener.volume);
        slider.onValueChanged.AddListener(UpdateVolume);
    }

    private void UpdateVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumePreference, AudioListener.volume);
        volumeLabel.text = "소리  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
    }

    private void ReturnToTitle()
    {
        if (loading) return;
        PlayerMove player = FindAnyObjectByType<PlayerMove>();
        string path = player != null ? player.gameObject.scene.path : gameObject.scene.path;
        if (string.IsNullOrEmpty(path) || !Application.CanStreamedLevelBeLoaded(path))
        {
            menuMessage.text = "현재 씬을 Build Profiles의 Scene List에 추가해 주세요.";
            return;
        }
        PlayerPrefs.Save();
        RunCheckpointState.PrepareRestart(player, path);
        loading = true;
        group.interactable = false;
        started = false;
        bool wasCompleted = completed;
        completed = false;
        // The new scene owns its title pause; this instance must not unpause it on destruction.
        visible = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            if (operation == null) throw new System.InvalidOperationException("Scene reload could not start.");
            // Keep the old world frozen while the scene loads. The new title resumes at normal speed.
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
        catch (System.Exception exception)
        {
            RunCheckpointState.CancelRestart();
            loading = false;
            visible = started = true;
            completed = wasCompleted;
            group.interactable = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            menuMessage.text = "시작 화면으로 돌아가지 못했습니다. 다시 시도해 주세요.";
            Debug.LogException(exception, this);
        }
    }

    private void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.GetComponent<Image>().color = color;
        rect.GetComponent<Image>().raycastTarget = name == "Backdrop";
        return rect;
    }

    private Text Label(string name, string value, int size, float y, Color color)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
        rect.SetParent(screen.transform, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(900f, 76f);
        Text text = rect.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.01f, 0.015f, 0.025f, 0.9f);
        shadow.effectDistance = new Vector2(1f, -2f);
        return text;
    }

    private Button MakeButton(string caption, float y, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Box(caption, screen.transform, new Vector2(0f, y), new Vector2(310f, 52f), new Color(0.13f, 0.19f, 0.22f));
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1.5f, 1.5f, 1.3f);
        button.colors = colors;
        button.onClick.AddListener(action);
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.52f, 0.34f);
        outline.effectDistance = Vector2.one;
        Label(caption + "Label", caption, 20, y, new Color(0.94f, 0.9f, 0.8f));
        return button;
    }

    private void OnDestroy()
    {
        if (visible)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
        }
        if (font != null) Destroy(font);
        if (instance == this) instance = null;
    }
}
