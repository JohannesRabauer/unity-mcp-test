using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a readable, full-size top-down city on a regular street grid:
/// asphalt streets every 22 units (x/z = -44,-22,0,22,44) with sidewalks, dashed
/// lane lines and zebra crossings, framing 16 city blocks. The four downtown
/// blocks form an open civic core (central park, parking lot, hospital plaza and
/// police plaza) and the surrounding blocks hold neon high-rises and landmark
/// towers, with an extraction helipad in the far corner.
///
/// Everything is parented under "=CITY=" and rebuilt from scratch each call (it
/// also removes the old hand-placed buildings), so it is safe to re-run.
/// </summary>
public static class CityBuilder
{
    // Street centre lines on both axes; blocks sit centred between them.
    static readonly float[] Lines = { -44f, -22f, 0f, 22f, 44f };
    static readonly float[] Blocks = { -33f, -11f, 11f, 33f };
    const float StreetW = 8f;     // drivable width
    const float Pad = 14f;        // sidewalk block footprint (pitch 22 - street 8)
    const float Span = 96f;       // street length (covers -48..48)

    static Material _asphalt, _sidewalk, _grass, _lot, _bay, _dash, _trunk, _leaf, _bench, _path, _curbHi;

    public static void Build()
    {
        RemoveOld();
        InitMaterials();

        var root = new GameObject("=CITY=").transform;

        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            var mr = ground.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = NeonFactory.Plain(new Color(0.05f, 0.05f, 0.065f), 0.1f);
        }
        var plaza = GameObject.Find("Plaza");
        if (plaza != null) plaza.SetActive(false);

        BuildSidewalkPads(root);
        BuildStreets(root);
        BuildMarkings(root);
        BuildBlocks(root);
    }

    /// <summary>Drop the previous build, the old pulsing grid lines and the old hand-placed buildings.</summary>
    static void RemoveOld()
    {
        var old = GameObject.Find("=CITY=");
        if (old != null) Kill(old);
        foreach (var pl in Object.FindObjectsByType<PulseLine>(FindObjectsSortMode.None))
            if (pl != null) Kill(pl.gameObject);

        var toKill = new List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null) continue;
            string n = t.name;
            if (n.StartsWith("Bld") || n.StartsWith("Tower") || n == "HospitalPad" ||
                n == "CityDetail" || n == "Bld2_Fill" || n == "Bld3_Outer")
                toKill.Add(t.gameObject);
        }
        foreach (var g in toKill) if (g != null) Kill(g);
    }

    // ---------------------------------------------------------------- streets

    static void BuildStreets(Transform root)
    {
        var roads = new GameObject("Streets").transform;
        roads.SetParent(root, false);
        foreach (var x in Lines) Slab(roads, "St_V", x, 0f, StreetW, Span, _asphalt, 0.04f);
        foreach (var z in Lines) Slab(roads, "St_H", 0f, z, Span, StreetW, _asphalt, 0.04f);
    }

    static void BuildSidewalkPads(Transform root)
    {
        var walks = new GameObject("Sidewalks").transform;
        walks.SetParent(root, false);
        foreach (var bx in Blocks)
            foreach (var bz in Blocks)
                Slab(walks, "Walk", bx, bz, Pad, Pad, _sidewalk, 0.06f);
    }

    static void BuildMarkings(Transform root)
    {
        var marks = new GameObject("LaneMarks").transform;
        marks.SetParent(root, false);

        // Dashed centre lines along every street, skipping the intersection boxes.
        foreach (var x in Lines)
            for (int z = -46; z <= 46; z += 4)
            {
                if (NearLine(z)) continue;
                Slab(marks, "Dash", x, z, 0.25f, 1.7f, _dash, 0.07f);
            }
        foreach (var z in Lines)
            for (int x = -46; x <= 46; x += 4)
            {
                if (NearLine(x)) continue;
                Slab(marks, "Dash", x, z, 1.7f, 0.25f, _dash, 0.07f);
            }

        // Zebra crossings on the four arms of the walkable downtown intersections.
        float[] core = { -22f, 0f, 22f };
        foreach (var cx in core)
            foreach (var cz in core)
            {
                Crosswalk(marks, cx, cz + 5.2f, true);
                Crosswalk(marks, cx, cz - 5.2f, true);
                Crosswalk(marks, cx + 5.2f, cz, false);
                Crosswalk(marks, cx - 5.2f, cz, false);
            }
    }

    static bool NearLine(int v)
    {
        foreach (var l in Lines) if (Mathf.Abs(v - l) < 6f) return true;
        return false;
    }

    static void Crosswalk(Transform parent, float cx, float cz, bool horizontalRoad)
    {
        for (int i = -2; i <= 2; i++)
        {
            if (horizontalRoad)
                Slab(parent, "Zebra", cx + i * 1.1f, cz, 0.5f, 3.6f, _bay, 0.08f);
            else
                Slab(parent, "Zebra", cx, cz + i * 1.1f, 3.6f, 0.5f, _bay, 0.08f);
        }
    }

    // ----------------------------------------------------------------- blocks

    static void BuildBlocks(Transform root)
    {
        var bld = new GameObject("Buildings").transform;
        bld.SetParent(root, false);

        int hueSeed = 0;
        foreach (var bx in Blocks)
            foreach (var bz in Blocks)
            {
                var c = new Vector3(bx, 0f, bz);
                if (bx == -11f && bz == -11f) { BuildPark(root, c); continue; }
                if (bx == 11f && bz == -11f) { BuildParkingLot(root, c); continue; }
                if (bx == 11f && bz == 11f) { BuildHospital(bld, c); continue; }
                if (bx == -11f && bz == 11f) { BuildPolice(bld, c); continue; }
                if (bx == 33f && bz == 33f) { BuildHelipad(root, c); continue; }

                bool tower = Mathf.Abs(bx) == 33f && Mathf.Abs(bz) == 33f;
                float h = tower ? Random.Range(20f, 28f) : Random.Range(7f, 15f);
                float fp = tower ? 8f : Random.Range(9f, 11.5f);
                Color hue = Hue(hueSeed++);
                Building(bld, "Bld_" + bx + "_" + bz, c, fp, h, hue);
            }
    }

    static void Building(Transform parent, string name, Vector3 c, float footprint, float height, Color hue)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = name;
        body.transform.SetParent(parent, false);
        body.transform.position = new Vector3(c.x, height * 0.5f, c.z);
        body.transform.localScale = new Vector3(footprint, height, footprint);
        body.GetComponent<MeshRenderer>().sharedMaterial =
            NeonFactory.Lit_(hue * 0.55f, hue, 0.35f, 0.4f);

        // Subtle glowing roof rim for the neon skyline read. Parent to the
        // (unscaled) Buildings root - NOT the scaled body - so its world size is
        // exactly what we set here.
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "Rim_" + name;
        var capCol = cap.GetComponent<Collider>(); if (capCol != null) Kill(capCol);
        cap.transform.SetParent(parent, false);
        cap.transform.position = new Vector3(c.x, height + 0.15f, c.z);
        cap.transform.localScale = new Vector3(footprint * 1.04f, 0.5f, footprint * 1.04f);
        cap.GetComponent<MeshRenderer>().sharedMaterial =
            NeonFactory.Lit_(hue * 0.7f, hue, 1.1f, 0.5f);
    }

    static Color Hue(int i)
    {
        // A neon palette cycled deterministically so rebuilds look stable.
        Color[] p =
        {
            new Color(1f, 0.2f, 0.6f), new Color(0.25f, 0.9f, 1f), new Color(0.7f, 0.3f, 1f),
            new Color(1f, 0.75f, 0.2f), new Color(0.3f, 1f, 0.6f), new Color(1f, 0.4f, 0.3f),
            new Color(0.4f, 0.6f, 1f), new Color(0.95f, 0.95f, 0.4f),
        };
        return p[((i % p.Length) + p.Length) % p.Length];
    }

    // --------------------------------------------------------- civic content

    static void BuildHospital(Transform parent, Vector3 c)
    {
        // Open plaza pad already provided by the sidewalk; add a compact hospital at the outer corner.
        Vector3 corner = c + new Vector3(4.4f, 0f, 4.4f);
        Building(parent, "Hospital", corner, 6f, 9f, new Color(0.9f, 0.95f, 1f));
        // Red cross marking on the plaza so it reads as a hospital.
        Slab(parent, "CrossH", c.x - 3f, c.z - 3f, 3.2f, 0.8f, _bay, 0.08f);
        Slab(parent, "CrossV", c.x - 3f, c.z - 3f, 0.8f, 3.2f, _bay, 0.08f);
    }

    static void BuildPolice(Transform parent, Vector3 c)
    {
        Vector3 corner = c + new Vector3(-4.4f, 0f, 4.4f);
        Building(parent, "PoliceHQ", corner, 6.5f, 8f, new Color(0.3f, 0.45f, 0.9f));
    }

    static void BuildHelipad(Transform root, Vector3 c)
    {
        var pad = new GameObject("Helipad").transform;
        pad.SetParent(root, false);
        Slab(pad, "Pad", c.x, c.z, 11f, 11f, _lot, 0.06f);
        // Ring + H so it reads as the extraction helipad (extraction glow sits at the corner).
        Slab(pad, "H_L", c.x - 1.6f, c.z, 0.5f, 4f, _bay, 0.08f);
        Slab(pad, "H_R", c.x + 1.6f, c.z, 0.5f, 4f, _bay, 0.08f);
        Slab(pad, "H_M", c.x, c.z, 3.2f, 0.5f, _bay, 0.08f);
    }

    // ------------------------------------------------------------------ park

    static void BuildPark(Transform root, Vector3 center)
    {
        var park = new GameObject("Park").transform;
        park.SetParent(root, false);

        Slab(park, "Grass", center.x, center.z, Pad, Pad, _grass, 0.055f);
        Slab(park, "Path", center.x, center.z, Pad, 1.6f, _path, 0.07f);
        Slab(park, "Path2", center.x, center.z, 1.6f, Pad, _path, 0.07f);
        // Small pond.
        var pond = Slab(park, "Pond", center.x + 3f, center.z + 3f, 4f, 4f,
            NeonFactory.Lit_(new Color(0.1f, 0.4f, 0.7f), new Color(0.15f, 0.5f, 0.9f), 0.8f, 0.7f), 0.06f);

        float h = Pad * 0.5f - 1.8f;
        Vector3[] spots =
        {
            center + new Vector3(-h, 0f, h), center + new Vector3(h, 0f, -h),
            center + new Vector3(-h, 0f, -h), center + new Vector3(-3f, 0f, 3f),
        };
        foreach (var s in spots) Tree(park, s);
        Bench(park, center + new Vector3(-2.5f, 0f, -1f));
        Bench(park, center + new Vector3(2.5f, 0f, -4f));
    }

    static void Tree(Transform parent, Vector3 pos)
    {
        var trunk = Piece(PrimitiveType.Cylinder, parent, "Trunk", new Vector3(0.28f, 0.7f, 0.28f), _trunk);
        trunk.transform.position = pos + new Vector3(0f, 0.7f, 0f);
        var leaf = Piece(PrimitiveType.Sphere, parent, "Leaves", new Vector3(1.9f, 2.1f, 1.9f), _leaf);
        leaf.transform.position = pos + new Vector3(0f, 2.0f, 0f);
    }

    static void Bench(Transform parent, Vector3 pos)
    {
        var b = Piece(PrimitiveType.Cube, parent, "Bench", new Vector3(1.8f, 0.25f, 0.5f), _bench);
        b.transform.position = pos + new Vector3(0f, 0.35f, 0f);
    }

    // --------------------------------------------------------------- parking

    static void BuildParkingLot(Transform root, Vector3 center)
    {
        var lot = new GameObject("ParkingLot").transform;
        lot.SetParent(root, false);

        float sx = 12f, sz = 12f;
        Slab(lot, "Pad", center.x, center.z, sx, sz, _lot, 0.05f);
        int bays = 5;
        float step = sx / bays;
        float startX = center.x - sx * 0.5f;
        for (int i = 0; i <= bays; i++)
            Slab(lot, "BayLine", startX + i * step, center.z, 0.16f, sz - 1.2f, _bay, 0.07f);
        Slab(lot, "BayEdge", center.x, center.z + sz * 0.5f - 0.3f, sx, 0.16f, _bay, 0.07f);
        Slab(lot, "BayEdge", center.x, center.z - sz * 0.5f + 0.3f, sx, 0.16f, _bay, 0.07f);
    }

    // ----------------------------------------------------------------- utils

    static GameObject Slab(Transform parent, string name, float cx, float cz, float sx, float sz, Material mat, float y)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Kill(col);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(cx, y, cz);
        go.transform.localScale = new Vector3(sx, Mathf.Max(0.04f, y * 2f), sz);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Piece(PrimitiveType prim, Transform parent, string name, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(prim);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Kill(col);
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static void InitMaterials()
    {
        _asphalt = NeonFactory.Plain(new Color(0.07f, 0.07f, 0.085f), 0.18f);
        _sidewalk = NeonFactory.Plain(new Color(0.22f, 0.22f, 0.27f), 0.12f);
        _grass = NeonFactory.Plain(new Color(0.11f, 0.3f, 0.14f), 0.05f);
        _lot = NeonFactory.Plain(new Color(0.1f, 0.1f, 0.13f), 0.15f);
        _bay = NeonFactory.Lit_(new Color(0.7f, 0.7f, 0.75f), new Color(0.7f, 0.7f, 0.75f), 0.4f, 0.2f);
        _dash = NeonFactory.Lit_(new Color(0.25f, 0.9f, 1f), new Color(0.25f, 0.9f, 1f), 1.4f, 0.3f);
        _trunk = NeonFactory.Plain(new Color(0.28f, 0.18f, 0.1f), 0.1f);
        _leaf = NeonFactory.Lit_(new Color(0.12f, 0.42f, 0.16f), new Color(0.08f, 0.3f, 0.12f), 0.5f, 0.2f);
        _bench = NeonFactory.Plain(new Color(0.18f, 0.16f, 0.2f), 0.2f);
        _path = NeonFactory.Plain(new Color(0.3f, 0.28f, 0.24f), 0.1f);
        _curbHi = NeonFactory.Lit_(new Color(0.8f, 0.3f, 0.9f), new Color(0.8f, 0.3f, 0.9f), 1.2f, 0.4f);
    }

    static void Kill(Object o)
    {
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }
}
