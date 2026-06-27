using UnityEngine;

/// <summary>
/// Gently pulses a renderer's emission so the ground lines softly breathe.
/// Uses a MaterialPropertyBlock so it never leaks materials, and a per-instance
/// phase offset so a row of lines ripples like a wave.
/// </summary>
public class PulseLine : MonoBehaviour
{
    public Color baseEmission = new Color(0.2f, 0.9f, 1f);
    public float minIntensity = 0.15f;
    public float maxIntensity = 0.9f;
    public float speed = 1.6f;
    public float phase;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    Renderer _r;
    MaterialPropertyBlock _mpb;

    void Start()
    {
        _r = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        if (phase == 0f) phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (_r == null) return;
        float k = 0.5f * (1f + Mathf.Sin(Time.time * speed + phase));
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, k);
        _r.GetPropertyBlock(_mpb);
        _mpb.SetColor(EmissionId, baseEmission * intensity);
        _r.SetPropertyBlock(_mpb);
    }
}
