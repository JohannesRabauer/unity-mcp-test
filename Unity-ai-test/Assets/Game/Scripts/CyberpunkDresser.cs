using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds a stronger cyberpunk dressing pass on top of the built city: pulsing
/// neon sign strips mounted on building faces and tall holographic billboards at
/// the main intersections. Runtime-built, no assets. Self-wiring: drop on any
/// scene object and it dresses the city a couple of frames after it is built.
/// </summary>
public class CyberpunkDresser : MonoBehaviour
{
    static readonly Color[] Palette =
    {
        new Color(1f, 0.15f, 0.6f), new Color(0.2f, 0.9f, 1f), new Color(0.7f, 0.3f, 1f),
        new Color(1f, 0.7f, 0.2f), new Color(0.3f, 1f, 0.7f), new Color(1f, 0.35f, 0.35f),
    };

    void Start()
    {
        if (GameObject.Find("=CYBERPUNK=") != null) return;
        StartCoroutine(DressWhenReady());
    }

    IEnumerator DressWhenReady()
    {
        // Wait for CityBuilder to have created the Buildings.
        Transform buildings = null;
        for (int i = 0; i < 30 && buildings == null; i++)
        {
            var go = GameObject.Find("Buildings");
            if (go != null) buildings = go.transform;
            yield return null;
        }

        var root = new GameObject("=CYBERPUNK=").transform;
        Random.InitState(91237);

        if (buildings != null) DressBuildings(buildings, root);
        BuildBillboards(root);
    }

    void DressBuildings(Transform buildings, Transform root)
    {
        var signs = new GameObject("Signs").transform;
        signs.SetParent(root, false);

        var bodies = new List<Transform>();
        foreach (Transform child in buildings)
            if (child.name.StartsWith("Bld_")) bodies.Add(child);

        foreach (var b in bodies)
        {
            Vector3 s = b.localScale;
            Vector3 c = b.position;
            float fp = s.x;
            float h = s.y;
            int count = Random.Range(1, 3);
            for (int i = 0; i < count; i++)
            {
                Color col = Palette[Random.Range(0, Palette.Length)];
                int face = Random.Range(0, 4);
                float signH = Random.Range(1.2f, 2.6f);
                float signW = fp * Random.Range(0.45f, 0.8f);
                float y = Random.Range(2.5f, Mathf.Max(3f, h - 1.5f));
                float off = fp * 0.5f + 0.08f;

                var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = "Sign_" + b.name + "_" + i;
                var col0 = sign.GetComponent<Collider>(); if (col0 != null) Destroy(col0);
                sign.transform.SetParent(signs, false);

                Vector3 p; Vector3 sc;
                switch (face)
                {
                    case 0: p = new Vector3(c.x + off, y, c.z); sc = new Vector3(0.2f, signH, signW); break;
                    case 1: p = new Vector3(c.x - off, y, c.z); sc = new Vector3(0.2f, signH, signW); break;
                    case 2: p = new Vector3(c.x, y, c.z + off); sc = new Vector3(signW, signH, 0.2f); break;
                    default: p = new Vector3(c.x, y, c.z - off); sc = new Vector3(signW, signH, 0.2f); break;
                }
                sign.transform.position = p;
                sign.transform.localScale = sc;
                sign.GetComponent<MeshRenderer>().sharedMaterial =
                    NeonFactory.Lit_(col * 0.6f, col, 2f, 0.7f);

                var pulse = sign.AddComponent<NeonPulse>();
                pulse.color = col;
                pulse.baseIntensity = 1.4f;
                pulse.amplitude = 1.4f;
                pulse.speed = Random.Range(1.2f, 3.2f);
            }
        }
    }

    void BuildBillboards(Transform root)
    {
        var holo = new GameObject("Holograms").transform;
        holo.SetParent(root, false);

        // Tall holographic panels at the inner intersection corners.
        Vector3[] spots =
        {
            new Vector3(-22f, 0f, 22f), new Vector3(22f, 0f, 22f),
            new Vector3(-22f, 0f, -22f), new Vector3(22f, 0f, -22f),
            new Vector3(0f, 0f, 44f), new Vector3(0f, 0f, -44f),
        };

        for (int i = 0; i < spots.Length; i++)
        {
            Color col = Palette[i % Palette.Length];
            float ph = Random.Range(7f, 11f);

            // Thin support mast.
            var mast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mast.name = "HoloMast_" + i;
            var mc = mast.GetComponent<Collider>(); if (mc != null) Destroy(mc);
            mast.transform.SetParent(holo, false);
            mast.transform.position = new Vector3(spots[i].x, ph * 0.5f, spots[i].z);
            mast.transform.localScale = new Vector3(0.25f, ph, 0.25f);
            mast.GetComponent<MeshRenderer>().sharedMaterial =
                NeonFactory.Plain(new Color(0.05f, 0.06f, 0.09f));

            // Rotating holographic panel near the top.
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Holo_" + i;
            var pc = panel.GetComponent<Collider>(); if (pc != null) Destroy(pc);
            panel.transform.SetParent(holo, false);
            panel.transform.position = new Vector3(spots[i].x, ph + 1.6f, spots[i].z);
            panel.transform.localScale = new Vector3(3.4f, 3.2f, 0.12f);
            panel.GetComponent<MeshRenderer>().sharedMaterial =
                NeonFactory.Lit_(col * 0.5f, col, 2.4f, 0.8f);

            var pulse = panel.AddComponent<NeonPulse>();
            pulse.color = col;
            pulse.baseIntensity = 1.6f;
            pulse.amplitude = 1.1f;
            pulse.speed = Random.Range(1.5f, 2.8f);

            var spin = panel.AddComponent<HoloSpin>();
            spin.speed = (i % 2 == 0) ? 28f : -34f;
        }
    }
}

/// <summary>Slow Y rotation for holographic billboards.</summary>
public class HoloSpin : MonoBehaviour
{
    public float speed = 30f;
    void Update() => transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
}
