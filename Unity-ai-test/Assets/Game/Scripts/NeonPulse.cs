using UnityEngine;

/// <summary>
/// Gently pulses a renderer's emission so neon signage breathes (drives Bloom).
/// </summary>
public class NeonPulse : MonoBehaviour
{
    public Color color = new Color(1f, 0.2f, 0.7f);
    public float baseIntensity = 1.6f;
    public float amplitude = 1.2f;
    public float speed = 2f;

    MeshRenderer _mr;
    MaterialPropertyBlock _mpb;
    float _phase;

    void Awake()
    {
        _mr = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
        _phase = Random.value * 6.283f;
    }

    void Update()
    {
        if (_mr == null) return;
        float k = baseIntensity + amplitude * (0.5f + 0.5f * Mathf.Sin(_phase + Time.time * speed));
        _mr.GetPropertyBlock(_mpb);
        _mpb.SetColor("_EmissionColor", color * k);
        _mr.SetPropertyBlock(_mpb);
    }
}
