using UnityEngine;

/// <summary>
/// Trauma-based camera shake. Other systems call <see cref="Shake"/> to add trauma;
/// the rig reads <see cref="Offset"/> each frame and adds it on top of its smoothed position.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    public float maxOffset = 1.4f;
    public float decay = 1.8f;
    public float frequency = 26f;

    [Range(0f, 1f)] float _trauma;
    float _seed;

    void Awake()
    {
        Instance = this;
        _seed = Random.value * 100f;
    }

    /// <summary>Add trauma (0..1). Effect scales with trauma squared, so small taps are subtle.</summary>
    public void Add(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

    public static void Shake(float amount)
    {
        if (Instance != null) Instance.Add(amount);
    }

    void Update()
    {
        if (_trauma > 0f) _trauma = Mathf.Max(0f, _trauma - decay * Time.deltaTime);
    }

    public Vector3 Offset
    {
        get
        {
            if (_trauma <= 0f) return Vector3.zero;
            float s = _trauma * _trauma;
            float t = Time.time * frequency;
            float x = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
            float z = (Mathf.PerlinNoise(_seed + 17.3f, t) - 0.5f) * 2f;
            return new Vector3(x, 0f, z) * (maxOffset * s);
        }
    }
}
