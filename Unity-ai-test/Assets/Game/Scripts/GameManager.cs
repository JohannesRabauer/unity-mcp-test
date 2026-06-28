using UnityEngine;

/// <summary>
/// Game-wide state: pickups, wanted level, win/lose, respawn, and the on-screen HUD.
/// Self-wiring singleton so other systems can reach it without serialized references.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { Playing, Won, Dead }
    public State CurrentState = State.Playing;

    [Header("Pickups")]
    public int pickupsTotal;
    public int pickupsCollected;
    public bool AllCollected => pickupsTotal > 0 && pickupsCollected >= pickupsTotal;

    [Header("Wanted")]
    [Range(0, 5)] public int wanted;
    public float wantedDecaySeconds = 12f;
    float _wantedTimer;

    [Header("Economy")]
    public int cash;
    public int cashPerPickup = 250;
    public int cashPerBust = 50;

    [Header("Run timer")]
    public float runTime;
    public bool timerRunning = true;

    [Header("Respawn")]
    public Vector3 respawnPoint = Vector3.zero;

    string _banner = "";
    float _bannerTimer;

    GUIStyle _big, _mid, _small, _shadow;
    bool _stylesReady;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterPickup() => pickupsTotal++;

    public void CollectPickup()
    {
        pickupsCollected++;
        AddCash(cashPerPickup);
        if (AllCollected)
            ShowBanner("ALL LOOT SECURED - GET TO THE EXTRACTION ZONE", 5f);
        else
            ShowBanner($"LOOT  +${cashPerPickup}", 1.5f);
    }

    public void AddCash(int amount)
    {
        cash = Mathf.Max(0, cash + amount);
    }

    public void AddWanted(int amount = 1)
    {
        wanted = Mathf.Clamp(wanted + amount, 0, 5);
        _wantedTimer = wantedDecaySeconds;
        if (amount > 0) ShowBanner("WANTED LEVEL UP", 1.2f);
    }

    public void OnPlayerDied()
    {
        ShowBanner("BUSTED / WASTED - RESPAWNING", 2.5f);
    }

    public void Win()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Won;
        timerRunning = false;
        int bonus = Mathf.Max(0, 5000 - Mathf.FloorToInt(runTime) * 10);
        AddCash(bonus);
        ShowBanner($"MISSION COMPLETE  +${bonus} TIME BONUS", 999f);
        Time.timeScale = 0f;
    }

    public void ShowBanner(string text, float seconds)
    {
        _banner = text;
        _bannerTimer = seconds;
    }

    void Update()
    {
        if (_bannerTimer > 0f) _bannerTimer -= Time.unscaledDeltaTime;

        if (timerRunning && CurrentState == State.Playing)
            runTime += Time.deltaTime;

        if (wanted > 0)
        {
            _wantedTimer -= Time.deltaTime;
            if (_wantedTimer <= 0f)
            {
                wanted--;
                _wantedTimer = wantedDecaySeconds;
            }
        }
    }

    void EnsureStyles()
    {
        if (_stylesReady) return;
        _big = new GUIStyle { fontSize = 34, fontStyle = FontStyle.Bold };
        _big.normal.textColor = new Color(0.2f, 1f, 0.9f);
        _mid = new GUIStyle { fontSize = 22, fontStyle = FontStyle.Bold };
        _mid.normal.textColor = new Color(1f, 0.85f, 0.2f);
        _small = new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold };
        _small.normal.textColor = Color.white;
        _shadow = new GUIStyle { fontSize = 34, fontStyle = FontStyle.Bold };
        _shadow.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        _stylesReady = true;
    }

    void OnGUI()
    {
        EnsureStyles();

        // Health
        var player = PlayerController.Instance;
        float hp = player != null && player.Health != null ? player.Health.current : 0f;
        float hpMax = player != null && player.Health != null ? player.Health.maxHealth : 100f;
        GUI.Label(new Rect(20, 16, 400, 30), $"HEALTH  {Mathf.CeilToInt(hp)}/{Mathf.CeilToInt(hpMax)}", _mid);
        DrawBar(new Rect(20, 48, 260, 16), hp / Mathf.Max(1f, hpMax), new Color(1f, 0.2f, 0.45f));

        // Loot
        GUI.Label(new Rect(20, 74, 400, 30), $"LOOT  {pickupsCollected}/{pickupsTotal}", _mid);

        // Wanted stars
        string stars = "";
        for (int i = 0; i < 5; i++) stars += i < wanted ? "*" : "-";
        GUI.Label(new Rect(20, 106, 400, 30), $"WANTED  {stars}", _mid);

        // Cash + run time (top-right)
        var cashStyle = new GUIStyle(_mid) { alignment = TextAnchor.UpperRight };
        cashStyle.normal.textColor = new Color(0.4f, 1f, 0.5f);
        GUI.Label(new Rect(Screen.width - 320, 16, 300, 30), $"${cash:N0}", cashStyle);
        int mins = Mathf.FloorToInt(runTime / 60f);
        int secs = Mathf.FloorToInt(runTime % 60f);
        var timeStyle = new GUIStyle(_small) { alignment = TextAnchor.UpperRight };
        GUI.Label(new Rect(Screen.width - 320, 50, 300, 24), $"TIME  {mins:00}:{secs:00}", timeStyle);

        // Weapon + ammo (bottom-right)
        var wp = player != null ? player.weapon : null;
        if (wp != null)
        {
            var wStyle = new GUIStyle(_mid) { alignment = TextAnchor.LowerRight };
            wStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);
            GUI.Label(new Rect(Screen.width - 360, Screen.height - 78, 340, 28), wp.weaponName.ToUpper(), wStyle);

            string ammo = wp.infiniteAmmo ? "INF"
                : wp.IsReloading ? "RELOADING..."
                : $"{wp.Magazine} / {wp.Reserve}";
            var aStyle = new GUIStyle(_small) { alignment = TextAnchor.LowerRight };
            aStyle.normal.textColor = wp.IsReloading ? new Color(1f, 0.6f, 0.3f) : new Color(0.7f, 1f, 0.8f);
            GUI.Label(new Rect(Screen.width - 360, Screen.height - 50, 340, 24), ammo, aStyle);
        }

        // Controls hint
        GUI.Label(new Rect(20, Screen.height - 30, 1100, 24),
            "WASD move   Mouse aim   LMB shoot   R reload   1-4/Q gun   Space jump   Ctrl roll   E car   Shift nitro   H horn   Esc pause", _small);

        // Banner
        if (_bannerTimer > 0f && !string.IsNullOrEmpty(_banner))
        {
            var r = new Rect(0, Screen.height * 0.34f, Screen.width, 60);
            var rs = new Rect(2, Screen.height * 0.34f + 2, Screen.width, 60);
            var center = new GUIStyle(_big) { alignment = TextAnchor.MiddleCenter };
            var centerS = new GUIStyle(_shadow) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(rs, _banner, centerS);
            GUI.Label(r, _banner, center);
        }
    }

    void DrawBar(Rect r, float t, Color fill)
    {
        t = Mathf.Clamp01(t);
        var bg = r;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(bg, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * t, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
