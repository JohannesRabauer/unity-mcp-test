using UnityEngine;

/// <summary>
/// Rewards aggressive, stylish play. Chaining kills (by gun or by the player's car)
/// within a short window builds a combo multiplier that pays out bonus cash and shows
/// a punchy on-screen readout. Let the timer lapse and the streak banks and resets.
/// Listens to the global <see cref="Health.OnAnyDeath"/> feed, so it needs no wiring
/// into other systems.
/// </summary>
public class StyleMeter : MonoBehaviour
{
    [Header("Tuning")]
    public float comboWindow = 4f;
    public int baseBonus = 25;

    int _combo;
    float _timer;
    int _bankedThisStreak;
    string _lastAction = "";
    float _flashTimer;

    GUIStyle _comboStyle, _comboShadow, _subStyle;
    bool _stylesReady;

    void OnEnable() { Health.OnAnyDeath += OnAnyDeath; }
    void OnDisable() { Health.OnAnyDeath -= OnAnyDeath; }

    void OnAnyDeath(Health victim, GameObject instigator)
    {
        if (!CreditedToPlayer(instigator)) return;
        if (victim != null && PlayerController.Instance != null &&
            victim.gameObject == PlayerController.Instance.gameObject) return;

        _combo++;
        _timer = comboWindow;
        _flashTimer = 0.5f;

        int mult = Mathf.Max(1, _combo);
        int bonus = baseBonus * mult;
        _bankedThisStreak += bonus;
        GameManager.Instance?.AddCash(bonus);

        _lastAction = LabelFor(victim);
        SfxManager.Play("switch", 0.4f, 1f + Mathf.Min(0.6f, _combo * 0.06f));
    }

    bool CreditedToPlayer(GameObject instigator)
    {
        if (instigator == null) return false;
        var player = PlayerController.Instance;
        if (player == null) return false;
        if (instigator == player.gameObject) return true;
        // Kills by the car the player is driving also count.
        var car = instigator.GetComponentInParent<CarController>();
        return car != null && car.Driver == player;
    }

    string LabelFor(Health victim)
    {
        if (victim == null) return "TAKEDOWN";
        if (victim.GetComponentInParent<PoliceAI>() != null) return "COP DOWN";
        if (victim.GetComponentInParent<CarController>() != null) return "WRECKED";
        if (victim.GetComponentInParent<ExplosiveBarrel>() != null) return "BOOM";
        return "TAKEDOWN";
    }

    void Update()
    {
        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f) BankStreak();
        }
        if (_flashTimer > 0f) _flashTimer -= Time.deltaTime;
    }

    void BankStreak()
    {
        if (_combo >= 3)
            GameManager.Instance?.ShowBanner($"STYLE STREAK x{_combo}  +${_bankedThisStreak}", 1.6f);
        _combo = 0;
        _bankedThisStreak = 0;
    }

    void EnsureStyles()
    {
        if (_stylesReady) return;
        _comboStyle = new GUIStyle { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _comboStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        _comboShadow = new GUIStyle(_comboStyle);
        _comboShadow.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        _subStyle = new GUIStyle { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _subStyle.normal.textColor = new Color(1f, 0.4f, 0.8f);
        _stylesReady = true;
    }

    void OnGUI()
    {
        if (_combo < 2) return;
        EnsureStyles();

        float pop = 1f + (_flashTimer > 0f ? _flashTimer * 0.4f : 0f);
        int sz = Mathf.RoundToInt(40 * pop);
        _comboStyle.fontSize = sz;
        _comboShadow.fontSize = sz;

        float y = Screen.height * 0.22f;
        var r = new Rect(0, y, Screen.width, 50);
        var rs = new Rect(2, y + 2, Screen.width, 50);
        string txt = $"COMBO x{_combo}";
        GUI.Label(rs, txt, _comboShadow);
        GUI.Label(r, txt, _comboStyle);

        if (!string.IsNullOrEmpty(_lastAction))
            GUI.Label(new Rect(0, y + sz + 2, Screen.width, 24), _lastAction, _subStyle);

        // Thin combo-timer bar under the readout.
        float t = comboWindow > 0f ? Mathf.Clamp01(_timer / comboWindow) : 0f;
        float bw = 180f;
        var bar = new Rect(Screen.width * 0.5f - bw * 0.5f, y + sz + 28, bw, 5f);
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.85f, 0.2f);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * t, bar.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
