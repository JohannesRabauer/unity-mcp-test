using UnityEngine;

/// <summary>
/// Builds a clear, solid neon wall around the playable area so the player and
/// vehicles can no longer wander off the city. Four tall box segments with
/// non-trigger colliders frame the map; emissive accents make the boundary read
/// as a deliberate cyberpunk perimeter rather than an invisible barrier.
/// Self-wiring: drop on any scene object and it builds itself at runtime.
/// </summary>
public class WorldBounds : MonoBehaviour
{
    [Tooltip("Half-extent of the enclosed square (wall centre line distance from origin).")]
    public float extent = 50f;
    public float height = 10f;
    public float thickness = 2f;

    void Start()
    {
        var existing = GameObject.Find("=WORLDBOUNDS=");
        if (existing != null) return; // already built (e.g. domain reload in edit mode)

        var root = new GameObject("=WORLDBOUNDS=").transform;

        float len = extent * 2f + thickness * 2f;
        // North / South run along X; East / West run along Z.
        Wall(root, "Wall_N", new Vector3(0f, height * 0.5f, extent), new Vector3(len, height, thickness));
        Wall(root, "Wall_S", new Vector3(0f, height * 0.5f, -extent), new Vector3(len, height, thickness));
        Wall(root, "Wall_E", new Vector3(extent, height * 0.5f, 0f), new Vector3(thickness, height, len));
        Wall(root, "Wall_W", new Vector3(-extent, height * 0.5f, 0f), new Vector3(thickness, height, len));
    }

    void Wall(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        // Solid body (keeps its BoxCollider so it physically blocks movement).
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = name;
        body.transform.SetParent(parent, false);
        body.transform.position = pos;
        body.transform.localScale = scale;
        body.GetComponent<MeshRenderer>().sharedMaterial =
            NeonFactory.Lit_(new Color(0.04f, 0.05f, 0.08f), new Color(0.1f, 0.6f, 1f), 0.25f, 0.3f);

        bool alongX = scale.x >= scale.z;
        float runLength = alongX ? scale.x : scale.z;

        // Bright neon top rim for an unmistakable edge read.
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rim.name = name + "_Rim";
        var rc = rim.GetComponent<Collider>(); if (rc != null) Destroy(rc);
        rim.transform.SetParent(parent, false);
        rim.transform.position = new Vector3(pos.x, height + 0.2f, pos.z);
        rim.transform.localScale = new Vector3(
            alongX ? runLength : thickness * 1.1f, 0.4f, alongX ? thickness * 1.1f : runLength);
        rim.GetComponent<MeshRenderer>().sharedMaterial =
            NeonFactory.Lit_(new Color(1f, 0.2f, 0.7f), new Color(1f, 0.2f, 0.7f), 2.6f, 0.6f);

        // Evenly spaced vertical neon bars along the inner face.
        int bars = Mathf.Max(4, Mathf.RoundToInt(runLength / 8f));
        var barMat = NeonFactory.Lit_(new Color(0.2f, 0.9f, 1f), new Color(0.2f, 0.9f, 1f), 2.2f, 0.6f);
        for (int i = 0; i <= bars; i++)
        {
            float t = (i / (float)bars - 0.5f) * runLength * 0.96f;
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name + "_Bar" + i;
            var bc = bar.GetComponent<Collider>(); if (bc != null) Destroy(bc);
            bar.transform.SetParent(parent, false);
            float inset = thickness * 0.5f + 0.06f;
            Vector3 bp = alongX
                ? new Vector3(pos.x + t, height * 0.5f, pos.z - Mathf.Sign(pos.z) * inset)
                : new Vector3(pos.x - Mathf.Sign(pos.x) * inset, height * 0.5f, pos.z + t);
            bar.transform.position = bp;
            bar.transform.localScale = alongX
                ? new Vector3(0.25f, height * 0.92f, 0.12f)
                : new Vector3(0.12f, height * 0.92f, 0.25f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = barMat;
        }
    }
}
