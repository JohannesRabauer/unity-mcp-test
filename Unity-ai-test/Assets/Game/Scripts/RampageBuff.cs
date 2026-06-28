using UnityEngine;

/// <summary>
/// Temporary "Rampage" buff: infinite ammo + double weapon damage for a few seconds.
/// Activated by the Rampage powerup. Self-creates a persistent runner that drives the
/// global <see cref="Weapon"/> statics and draws a countdown bar. Picking up another
/// Rampage extends the timer.
/// </summary>
public class RampageBuff : MonoBehaviour
{
    static RampageBuff _instance;
    float _endTime;

    GUIStyle _style;

    public static void Activate(float seconds)
    {
        if (_instance == null)
        {
            var go = new GameObject("RampageBuff");
            _instance = go.AddComponent<RampageBuff>();
        }
        _instance._endTime = Mathf.Max(_instance._endTime, Time.time + seconds);
        Weapon.ForceInfinite = true;
        Weapon.DamageMultiplier = 2f;
        GameManager.Instance?.ShowBanner("RAMPAGE!  x2 DAMAGE / INFINITE AMMO", 1.6f);
        SfxManager.Play("rampage", 0.8f);
    }

    void Update()
    {
        if (Time.time >= _endTime)
        {
            Weapon.ForceInfinite = false;
            Weapon.DamageMultiplier = 1f;
            Destroy(gameObject);
            _instance = null;
        }
    }

    void OnGUI()
    {
        float remain = _endTime - Time.time;
        if (remain <= 0f) return;

        if (_style == null)
        {
            _style = new GUIStyle { fontSize = 18, fontStyle = FontStyle.Bold };
            _style.normal.textColor = new Color(1f, 0.6f, 0.2f);
        }

        float w = 240f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height - 120f;
        GUI.Label(new Rect(x, y - 24f, w, 22f), $"RAMPAGE  {remain:0.0}s", _style);

        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(x, y, w, 12f), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.55f, 0.15f);
        float frac = Mathf.Clamp01(remain / 8f);
        GUI.DrawTexture(new Rect(x, y, w * frac, 12f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void OnDestroy()
    {
        // Safety: never leave the buff statics stuck on.
        if (_instance == this)
        {
            Weapon.ForceInfinite = false;
            Weapon.DamageMultiplier = 1f;
        }
    }
}
