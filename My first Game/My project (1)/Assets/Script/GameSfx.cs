using UnityEngine;

public enum GameSound { Swing, Hit, Hurt, Jump, Dash, Land, Cast, Impact, Slam, Rest, Pickup, Gate, BossMelee, BossDash, BossAxeThrow }

// Generated placeholders plus user-provided audio clips.
public class GameSfx : MonoBehaviour
{
    private const float MasterVolumeScale = 0.8f;
    private static GameSfx instance;
    private readonly AudioSource[] voices = new AudioSource[8];
    private static readonly int SoundCount = System.Enum.GetValues(typeof(GameSound)).Length;
    private readonly AudioClip[] clips = new AudioClip[SoundCount];
    private readonly bool[] generatedClips = new bool[SoundCount];
    private readonly float[] nextTimes = new float[SoundCount];
    private PlayerMove player;
    private AudioClip[] footsteps;
    private int lastFootstep = -1;
    private float nextFootstepTime;
    private readonly System.Random footstepRandom = new System.Random();

    public static void PlayFootstep(Vector3 position, float volume)
    {
        if (Time.timeScale <= 0f) return;
        if (instance == null) instance = new GameObject("GameSoundEffects").AddComponent<GameSfx>();
        if (instance.footsteps.Length == 0 || Time.unscaledTime < instance.nextFootstepTime) return;
        int count = instance.footsteps.Length;
        int index = instance.lastFootstep < 0 || count == 1 ? instance.footstepRandom.Next(count)
            : (instance.lastFootstep + 1 + instance.footstepRandom.Next(count - 1)) % count;
        if (instance.PlayClip(instance.footsteps[index], position, volume))
        {
            instance.lastFootstep = index;
            instance.nextFootstepTime = Time.unscaledTime + 0.1f;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; }

    public static void Play(GameSound sound, Vector3 position, float volume = 1f, float startTime = 0f)
    {
        if (Time.timeScale <= 0f) return;
        if (instance == null) instance = new GameObject("GameSoundEffects").AddComponent<GameSfx>();
        instance.PlaySound(sound, position, volume, startTime);
    }

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerMove>();
        footsteps = Resources.LoadAll<AudioClip>("Audio/Footsteps");
        for (int i = 0; i < clips.Length; i++)
        {
            if ((GameSound)i == GameSound.Hit)
                clips[i] = Resources.Load<AudioClip>("Audio/Combat/impactPlank_medium_001");
            else if ((GameSound)i == GameSound.Jump)
                clips[i] = Resources.Load<AudioClip>("Audio/Movement/cloth1");
            else if ((GameSound)i == GameSound.Land)
                clips[i] = Resources.Load<AudioClip>("Audio/Movement/bookPlace1");
            else if ((GameSound)i == GameSound.BossMelee)
                clips[i] = Resources.Load<AudioClip>("Audio/Combat/BossMeleeSwing");
            else if ((GameSound)i == GameSound.BossDash)
                clips[i] = Resources.Load<AudioClip>("Audio/Combat/BossDash");
            else if ((GameSound)i == GameSound.BossAxeThrow)
                clips[i] = Resources.Load<AudioClip>("Audio/Combat/BossAxeThrow");
            else if ((GameSound)i == GameSound.Slam)
                clips[i] = Resources.Load<AudioClip>("Audio/Combat/BossGroundImpact");
            if (clips[i] == null)
            {
                clips[i] = Synthesize((GameSound)i);
                generatedClips[i] = true;
            }
            nextTimes[i] = -1f;
        }
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
        }
    }

    private void PlaySound(GameSound sound, Vector3 position, float volume, float startTime)
    {
        if (player == null) player = FindAnyObjectByType<PlayerMove>();
        if (player != null && !player.soundEffectsEnabled) return;
        int index = (int)sound;
        if (Time.unscaledTime < nextTimes[index]) return;
        if (PlayClip(clips[index], position, volume, startTime))
            nextTimes[index] = Time.unscaledTime + (sound == GameSound.Impact ? 0.08f : 0.04f);
    }

    private bool PlayClip(AudioClip clip, Vector3 position, float volume, float startTime = 0f)
    {
        if (player == null) player = FindAnyObjectByType<PlayerMove>();
        if (clip == null || (player != null && !player.soundEffectsEnabled)) return false;
        Camera camera = Camera.main;
        float distance = camera != null ? Vector2.Distance(camera.transform.position, position) : 0f;
        float attenuation = 1f - Mathf.InverseLerp(8f, 24f, distance);
        if (attenuation <= 0f) return false;
        AudioSource voice = null;
        foreach (AudioSource candidate in voices)
            if (!candidate.isPlaying) { voice = candidate; break; }
        if (voice == null) return false;
        voice.clip = clip;
        voice.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.01f));
        voice.volume = MasterVolumeScale * Mathf.Clamp01(volume) * attenuation * (player != null ? Mathf.Clamp01(player.soundEffectsVolume) : 0.4f);
        voice.pitch = 1f;
        voice.Play();
        return true;
    }

    private static AudioClip Synthesize(GameSound sound)
    {
        if (sound == GameSound.BossMelee) sound = GameSound.Swing;
        if (sound == GameSound.BossAxeThrow) sound = GameSound.Swing;
        if (sound == GameSound.BossDash) sound = GameSound.Dash;
        const int rate = 22050;
        float duration = sound == GameSound.Gate ? 1.5f :
            sound == GameSound.Rest || sound == GameSound.Pickup ? 0.55f : sound == GameSound.Slam ? 0.4f : 0.18f;
        float[] data = new float[Mathf.CeilToInt(duration * rate)];
        var random = new System.Random(731 + (int)sound);
        float filtered = 0f;
        double phase = 0;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / rate;
            float p = t / duration;
            float noise = (float)random.NextDouble() * 2f - 1f;
            // Jump uses a darkening noise sweep instead of a rising pitched oscillator.
            float filterAmount = sound == GameSound.Jump ? Mathf.Lerp(0.45f, 0.06f, p) : 0.18f;
            if (sound == GameSound.Hit) filterAmount = 0.08f;
            filtered = Mathf.Lerp(filtered, noise, filterAmount);
            float frequency = 120f;
            float noiseMix = 0.5f;
            switch (sound)
            {
                case GameSound.Swing: frequency = Mathf.Lerp(550f, 120f, p); noiseMix = 0.9f; break;
                case GameSound.Dash: frequency = Mathf.Lerp(300f, 90f, p); noiseMix = 0.95f; break;
                case GameSound.Jump: noiseMix = 1f; break;
                case GameSound.Hit: frequency = 65f + 75f * Mathf.Exp(-t * 55f); noiseMix = 0.25f; break;
                case GameSound.Hurt: frequency = Mathf.Lerp(150f, 60f, p); noiseMix = 0.35f; break;
                case GameSound.Land: frequency = Mathf.Lerp(100f, 45f, p); noiseMix = 0.7f; break;
                case GameSound.Cast: frequency = Mathf.Lerp(180f, 650f, p); noiseMix = 0.6f; break;
                case GameSound.Impact: frequency = Mathf.Lerp(160f, 45f, p); noiseMix = 0.85f; break;
                case GameSound.Slam: frequency = Mathf.Lerp(95f, 32f, p); noiseMix = 0.6f; break;
                case GameSound.Gate: frequency = 55f + Mathf.Sin(t * 27f) * 8f; noiseMix = 0.85f; break;
                case GameSound.Rest: frequency = p < 0.5f ? 330f : 495f; noiseMix = 0.02f; break;
                case GameSound.Pickup: frequency = p < 0.33f ? 440f : p < 0.66f ? 550f : 660f; noiseMix = 0.02f; break;
            }
            phase += 2.0 * System.Math.PI * frequency / rate;
            float tone = (float)System.Math.Sin(phase);
            float envelope = Mathf.Min(1f, t / 0.008f) * Mathf.Pow(1f - p, sound == GameSound.Gate ? 0.5f : 1.7f);
            if (sound == GameSound.Jump)
                envelope = Mathf.Min(1f, t / 0.025f) * Mathf.Pow(1f - p, 1.5f);
            data[i] = Mathf.Clamp((tone * (1f - noiseMix) + filtered * noiseMix * 2f) * envelope * 0.55f, -0.8f, 0.8f);
            if (sound == GameSound.Hit)
            {
                // A short low thump with a damped impact transient, rather than a bright crack.
                float attack = Mathf.Min(1f, t / 0.003f);
                float body = (tone + 0.2f * (float)System.Math.Sin(phase * 2.1)) * Mathf.Exp(-t * 26f);
                float impact = filtered * 1.6f * Mathf.Exp(-t * 85f);
                float tail = Mathf.Clamp01((duration - t) / 0.025f);
                data[i] = Mathf.Clamp((body * 0.58f + impact * 0.3f) * attack * tail, -0.8f, 0.8f);
            }
        }
        AudioClip clip = AudioClip.Create("Generated_" + sound, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        // Imported assets are shared; only destroy clips created by this instance.
        for (int i = 0; i < clips.Length; i++)
            if (generatedClips[i] && clips[i] != null) Destroy(clips[i]);
        if (instance == this) instance = null;
    }
}
