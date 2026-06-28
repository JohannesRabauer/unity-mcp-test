using UnityEngine;

/// <summary>
/// Generates and loops a synthwave background track at runtime - a four-chord neon
/// progression with a pulsing bass and an arpeggio, plus a soft kick pulse. No audio
/// assets required. Kept low in the mix so SFX stay clear. Self-contained singleton.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public float volume = 0.16f;

    const int SR = 44100;
    AudioSource _src;

    // Am - F - C - G, two seconds per chord (BPM ~120, eighth-note grid).
    static readonly float[] Roots = { 110.00f, 87.31f, 130.81f, 98.00f };
    // Triad intervals (semitone ratios) for a minor/major-ish arp.
    static readonly float[][] Triads =
    {
        new[] { 1f, 1.2f, 1.5f },   // minor-ish
        new[] { 1f, 1.26f, 1.5f },  // major-ish
        new[] { 1f, 1.26f, 1.5f },
        new[] { 1f, 1.26f, 1.5f },
    };

    void Start()
    {
        _src = gameObject.AddComponent<AudioSource>();
        _src.clip = BuildTrack();
        _src.loop = true;
        _src.playOnAwake = false;
        _src.spatialBlend = 0f;
        _src.volume = volume;
        _src.Play();
    }

    AudioClip BuildTrack()
    {
        float chordDur = 2f;
        int chords = Roots.Length;
        int n = (int)(SR * chordDur * chords);
        var buf = new float[n];

        float eighth = 0.25f; // seconds per arp/bass step
        for (int c = 0; c < chords; c++)
        {
            float root = Roots[c];
            float[] triad = Triads[c];
            int baseI = (int)(c * chordDur * SR);
            int steps = (int)(chordDur / eighth);
            for (int s = 0; s < steps; s++)
            {
                int start = baseI + (int)(s * eighth * SR);

                // Bass: root one octave down, pulsing each eighth.
                AddTone(buf, start, eighth, root * 0.5f, 0.32f, Wave.Saw, 9f);

                // Arp: cycle through triad notes one octave up.
                float arpFreq = root * 2f * triad[s % triad.Length];
                AddTone(buf, start, eighth * 0.9f, arpFreq, 0.16f, Wave.Square, 7f);

                // Soft kick on the beat (every other eighth).
                if (s % 2 == 0)
                    AddKick(buf, start, root);
            }
        }

        Normalize(buf, 0.85f);
        var clip = AudioClip.Create("NeonMusic", n, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }

    enum Wave { Sine, Square, Saw }

    void AddTone(float[] buf, int start, float dur, float freq, float vol, Wave wave, float decay)
    {
        int n = (int)(SR * dur);
        for (int i = 0; i < n; i++)
        {
            int idx = start + i;
            if (idx < 0 || idx >= buf.Length) break;
            float t = (float)i / SR;
            float ph = freq * t;
            float s;
            switch (wave)
            {
                case Wave.Square: s = Mathf.Sin(ph * 2f * Mathf.PI) >= 0f ? 1f : -1f; break;
                case Wave.Saw: s = 2f * (ph - Mathf.Floor(ph)) - 1f; break;
                default: s = Mathf.Sin(ph * 2f * Mathf.PI); break;
            }
            float env = Mathf.Exp(-decay * t);
            buf[idx] += s * env * vol;
        }
    }

    void AddKick(float[] buf, int start, float baseFreq)
    {
        float dur = 0.16f;
        int n = (int)(SR * dur);
        for (int i = 0; i < n; i++)
        {
            int idx = start + i;
            if (idx < 0 || idx >= buf.Length) break;
            float t = (float)i / SR;
            float f = Mathf.Lerp(120f, 45f, Mathf.Clamp01(t / dur));
            float env = Mathf.Exp(-18f * t);
            buf[idx] += Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.5f;
        }
    }

    static void Normalize(float[] a, float target)
    {
        float peak = 0f;
        for (int i = 0; i < a.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(a[i]));
        if (peak < 1e-4f) return;
        float g = target / peak;
        for (int i = 0; i < a.Length; i++) a[i] *= g;
    }
}
