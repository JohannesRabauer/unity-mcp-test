using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Bilingual (English / German) start screen built at runtime with uGUI + TextMeshPro
/// so German umlauts (ä ö ü ß) render perfectly. Pauses the game until dismissed.
/// Drop on an empty GameObject; it constructs its own Canvas and EventSystem.
/// </summary>
public class StartScreen : MonoBehaviour
{
    public enum Lang { EN, DE }
    public Lang language = Lang.EN;

    bool _open = true;
    Canvas _canvas;
    TMP_FontAsset _font;
    TMP_Text _titleT, _tagT, _storyT, _hintT, _startT;
    Button _enBtn, _deBtn;
    readonly Color _cyan = new Color(0.25f, 1f, 0.95f);
    readonly Color _pink = new Color(1f, 0.3f, 0.8f);

    void Start()
    {
        Time.timeScale = 0f;
        _font = TMP_Settings.defaultFontAsset;
        EnsureEventSystem();
        Build();
        Refresh();
    }

    void Update()
    {
        if (!_open) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
            kb.spaceKey.wasPressedThisFrame)
            StartGame();
        else if (kb.lKey.wasPressedThisFrame)
            SetLang(language == Lang.EN ? Lang.DE : Lang.EN);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>();
    }

    // ---- localized content ----

    string Title => "NEON CITY";

    string Tag => language == Lang.EN
        ? "A neon-soaked top-down crime ride"
        : "Ein neonbeleuchteter Top-Down-Verbrecher-Trip";

    string Story => language == Lang.EN
        ? "The city never sleeps and the neon never dies.\n\n" +
          "You are a small-time hustler trying to make it big on the glowing streets. " +
          "Steal cars, grab the loot, and take contracts from shady contacts who want " +
          "people gone. Every job pays in cold cash - but the more noise you make, the " +
          "harder the cops come down on you.\n\n" +
          "Build your fortune before the night burns out."
        : "Die Stadt schläft nie und das Neonlicht erlischt niemals.\n\n" +
          "Du bist ein kleiner Ganove, der es auf den leuchtenden Straßen nach ganz oben " +
          "schaffen will. Klau Autos, schnapp dir die Beute und nimm Aufträge von " +
          "zwielichtigen Kontakten an, die jemanden loswerden wollen. Jeder Job zahlt bar – " +
          "doch je mehr Lärm du machst, desto härter jagt dich die Polizei.\n\n" +
          "Häufe dein Vermögen an, bevor die Nacht verglüht.";

    string StartLabel => language == Lang.EN ? "START" : "SPIEL STARTEN";

    string Hint => language == Lang.EN
        ? "Enter / Space: Start      L: Switch language"
        : "Enter / Leertaste: Start      L: Sprache wechseln";

    // ---- UI construction ----

    void Build()
    {
        var canvasGO = new GameObject("StartScreenCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // full-screen dim
        var dim = MakeRT("Dim", canvasGO.transform);
        Stretch(dim);
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0.02f, 0.02f, 0.06f, 0.92f);

        // neon frame (slightly larger, behind panel)
        var frame = MakeRT("Frame", canvasGO.transform);
        Center(frame, 728, 588);
        var frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.color = new Color(_cyan.r, _cyan.g, _cyan.b, 0.55f);

        // panel
        var panel = MakeRT("Panel", canvasGO.transform);
        Center(panel, 720, 580);
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.04f, 0.10f, 1f);

        // title
        _titleT = Text("Title", panel, 60, _cyan, TextAlignmentOptions.Center, FontStyles.Bold);
        TopCenter(_titleT.rectTransform, 680, 78, -28);

        // tagline
        _tagT = Text("Tag", panel, 22, new Color(1f, 0.85f, 0.25f), TextAlignmentOptions.Center, FontStyles.Bold);
        TopCenter(_tagT.rectTransform, 680, 30, -112);

        // story
        _storyT = Text("Story", panel, 21, new Color(0.92f, 0.92f, 1f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        _storyT.enableWordWrapping = true;
        TopCenter(_storyT.rectTransform, 632, 320, -160);

        // language buttons (top-right of panel)
        _enBtn = MakeButton("EN", panel, "EN", () => SetLang(Lang.EN));
        TopRight(_enBtn.GetComponent<RectTransform>(), 74, 32, -92, -22);
        _deBtn = MakeButton("DE", panel, "DE", () => SetLang(Lang.DE));
        TopRight(_deBtn.GetComponent<RectTransform>(), 74, 32, -10, -22);

        // start button (bottom-center)
        var startBtn = MakeButton("Start", panel, StartLabel, StartGame);
        BottomCenter(startBtn.GetComponent<RectTransform>(), 260, 56, 86);
        _startT = startBtn.GetComponentInChildren<TMP_Text>();

        // hint
        _hintT = Text("Hint", panel, 16, new Color(0.8f, 0.8f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);
        BottomCenter(_hintT.rectTransform, 680, 26, 36);
    }

    void Refresh()
    {
        _titleT.text = Title;
        _tagT.text = Tag;
        _storyT.text = Story;
        _startT.text = StartLabel;
        _hintT.text = Hint;
        SetBtnColor(_enBtn, language == Lang.EN);
        SetBtnColor(_deBtn, language == Lang.DE);
    }

    void SetLang(Lang l)
    {
        if (language == l) { Refresh(); return; }
        language = l;
        Refresh();
    }

    void SetBtnColor(Button b, bool active)
    {
        var lbl = b.GetComponentInChildren<TMP_Text>();
        if (lbl != null) lbl.color = active ? _pink : Color.white;
        var img = b.GetComponent<Image>();
        if (img != null) img.color = active ? new Color(0.18f, 0.10f, 0.18f, 1f)
                                            : new Color(0.10f, 0.10f, 0.16f, 1f);
    }

    void StartGame()
    {
        if (!_open) return;
        _open = false;
        Time.timeScale = 1f;
        SfxManager.Play("ui", 0.7f);
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }

    // ---- layout helpers ----

    static RectTransform MakeRT(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rt, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
    }

    static void TopCenter(RectTransform rt, float w, float h, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0, y);
    }

    static void TopRight(RectTransform rt, float w, float h, float x, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    static void BottomCenter(RectTransform rt, float w, float h, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0, y);
    }

    TMP_Text Text(string name, Transform parent, float size, Color col,
        TextAlignmentOptions align, FontStyles style)
    {
        var rt = MakeRT(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.richText = true;
        return tmp;
    }

    Button MakeButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRT(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.10f, 0.10f, 0.16f, 1f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var lbl = Text("Label", rt, 20, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        lbl.text = label;
        Stretch(lbl.rectTransform);
        return btn;
    }
}
