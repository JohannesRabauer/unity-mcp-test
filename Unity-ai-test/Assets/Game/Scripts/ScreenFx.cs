using UnityEngine;

/// <summary>
/// Full-screen feedback overlay: a red vignette that intensifies and pulses as the
/// player's health drops, plus a quick crimson flash whenever damage is taken.
/// Pure OnGUI, no post-processing dependency. Self-wires to the player's Health.
/// </summary>
public class ScreenFx : MonoBehaviour
{
    public float lowHealthThreshold = 0.45f;

    Texture2D _vignette;
    Health _health;
    float _flash;

    void Update()
    {
        if (_health == null)
        {
            var p = PlayerController.Instance;
            if (p != null && p.Health != null)
            {
                _health = p.Health;
                _health.OnDamaged += (_, __) => _flash = 0.35f;
            }
        }
        if (_flash > 0f) _flash -= Time.unscaledDeltaTime;
    }

    void EnsureTex()
    {
        if (_vignette != null) return;
        int n = 64;
        _vignette = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2((n - 1) * 0.5f, (n - 1) * 0.5f);
        float maxD = c.magnitude;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD;
                // Transparent center, opaque edges.
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.45f) / 0.55f));
                _vignette.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        _vignette.Apply();
        _vignette.wrapMode = TextureWrapMode.Clamp;
    }

    void OnGUI()
    {
        EnsureTex();
        var full = new Rect(0, 0, Screen.width, Screen.height);

        // Low-health vignette.
        if (_health != null && !_health.IsDead)
        {
            float frac = _health.current / Mathf.Max(1f, _health.maxHealth);
            if (frac < lowHealthThreshold)
            {
                float severity = 1f - (frac / lowHealthThreshold);          // 0..1
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
                float a = Mathf.Clamp01(severity * 0.6f * pulse);
                GUI.color = new Color(0.9f, 0.05f, 0.12f, a);
                GUI.DrawTexture(full, _vignette);
            }
        }

        // Damage flash.
        if (_flash > 0f)
        {
            GUI.color = new Color(1f, 0.1f, 0.15f, Mathf.Clamp01(_flash) * 0.45f);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}
