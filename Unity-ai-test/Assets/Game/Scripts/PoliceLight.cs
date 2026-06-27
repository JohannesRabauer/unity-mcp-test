using UnityEngine;

/// <summary>
/// Makes an officer unmistakable from a top-down view: a peaked cap plus a
/// squad-car style light bar hovering above the head that flashes red/blue.
/// Drop it on a police root (capsule body at local origin) and it builds the
/// extra geometry on Start. Safe to add twice (it only builds once).
/// </summary>
public class PoliceLight : MonoBehaviour
{
    public float flashHz = 4.5f;

    static readonly int EmId = Shader.PropertyToID("_EmissionColor");
    static readonly Color Red = new Color(1f, 0.06f, 0.14f);
    static readonly Color Blue = new Color(0.12f, 0.32f, 1f);

    Renderer _cellL, _cellR;
    MaterialPropertyBlock _mpb;

    void Start()
    {
        if (transform.Find("CopRig") == null) Build();
        else CacheCells();
        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_cellL == null || _cellR == null) return;
        // Alternate the two cells so it reads as a rotating beacon.
        bool phase = Mathf.Sin(Time.time * flashHz * Mathf.PI * 2f) >= 0f;
        SetCell(_cellL, phase ? Red : Red * 0.06f);
        SetCell(_cellR, phase ? Blue * 0.06f : Blue);
    }

    void SetCell(Renderer r, Color emission)
    {
        r.GetPropertyBlock(_mpb);
        _mpb.SetColor(EmId, emission * 5f);
        r.SetPropertyBlock(_mpb);
    }

    void Build()
    {
        var rig = new GameObject("CopRig").transform;
        rig.SetParent(transform, false);
        rig.localPosition = Vector3.zero;
        rig.localRotation = Quaternion.identity;

        var dark = NeonFactory.Lit_(new Color(0.04f, 0.05f, 0.09f), Color.black, 0f, 0.25f);

        // Peaked cap sitting on the head (head center ~y0.62, radius ~0.31).
        MakePiece(PrimitiveType.Cylinder, rig, new Vector3(0f, 0.96f, 0.05f), new Vector3(0.66f, 0.04f, 0.66f), dark, "CapBrim");
        MakePiece(PrimitiveType.Cube, rig, new Vector3(0f, 1.05f, 0f), new Vector3(0.5f, 0.2f, 0.5f), dark, "CapCrown");

        // Light-bar housing + two emissive cells floating just above the head.
        MakePiece(PrimitiveType.Cube, rig, new Vector3(0f, 1.5f, 0f), new Vector3(0.98f, 0.18f, 0.36f), dark, "BarHousing");
        var lMat = NeonFactory.Lit_(Red, Red, 5f, 0.5f);
        var rMat = NeonFactory.Lit_(Blue, Blue, 5f, 0.5f);
        var l = MakePiece(PrimitiveType.Cube, rig, new Vector3(-0.25f, 1.53f, 0f), new Vector3(0.44f, 0.2f, 0.4f), lMat, "Cell_L");
        var r = MakePiece(PrimitiveType.Cube, rig, new Vector3(0.25f, 1.53f, 0f), new Vector3(0.44f, 0.2f, 0.4f), rMat, "Cell_R");
        _cellL = l.GetComponent<Renderer>();
        _cellR = r.GetComponent<Renderer>();
    }

    void CacheCells()
    {
        var l = transform.Find("CopRig/Cell_L");
        var r = transform.Find("CopRig/Cell_R");
        if (l != null) _cellL = l.GetComponent<Renderer>();
        if (r != null) _cellR = r.GetComponent<Renderer>();
    }

    GameObject MakePiece(PrimitiveType prim, Transform parent, Vector3 lpos, Vector3 lscale, Material mat, string nm)
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
}
