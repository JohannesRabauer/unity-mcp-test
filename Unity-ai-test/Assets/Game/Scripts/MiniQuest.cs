using UnityEngine;

/// <summary>
/// A self-contained "hit contract" mini quest.
///
/// A gold <b>contact</b> NPC (this GameObject) offers the player a job: walk up to
/// the contact to accept, then track down and eliminate the marked <b>runner</b>
/// (a wandering pedestrian flagged with a red beacon). Once the runner is down,
/// return to the contact to collect a cash reward.
///
/// Everything is built procedurally on Start, so dropping this component on an
/// empty GameObject is enough to spawn a working quest.
/// </summary>
public class MiniQuest : MonoBehaviour
{
    [Header("Reward / tuning")]
    public int reward = 750;
    public float interactRadius = 3.5f;

    [Header("Runner")]
    public Vector3 runnerSpawn = new Vector3(-22f, 1f, 10f);
    public float runnerRoam = 24f;

    enum Phase { Offered, Active, TargetDown, Completed }
    Phase _phase = Phase.Offered;

    GameObject _runner;
    Health _runnerHealth;
    GameObject _giverBeacon;
    GameObject _runnerBeacon;

    GUIStyle _objStyle, _objShadow;
    bool _stylesReady;

    GUIStyle _guideStyle, _guideShadow;
    Texture2D _arrowTex;

    public static MiniQuest Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    /// <summary>World position the player should currently head toward, if any.</summary>
    public Vector3? GuideWorldPos()
    {
        switch (_phase)
        {
            case Phase.Offered:
            case Phase.TargetDown:
                return transform.position;
            case Phase.Active:
                return _runner != null ? _runner.transform.position : (Vector3?)null;
            default:
                return null;
        }
    }

    public Color GuideColor => _phase == Phase.Active
        ? new Color(1f, 0.22f, 0.28f)
        : new Color(1f, 0.82f, 0.15f);

    string GuideLabel => _phase == Phase.Active ? "TARGET" : "CONTACT";

    void Start()
    {
        BuildContactVisual();
        _giverBeacon = MakeBeacon(transform, new Color(1f, 0.82f, 0.15f), new Vector3(0f, 0f, 0f));
        SpawnRunner();
        RefreshBeacons();
        GameManager.Instance?.ShowBanner("NEW CONTRACT - meet the contact (gold beacon)", 4f);
    }

    void Update()
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        float dist = Vector3.Distance(player.transform.position, transform.position);

        switch (_phase)
        {
            case Phase.Offered:
                if (dist <= interactRadius) Accept();
                break;
            case Phase.TargetDown:
                if (dist <= interactRadius) Complete();
                break;
        }
    }

    void Accept()
    {
        _phase = Phase.Active;
        RefreshBeacons();
        GameManager.Instance?.ShowBanner("CONTRACT ACCEPTED - eliminate the marked runner", 3.5f);
    }

    void OnRunnerDied()
    {
        if (_phase != Phase.Active) return;
        _phase = Phase.TargetDown;
        if (_runnerBeacon != null) Destroy(_runnerBeacon);
        RefreshBeacons();
        GameManager.Instance?.ShowBanner("TARGET DOWN - return to the contact for payment", 4f);
    }

    void Complete()
    {
        _phase = Phase.Completed;
        RefreshBeacons();
        GameManager.Instance?.AddCash(reward);
        SfxManager.Play("quest", 0.8f);
        GameManager.Instance?.ShowBanner($"CONTRACT COMPLETE  +${reward}", 4f);
    }

    void RefreshBeacons()
    {
        // Contact is highlighted while there is something to do there.
        if (_giverBeacon != null)
            _giverBeacon.SetActive(_phase == Phase.Offered || _phase == Phase.TargetDown);
        if (_runnerBeacon != null)
            _runnerBeacon.SetActive(_phase == Phase.Active);
    }

    // ---------------------------------------------------------------- visuals

    void BuildContactVisual()
    {
        var bodyCol = new Color(0.95f, 0.78f, 0.15f);
        var headCol = new Color(0.95f, 0.85f, 0.72f);
        AddMesh("Body", PrimitiveType.Capsule, transform, new Vector3(0f, 1f, 0f), new Vector3(0.9f, 1f, 0.9f),
            NeonFactory.Lit_(bodyCol, bodyCol, 0.4f, 0.4f));
        AddMesh("Head", PrimitiveType.Sphere, transform, new Vector3(0f, 1.7f, 0f), new Vector3(0.6f, 0.6f, 0.6f),
            NeonFactory.Lit_(headCol, headCol, 0.2f, 0.4f));
    }

    void SpawnRunner()
    {
        _runner = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _runner.name = "QuestRunner";
        _runner.transform.position = FindClearSpawn(runnerSpawn);
        var rb = _runner.AddComponent<Rigidbody>();
        rb.mass = 60f;
        var health = _runner.AddComponent<Health>();
        var ped = _runner.AddComponent<PedestrianAI>();
        ped.roamRadius = runnerRoam;
        ped.walkSpeed = 3.4f;
        ped.fleeSpeed = 7f;
        _runner.AddComponent<DamageFx>();

        // Recolor the body and add a head so the runner reads as a person.
        var bodyCol = new Color(0.85f, 0.15f, 0.2f);
        _runner.GetComponent<Renderer>().sharedMaterial = NeonFactory.Lit_(bodyCol, bodyCol, 0.3f, 0.4f);
        AddMesh("Head", PrimitiveType.Sphere, _runner.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 0.6f, 0.6f),
            NeonFactory.Lit_(new Color(0.95f, 0.85f, 0.72f), new Color(0.95f, 0.85f, 0.72f), 0.2f, 0.4f));

        _runnerBeacon = MakeBeacon(_runner.transform, new Color(1f, 0.15f, 0.2f), new Vector3(0f, 0f, 0f));

        _runnerHealth = health;
        _runnerHealth.OnDied += _ => OnRunnerDied();
    }

    /// <summary>Returns a spawn point that is not embedded inside a building, so
    /// the runner is always reachable and killable.</summary>
    Vector3 FindClearSpawn(Vector3 preferred)
    {
        if (IsClear(preferred)) return preferred;
        for (int i = 0; i < 50; i++)
        {
            var p = new Vector3(Random.Range(-22f, 22f), 1f, Random.Range(-22f, 22f));
            if (IsClear(p)) return p;
        }
        return new Vector3(0f, 1f, 10f); // open avenue fallback
    }

    bool IsClear(Vector3 p)
    {
        var hits = Physics.OverlapSphere(p + Vector3.up * 0.4f, 1.4f, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.gameObject.name == "Ground") continue;
            if (h.GetComponentInParent<PlayerController>() != null) continue;
            return false;
        }
        return true;
    }

    GameObject MakeBeacon(Transform parent, Color color, Vector3 localOffset)
    {
        var root = new GameObject("QuestBeacon");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localOffset;

        var mat = NeonFactory.Lit_(color, color, 3.5f, 0.6f);

        var pillar = MakeRaw("Pillar", PrimitiveType.Cylinder, root.transform,
            new Vector3(0f, 3.4f, 0f), new Vector3(0.12f, 1.6f, 0.12f), mat);

        var orbHolder = new GameObject("Orb");
        orbHolder.transform.SetParent(root.transform, false);
        orbHolder.transform.localPosition = new Vector3(0f, 5.4f, 0f);
        orbHolder.AddComponent<BeaconBob>();
        MakeRaw("OrbMesh", PrimitiveType.Cube, orbHolder.transform, Vector3.zero,
            new Vector3(0.55f, 0.55f, 0.55f), mat).transform.localEulerAngles = new Vector3(45f, 0f, 45f);

        return root;
    }

    GameObject AddMesh(string nm, PrimitiveType prim, Transform parent, Vector3 lpos, Vector3 lscale, Material mat)
    {
        var g = MakeRaw(nm, prim, parent, lpos, lscale, mat);
        return g;
    }

    GameObject MakeRaw(string nm, PrimitiveType prim, Transform parent, Vector3 lpos, Vector3 lscale, Material mat)
    {
        var g = GameObject.CreatePrimitive(prim);
        g.name = nm;
        var col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lpos;
        g.transform.localScale = lscale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    // ---------------------------------------------------------------- HUD

    void EnsureStyles()
    {
        if (_stylesReady) return;
        _objStyle = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        _objStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        _objShadow = new GUIStyle(_objStyle);
        _objShadow.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        _stylesReady = true;
    }

    void OnGUI()
    {
        EnsureStyles();
        DrawGuide();

        string objective = _phase switch
        {
            Phase.Offered => "CONTRACT: Meet the contact (gold beacon)",
            Phase.Active => "CONTRACT: Eliminate the marked runner (red beacon)",
            Phase.TargetDown => "CONTRACT: Return to the contact for payment (gold beacon)",
            _ => null
        };
        if (string.IsNullOrEmpty(objective)) return;

        var r = new Rect(0, Screen.height * 0.13f, Screen.width, 28);
        var rs = new Rect(1, Screen.height * 0.13f + 1, Screen.width, 28);
        GUI.Label(rs, objective, _objShadow);
        GUI.Label(r, objective, _objStyle);
    }

    // -------------------------------------------------------------- guidance

    void DrawGuide()
    {
        var pos = GuideWorldPos();
        var cam = Camera.main;
        var player = PlayerController.Instance;
        if (pos == null || cam == null || player == null) return;

        EnsureGuideAssets();
        Color col = GuideColor;
        Vector3 world = pos.Value + Vector3.up * 2f;
        Vector3 sp = cam.WorldToScreenPoint(world);

        float w = Screen.width, h = Screen.height;
        float margin = 56f;
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        // Convert to GUI space (origin top-left).
        Vector2 tp = new Vector2(sp.x, h - sp.y);
        if (sp.z < 0f) tp = center + (center - tp); // behind camera: mirror

        bool onScreen = sp.z > 0f &&
                        tp.x >= margin && tp.x <= w - margin &&
                        tp.y >= margin && tp.y <= h - margin;

        float dist = Vector3.Distance(player.transform.position, pos.Value);
        string distTxt = Mathf.RoundToInt(dist) + "m";

        if (onScreen)
        {
            // Reticle hovering over the target plus a distance readout.
            float ring = 34f + 6f * Mathf.Sin(Time.time * 6f);
            GUI.color = col;
            GUI.DrawTexture(new Rect(tp.x - ring * 0.5f, tp.y - ring * 0.5f, ring, ring), _ringTex, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            DrawGuideLabel(new Rect(tp.x - 60f, tp.y - ring * 0.5f - 26f, 120f, 22f), GuideLabel + "  " + distTxt, col);
        }
        else
        {
            // Clamp an arrow to the screen edge, pointing toward the target.
            Vector2 dir = (tp - center);
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
            dir.Normalize();
            Vector2 edge = ClampToFrame(center, dir, w, h, margin);
            float angle = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;

            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, edge);
            GUI.color = col;
            float a = 40f;
            GUI.DrawTexture(new Rect(edge.x - a * 0.5f, edge.y - a * 0.5f, a, a), _arrowTex, ScaleMode.StretchToFill, true);
            GUI.matrix = m;
            GUI.color = Color.white;

            // Distance label nudged inward from the edge.
            Vector2 lbl = edge - dir * 34f;
            DrawGuideLabel(new Rect(lbl.x - 60f, lbl.y - 11f, 120f, 22f), GuideLabel + "  " + distTxt, col);
        }
    }

    void DrawGuideLabel(Rect rect, string text, Color col)
    {
        var sh = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
        _guideStyle.normal.textColor = col;
        GUI.Label(sh, text, _guideShadow);
        GUI.Label(rect, text, _guideStyle);
    }

    Vector2 ClampToFrame(Vector2 center, Vector2 dir, float w, float h, float margin)
    {
        float halfW = w * 0.5f - margin;
        float halfH = h * 0.5f - margin;
        float scale = Mathf.Min(
            halfW / Mathf.Max(Mathf.Abs(dir.x), 0.0001f),
            halfH / Mathf.Max(Mathf.Abs(dir.y), 0.0001f));
        return center + dir * scale;
    }

    Texture2D _ringTex;

    void EnsureGuideAssets()
    {
        if (_guideStyle == null)
        {
            _guideStyle = new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _guideStyle.normal.textColor = Color.white;
            _guideShadow = new GUIStyle(_guideStyle);
            _guideShadow.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
        }
        if (_arrowTex == null) _arrowTex = BuildArrowTex();
        if (_ringTex == null) _ringTex = BuildRingTex();
    }

    static Texture2D BuildArrowTex()
    {
        int n = 32;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // Triangle pointing up: width shrinks from bottom to top.
                float fy = y / (float)(n - 1);             // 0 bottom .. 1 top
                float halfWidth = Mathf.Lerp(0.5f, 0.02f, fy);
                float dx = Mathf.Abs((x / (float)(n - 1)) - 0.5f);
                bool inside = fy > 0.15f && dx <= halfWidth;
                t.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
            }
        t.Apply();
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    static Texture2D BuildRingTex()
    {
        int n = 48;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2((n - 1) * 0.5f, (n - 1) * 0.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (n * 0.5f);
                bool ring = d > 0.72f && d < 0.98f;
                t.SetPixel(x, y, ring ? Color.white : new Color(1, 1, 1, 0));
            }
        t.Apply();
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }
}
