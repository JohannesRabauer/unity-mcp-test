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
        _runner.transform.position = runnerSpawn;
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
        string objective = _phase switch
        {
            Phase.Offered => "CONTRACT: Meet the contact (gold beacon)",
            Phase.Active => "CONTRACT: Eliminate the marked runner (red beacon)",
            Phase.TargetDown => "CONTRACT: Return to the contact for payment (gold beacon)",
            _ => null
        };
        if (string.IsNullOrEmpty(objective)) return;

        EnsureStyles();
        var r = new Rect(0, Screen.height * 0.13f, Screen.width, 28);
        var rs = new Rect(1, Screen.height * 0.13f + 1, Screen.width, 28);
        GUI.Label(rs, objective, _objShadow);
        GUI.Label(r, objective, _objStyle);
    }
}
