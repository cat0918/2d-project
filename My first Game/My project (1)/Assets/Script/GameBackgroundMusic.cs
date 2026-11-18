using UnityEngine;

// Keep one music source across checkpoint reloads and returns to the title.
public class GameBackgroundMusic : MonoBehaviour
{
    private static GameBackgroundMusic instance;
    private AudioSource source;
    private const float MusicVolume = 0.18f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; }

    public static void EnsurePlaying()
    {
        if (instance == null)
            new GameObject("BackgroundMusic").AddComponent<GameBackgroundMusic>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        // Menus pause world audio, but music stays audible while adjusting master volume.
        source.ignoreListenerPause = true;
        source.volume = 0f;
        source.clip = Resources.Load<AudioClip>("Audio/Music/CitadelAmbient");
        if (source.clip != null) source.Play();
        else Debug.LogWarning("Background music clip was not found.", this);
    }

    private void Update()
    {
        source.volume = Mathf.MoveTowards(source.volume, MusicVolume, Time.unscaledDeltaTime * MusicVolume / 2f);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
