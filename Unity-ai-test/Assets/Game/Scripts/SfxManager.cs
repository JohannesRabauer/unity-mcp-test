using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally synthesizes all game sound effects at runtime (no external audio
/// assets, so everything is royalty-free and matches the synthwave aesthetic).
/// Call <see cref="Play"/> from anywhere; a self-created GameObject hosts it.
/// </summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    const int SR = 44100;
    readonly Dictionary<string, AudioClip> _clips = new();
    AudioSource[] _voices;
    int _voiceIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SfxManager");
        go.AddComponent<SfxManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // A small pool of voices so overlapping shots don't cut each other off.
        _voices = new AudioSource[8];
        for (int i = 0; i < _voices.Length; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            _voices[i] = s;
        }

        if (FindFirstObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        BuildAllClips();
    }

    public static void Play(string id, float volume = 1f, float pitch = 1f)
    {
        if (Instance != null) Instance.PlayInternal(id, volume, pitch);
    }

    void PlayInternal(string id, float volume, float pitch)
    {
        if (!_clips.TryGetValue(id, out var clip) || clip == null) return;
        var v = _voices[_voiceIndex];
        _voiceIndex = (_voiceIndex + 1) % _voices.Length;
        v.pitch = Mathf.Clamp(pitch, 0.2f, 3f);
        v.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ---------------------------------------------------------------- clips

    void BuildAllClips()
    {
        // Weapons
        Add("shot_pistol", Mix(
            Tone(150f, 0.16f, Wave.Saw, 0f, 34f, 0.5f, 60f),
            Noise(0.10f, 0f, 40f, 0.5f, 0.55f)));
        Add("shot_smg", Mix(
            Tone(220f, 0.09f, Wave.Square, 0f, 55f, 0.4f, 90f),
            Noise(0.06f, 0f, 70f, 0.45f, 0.5f)));
        Add("shot_shotgun", Mix(
            Tone(90f, 0.30f, Wave.Saw, 0f, 16f, 0.6f, 45f),
            Noise(0.22f, 0f, 16f, 0.7f, 0.7f)));
        Add("shot_laser", Mix(
            Tone(900f, 0.22f, Wave.Square, 0f, 26f, 0.35f, 180f),
            Tone(1800f, 0.18f, Wave.Sine, 0f, 30f, 0.18f, 300f)));

        // Feedback
        Add("reload", Concat(
            Noise(0.04f, 0.002f, 90f, 0.4f, 0.2f),
            Silence(0.10f),
            Tone(420f, 0.05f, Wave.Square, 0.001f, 50f, 0.3f),
            Silence(0.06f),
            Tone(620f, 0.06f, Wave.Square, 0.001f, 45f, 0.35f)));
        Add("dry", Tone(300f, 0.05f, Wave.Square, 0.001f, 80f, 0.3f));
        Add("hit", Mix(
            Tone(500f, 0.06f, Wave.Triangle, 0f, 60f, 0.35f),
            Noise(0.05f, 0f, 80f, 0.4f, 0.3f)));
        Add("pickup", Concat(
            Tone(660f, 0.08f, Wave.Square, 0.002f, 20f, 0.3f),
            Tone(990f, 0.12f, Wave.Square, 0.002f, 16f, 0.32f)));
        Add("explosion", Mix(
            Noise(0.55f, 0.003f, 7f, 0.8f, 0.85f),
            Tone(70f, 0.45f, Wave.Sine, 0.003f, 9f, 0.6f, 35f)));
        Add("car_start", Tone(70f, 0.45f, Wave.Saw, 0.02f, 3f, 0.4f, 150f));
        Add("car_stop", Tone(150f, 0.35f, Wave.Saw, 0.01f, 5f, 0.35f, 55f));
        Add("ui", Concat(
            Tone(520f, 0.06f, Wave.Square, 0.002f, 30f, 0.3f),
            Tone(780f, 0.10f, Wave.Square, 0.002f, 22f, 0.32f)));
        Add("quest", Concat(
            Tone(523f, 0.10f, Wave.Square, 0.002f, 18f, 0.3f),
            Tone(659f, 0.10f, Wave.Square, 0.002f, 18f, 0.3f),
            Tone(784f, 0.10f, Wave.Square, 0.002f, 16f, 0.3f),
            Tone(1047f, 0.20f, Wave.Square, 0.002f, 12f, 0.34f)));
        Add("switch", Tone(700f, 0.05f, Wave.Square, 0.001f, 45f, 0.28f, 900f));
    }

    void Add(string id, float[] samples)
    {
        Normalize(samples, 0.9f);
        var clip = AudioClip.Create(id, samples.Length, 1, SR, false);
        clip.SetData(samples, 0);
        _clips[id] = clip;
    }

    // ---------------------------------------------------------------- synth

    enum Wave { Sine, Square, Saw, Triangle }

    float[] Tone(float freq, float dur, Wave wave, float attack, float decay,
        float vol, float freqEnd = -1f)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        var a = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float u = (float)i / n;
            float t = (float)i / SR;
            float f = freqEnd > 0f ? Mathf.Lerp(freq, freqEnd, u) : freq;
            phase += 2f * Mathf.PI * f / SR;
            float ph = phase / (2f * Mathf.PI);
            float s;
            switch (wave)
            {
                case Wave.Square: s = Mathf.Sin(phase) >= 0f ? 1f : -1f; break;
                case Wave.Saw: s = 2f * (ph - Mathf.Floor(ph)) - 1f; break;
                case Wave.Triangle: s = Mathf.Asin(Mathf.Sin(phase)) * (2f / Mathf.PI); break;
                default: s = Mathf.Sin(phase); break;
            }
            float env = (attack > 0f ? Mathf.Min(1f, t / attack) : 1f) * Mathf.Exp(-decay * t);
            a[i] = s * env * vol;
        }
        return a;
    }

    float[] Noise(float dur, float attack, float decay, float vol, float smooth)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        var a = new float[n];
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float r = Random.Range(-1f, 1f);
            prev = Mathf.Lerp(r, prev, smooth); // one-pole lowpass
            float env = (attack > 0f ? Mathf.Min(1f, t / attack) : 1f) * Mathf.Exp(-decay * t);
            a[i] = prev * env * vol;
        }
        return a;
    }

    float[] Silence(float dur)
    {
        return new float[Mathf.Max(1, (int)(SR * dur))];
    }

    static float[] Mix(params float[][] parts)
    {
        int len = 0;
        foreach (var p in parts) len = Mathf.Max(len, p.Length);
        var a = new float[len];
        foreach (var p in parts)
            for (int i = 0; i < p.Length; i++) a[i] += p[i];
        return a;
    }

    static float[] Concat(params float[][] parts)
    {
        int len = 0;
        foreach (var p in parts) len += p.Length;
        var a = new float[len];
        int o = 0;
        foreach (var p in parts) { System.Array.Copy(p, 0, a, o, p.Length); o += p.Length; }
        return a;
    }

    static void Normalize(float[] a, float target)
    {
        float peak = 0f;
        for (int i = 0; i < a.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(a[i]));
        if (peak < 1e-4f) return;
        float g = target / peak;
        if (g >= 1f) return; // only attenuate, don't amplify noise
        for (int i = 0; i < a.Length; i++) a[i] *= g;
    }
}
