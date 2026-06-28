using UnityEngine;

/// <summary>
/// Animated day/night cycle. Rotates the scene's Directional Light through a full
/// arc and lerps its colour/intensity plus the ambient + fog tint so the neon city
/// glows at night and brightens by day. Purely cosmetic and self-wiring: it finds
/// the directional light on Start. Drop on a manager GameObject.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Tooltip("Seconds for one full day-night loop.")]
    public float dayLength = 120f;

    [Tooltip("Phase 0..1 to start at (0.25 = morning, 0.5 = noon, 0.75 = dusk).")]
    public float startPhase = 0.32f;

    Light _sun;
    float _phase;

    // Key colours through the cycle.
    static readonly Color NightSun = new Color(0.18f, 0.22f, 0.45f);
    static readonly Color DaySun = new Color(1f, 0.96f, 0.86f);
    static readonly Color DuskSun = new Color(1f, 0.55f, 0.35f);
    static readonly Color NightAmbient = new Color(0.05f, 0.06f, 0.12f);
    static readonly Color DayAmbient = new Color(0.5f, 0.52f, 0.58f);

    void Start()
    {
        _phase = Mathf.Repeat(startPhase, 1f);
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { _sun = l; break; }
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        Apply();
    }

    void Update()
    {
        if (dayLength <= 0.01f) return;
        _phase = Mathf.Repeat(_phase + Time.deltaTime / dayLength, 1f);
        Apply();
    }

    void Apply()
    {
        // daylight: 1 at noon (phase .5), 0 at midnight (phase 0/1).
        float daylight = Mathf.Clamp01(Mathf.Sin(_phase * Mathf.PI * 2f - Mathf.PI * 0.5f) * 0.5f + 0.5f);
        // dusk/dawn weighting peaks near the horizon transitions.
        float horizon = 1f - Mathf.Abs(daylight - 0.35f) / 0.35f;
        horizon = Mathf.Clamp01(horizon);

        Color sunCol = Color.Lerp(NightSun, DaySun, daylight);
        sunCol = Color.Lerp(sunCol, DuskSun, horizon * 0.6f);

        if (_sun != null)
        {
            _sun.color = sunCol;
            _sun.intensity = Mathf.Lerp(0.15f, 1.15f, daylight);
            // Sweep the sun across the sky.
            float elevation = Mathf.Lerp(-10f, 75f, daylight);
            _sun.transform.rotation = Quaternion.Euler(elevation, _phase * 360f, 0f);
        }

        RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, daylight);
        RenderSettings.fogColor = Color.Lerp(new Color(0.04f, 0.05f, 0.10f), new Color(0.55f, 0.6f, 0.68f), daylight);
        RenderSettings.fogDensity = Mathf.Lerp(0.012f, 0.004f, daylight);
    }
}
