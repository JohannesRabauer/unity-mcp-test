using UnityEngine;

/// <summary>
/// Bilingual (English / German) start screen that pauses the game and shows the
/// rough story before play begins. Drop on an empty GameObject; it builds its own
/// UI through OnGUI. Press the language buttons to switch, then Start to play.
/// </summary>
public class StartScreen : MonoBehaviour
{
    public enum Lang { EN, DE }
    public Lang language = Lang.EN;

    bool _open = true;
    GUIStyle _title, _story, _btn, _btnActive, _hint, _tag;
    bool _ready;

    void Start()
    {
        // Pause the world until the player dismisses the screen.
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (!_open) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space))
            StartGame();
        if (Input.GetKeyDown(KeyCode.L))
            language = language == Lang.EN ? Lang.DE : Lang.EN;
    }

    void StartGame()
    {
        _open = false;
        Time.timeScale = 1f;
    }

    string Title => "NEON CITY";

    string Story => language == Lang.EN
        ? "The city never sleeps and the neon never dies.\n\n" +
          "You are a small-time hustler trying to make it big on the glowing streets. " +
          "Steal cars, grab the loot, and take contracts from shady contacts who want " +
          "people gone. Every job pays in cold cash - but the more noise you make, the " +
          "harder the cops come down on you.\n\n" +
          "Build your fortune before the night burns out."
        : "Die Stadt schlaeft nie und das Neonlicht erlischt niemals.\n\n" +
          "Du bist ein kleiner Ganove, der es auf den leuchtenden Strassen nach ganz oben " +
          "schaffen will. Klau Autos, schnapp dir die Beute und nimm Auftraege von " +
          "zwielichtigen Kontakten an, die jemanden loswerden wollen. Jeder Job zahlt bar - " +
          "doch je mehr Laerm du machst, desto haerter jagt dich die Polizei.\n\n" +
          "Haeufe dein Vermoegen an, bevor die Nacht verglueht."
        ;

    string StartLabel => language == Lang.EN ? "START" : "SPIEL STARTEN";
    string Hint => language == Lang.EN
        ? "Enter / Space: Start      L: Switch language"
        : "Enter / Leertaste: Start      L: Sprache wechseln";

    void EnsureStyles()
    {
        if (_ready) return;
        _title = new GUIStyle { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _title.normal.textColor = new Color(0.25f, 1f, 0.95f);

        _story = new GUIStyle { fontSize = 20, wordWrap = true, alignment = TextAnchor.UpperLeft };
        _story.normal.textColor = new Color(0.92f, 0.92f, 1f);

        _btn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        _btnActive = new GUIStyle(_btn);
        _btnActive.normal.textColor = new Color(1f, 0.3f, 0.8f);

        _hint = new GUIStyle { fontSize = 15, alignment = TextAnchor.MiddleCenter };
        _hint.normal.textColor = new Color(0.8f, 0.8f, 0.9f);

        _tag = new GUIStyle { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _tag.normal.textColor = new Color(1f, 0.85f, 0.25f);

        _ready = true;
    }

    void OnGUI()
    {
        if (!_open) return;
        EnsureStyles();

        // Dim the whole screen.
        GUI.color = new Color(0.02f, 0.02f, 0.06f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = Mathf.Min(680, Screen.width - 60);
        float h = Mathf.Min(520, Screen.height - 60);
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        // Neon frame.
        GUI.color = new Color(0.25f, 1f, 0.95f, 0.5f);
        GUI.DrawTexture(new Rect(x - 3, y - 3, w + 6, h + 6), Texture2D.whiteTexture);
        GUI.color = new Color(0.05f, 0.04f, 0.10f, 1f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var pad = 28f;
        GUI.Label(new Rect(x, y + 26, w, 64), Title, _title);
        GUI.Label(new Rect(x, y + 92, w, 24),
            language == Lang.EN ? "A neon-soaked top-down crime ride" : "Ein neonbeleuchteter Top-Down-Verbrecher-Trip", _tag);

        // Language buttons.
        float lbw = 70f;
        if (GUI.Button(new Rect(x + w - pad - lbw * 2 - 8, y + 24, lbw, 30), "EN",
            language == Lang.EN ? _btnActive : _btn))
            language = Lang.EN;
        if (GUI.Button(new Rect(x + w - pad - lbw, y + 24, lbw, 30), "DE",
            language == Lang.DE ? _btnActive : _btn))
            language = Lang.DE;

        // Story.
        GUI.Label(new Rect(x + pad, y + 132, w - pad * 2, h - 250), Story, _story);

        // Start button.
        float bw = 240f, bh = 52f;
        if (GUI.Button(new Rect(x + (w - bw) * 0.5f, y + h - 96, bw, bh), StartLabel, _btn))
            StartGame();

        GUI.Label(new Rect(x, y + h - 34, w, 24), Hint, _hint);
    }
}
