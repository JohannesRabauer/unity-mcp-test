using UnityEngine;

/// <summary>
/// Builds a readable top-down city ground layout: asphalt streets with sidewalks
/// and lane markings, a small park (grass + trees) and a marked parking lot,
/// replacing the old "grid of glowing lines" look. Everything is parented under
/// "=CITY=" and rebuilt from scratch each call, so it is safe to re-run.
/// Call <see cref="Build"/> from the editor to bake it into the scene.
/// </summary>
public static class CityBuilder
{
    static Material _asphalt, _sidewalk, _grass, _lot, _bay, _dash, _trunk, _leaf, _bench, _path;

    public static void Build()
    {
        // Remove the previous build and the old pulsing grid lines.
        var old = GameObject.Find("=CITY=");
        if (old != null) Kill(old);
        foreach (var pl in Object.FindObjectsByType<PulseLine>(FindObjectsSortMode.None))
            if (pl != null) Kill(pl.gameObject);

        InitMaterials();

        var root = new GameObject("=CITY=").transform;

        // Plain dark concrete base.
        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            var mr = ground.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = NeonFactory.Plain(new Color(0.05f, 0.05f, 0.065f), 0.1f);
        }
        var plaza = GameObject.Find("Plaza");
        if (plaza != null) plaza.SetActive(false);

        BuildRoads(root);
        BuildSidewalksAndMarkings(root);
        BuildPark(root, new Vector3(-11f, 0f, -11f), 11f);
        BuildParkingLot(root, new Vector3(11f, 0f, -12f), 13f, 9f);
    }

    // ----------------------------------------------------------------- roads

    static void BuildRoads(Transform root)
    {
        var roads = new GameObject("Streets").transform;
        roads.SetParent(root, false);

        // Central loop road through the clear inner core (buildings start at +-14).
        Slab(roads, "Ring_E", 11f, 0f, 5f, 27f, _asphalt, 0.04f);
        Slab(roads, "Ring_W", -11f, 0f, 5f, 27f, _asphalt, 0.04f);
        Slab(roads, "Ring_N", 0f, 11f, 27f, 5f, _asphalt, 0.04f);
        Slab(roads, "Ring_S", 0f, -11f, 27f, 5f, _asphalt, 0.04f);

        // Main avenues crossing the centre.
        Slab(roads, "Ave_NS", 0f, 0f, 6f, 54f, _asphalt, 0.05f);
        Slab(roads, "Ave_EW", 0f, 0f, 54f, 6f, _asphalt, 0.05f);
    }

    static void BuildSidewalksAndMarkings(Transform root)
    {
        var walks = new GameObject("Sidewalks").transform;
        walks.SetParent(root, false);

        // Sidewalks flanking the two avenues.
        Slab(walks, "Walk_NS_L", -3.7f, 0f, 1.3f, 54f, _sidewalk, 0.07f);
        Slab(walks, "Walk_NS_R", 3.7f, 0f, 1.3f, 54f, _sidewalk, 0.07f);
        Slab(walks, "Walk_EW_B", 0f, -3.7f, 54f, 1.3f, _sidewalk, 0.07f);
        Slab(walks, "Walk_EW_T", 0f, 3.7f, 54f, 1.3f, _sidewalk, 0.07f);

        var marks = new GameObject("LaneMarks").transform;
        marks.SetParent(root, false);

        // Dashed centre lines along the avenues (kept subtly neon).
        for (int z = -24; z <= 24; z += 4)
        {
            if (Mathf.Abs(z) < 4) continue; // leave the central intersection clear
            Slab(marks, "Dash", 0f, z, 0.25f, 1.6f, _dash, 0.07f);
        }
        for (int x = -24; x <= 24; x += 4)
        {
            if (Mathf.Abs(x) < 4) continue;
            Slab(marks, "Dash", x, 0f, 1.6f, 0.25f, _dash, 0.07f);
        }

        // Zebra crossings where the avenues meet the loop road.
        Crosswalk(marks, 0f, 11f, true);
        Crosswalk(marks, 0f, -11f, true);
        Crosswalk(marks, 11f, 0f, false);
        Crosswalk(marks, -11f, 0f, false);
    }

    static void Crosswalk(Transform parent, float cx, float cz, bool horizontalRoad)
    {
        // Stripes run across the driving direction.
        for (int i = -2; i <= 2; i++)
        {
            if (horizontalRoad)
                Slab(parent, "Zebra", cx + i * 1.1f, cz, 0.5f, 4.4f, _bay, 0.08f);
            else
                Slab(parent, "Zebra", cx, cz + i * 1.1f, 4.4f, 0.5f, _bay, 0.08f);
        }
    }

    // ------------------------------------------------------------------ park

    static void BuildPark(Transform root, Vector3 center, float size)
    {
        var park = new GameObject("Park").transform;
        park.SetParent(root, false);
        park.position = center;

        Slab(park, "Grass", center.x, center.z, size, size, _grass, 0.05f);
        // A light path across the park.
        Slab(park, "Path", center.x, center.z, size, 1.4f, _path, 0.07f);

        float h = size * 0.5f - 1.5f;
        Vector3[] spots =
        {
            center + new Vector3(-h, 0f, h),
            center + new Vector3(h, 0f, h),
            center + new Vector3(-h, 0f, -h),
            center + new Vector3(h, 0f, -h),
            center + new Vector3(0f, 0f, h * 0.2f),
        };
        foreach (var s in spots) Tree(park, s);

        Bench(park, center + new Vector3(-h * 0.5f, 0f, 0f));
        Bench(park, center + new Vector3(h * 0.5f, 0f, 0f));
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

    static void BuildParkingLot(Transform root, Vector3 center, float sx, float sz)
    {
        var lot = new GameObject("ParkingLot").transform;
        lot.SetParent(root, false);
        lot.position = center;

        Slab(lot, "Pad", center.x, center.z, sx, sz, _lot, 0.05f);

        // Bay divider lines.
        int bays = 5;
        float step = sx / bays;
        float startX = center.x - sx * 0.5f;
        for (int i = 0; i <= bays; i++)
        {
            float x = startX + i * step;
            Slab(lot, "BayLine", x, center.z, 0.16f, sz - 1.2f, _bay, 0.07f);
        }
        // Front/back edge stripes.
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
    }

    static void Kill(Object o)
    {
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }
}
