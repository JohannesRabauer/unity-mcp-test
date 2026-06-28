using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight pause menu. Esc (or gamepad Start) toggles a dimmed overlay and
/// freezes time. Offers Resume and Restart. Built with OnGUI so it needs no canvas
/// wiring and works regardless of the current scene setup.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    bool _paused;
    float _prevTimeScale = 1f;

    GUIStyle _title, _hint, _btn;
    bool _stylesReady;

    void Update()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        bool toggle = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                      (gp != null && gp.startButton.wasPressedThisFrame);
        if (toggle) Toggle();
    }

    void Toggle()
    {
        // Don't fight other systems that own a hard pause (e.g. win screen).
        if (!_paused && GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.State.Playing)
            return;

        _paused = !_paused;
        if (_paused)
        {
            _prevTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            SfxManager.Play("ui", 0.6f);
        }
        else
        {
            Time.timeScale = _prevTimeScale;
            SfxManager.Play("ui", 0.6f, 1.2f);
        }
    }

    void Resume() => Toggle();

    void Restart()
    {
        Time.timeScale = 1f;
        SfxManager.Play("ui", 0.7f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void EnsureStyles()
    {
        if (_stylesReady) return;
        _title = new GUIStyle { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _title.normal.textColor = new Color(0.25f, 1f, 0.95f);
        _hint = new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _hint.normal.textColor = new Color(0.85f, 0.85f, 0.95f);
        _btn = new GUIStyle(GUI.skin != null ? GUI.skin.button : new GUIStyle()) { fontSize = 22, fontStyle = FontStyle.Bold };
        _stylesReady = true;
    }

    void OnGUI()
    {
        if (!_paused) return;
        EnsureStyles();

        // Dim.
        GUI.color = new Color(0.02f, 0.02f, 0.06f, 0.78f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cx = Screen.width * 0.5f;
        GUI.Label(new Rect(0, Screen.height * 0.28f, Screen.width, 60), "PAUSED", _title);

        float bw = 240f, bh = 54f;
        float y = Screen.height * 0.45f;
        if (GUI.Button(new Rect(cx - bw * 0.5f, y, bw, bh), "RESUME", _btn)) Resume();
        if (GUI.Button(new Rect(cx - bw * 0.5f, y + bh + 16f, bw, bh), "RESTART", _btn)) Restart();

        GUI.Label(new Rect(0, y + (bh + 16f) * 2f + 8f, Screen.width, 24),
            "Esc: resume      Click a button above", _hint);
    }
}
