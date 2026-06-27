using UnityEngine;

/// <summary>
/// Turns a pickup into an unmistakable collectible: a floating golden gem with a
/// vertical light beam and a pulsing ground ring, so loot/cash never reads as a
/// person. Hides the host object's own renderer and builds its own visuals.
/// </summary>
public class LootBeacon : MonoBehaviour
{
    public Color color = new Color(1f, 0.78f, 0.16f);
    public float pulseSpeed = 3.2f;

    static readonly int EmId = Shader.PropertyToID("_EmissionColor");

    Renderer _gem, _beam, _ring;
    MaterialPropertyBlock _mpb;
    Transform _gemT;

    void Start()
    {
        // Hide the plain host mesh (e.g. the old sphere) but keep its trigger.
        var host = GetComponent<MeshRenderer>();
        if (host != null) host.enabled = false;

        if (transform.Find("LootRig") == null) Build();
        else Cache();
        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        float t = 0.5f * (1f + Mathf.Sin(Time.time * pulseSpeed));
        Set(_gem, color * Mathf.Lerp(2.2f, 4.5f, t));
        Set(_beam, color * Mathf.Lerp(0.8f, 2.2f, t));
        Set(_ring, color * Mathf.Lerp(1.2f, 3.2f, t));
        if (_gemT != null)
            _gemT.localRotation = Quaternion.Euler(45f, Time.time * 80f, 45f);
    }

    void Set(Renderer r, Color emission)
    {
        if (r == null) return;
        r.GetPropertyBlock(_mpb);
        _mpb.SetColor(EmId, emission);
        r.SetPropertyBlock(_mpb);
    }

    void Build()
    {
        var rig = new GameObject("LootRig").transform;
        rig.SetParent(transform, false);
        rig.localPosition = Vector3.zero;

        var mat = NeonFactory.Lit_(new Color(0.25f, 0.18f, 0.02f), color, 3f, 0.7f);

        // Faceted gem (a cube tilted to read as a diamond from above).
        var gem = MakePiece(PrimitiveType.Cube, rig, new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.5f, 0.5f), mat, "Gem");
        _gemT = gem.transform;
        _gemT.localRotation = Quaternion.Euler(45f, 0f, 45f);
        _gem = gem.GetComponent<Renderer>();

        // Thin vertical light beam.
        _beam = MakePiece(PrimitiveType.Cylinder, rig, new Vector3(0f, 1.6f, 0f), new Vector3(0.06f, 1.6f, 0.06f), mat, "Beam")
            .GetComponent<Renderer>();

        // Flat ground ring (disc) just above the floor.
        _ring = MakePiece(PrimitiveType.Cylinder, rig, new Vector3(0f, -0.85f, 0f), new Vector3(1.5f, 0.02f, 1.5f), mat, "Ring")
            .GetComponent<Renderer>();
    }

    void Cache()
    {
        var rig = transform.Find("LootRig");
        var g = rig.Find("Gem"); if (g != null) { _gem = g.GetComponent<Renderer>(); _gemT = g; }
        var b = rig.Find("Beam"); if (b != null) _beam = b.GetComponent<Renderer>();
        var r = rig.Find("Ring"); if (r != null) _ring = r.GetComponent<Renderer>();
    }

    GameObject MakePiece(PrimitiveType prim, Transform parent, Vector3 lpos, Vector3 lscale, Material mat, string nm)
    {
        var go = GameObject.CreatePrimitive(prim);
        go.name = nm;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lpos;
        go.transform.localScale = lscale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
