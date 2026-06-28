using UnityEngine;

/// <summary>
/// Populates the city at runtime with explosive barrels, health/ammo powerups and
/// a livelier pedestrian crowd. Placement avoids buildings via an overlap check so
/// nothing spawns trapped inside geometry. Drop on one GameObject under =SYSTEMS=.
/// </summary>
public class WorldSpawner : MonoBehaviour
{
    [Header("Extents")]
    public float cityExtent = 44f;

    [Header("Barrels")]
    public int barrelClusters = 9;

    [Header("Powerups")]
    public int healthPacks = 4;
    public int ammoCrates = 4;
    public int rampageOrbs = 2;

    [Header("Crowd")]
    public int extraPedestrians = 16;

    void Start()
    {
        SpawnBarrels();
        SpawnPowerups();
        SpawnCrowd();
    }

    // ------------------------------------------------------------- barrels

    void SpawnBarrels()
    {
        for (int c = 0; c < barrelClusters; c++)
        {
            Vector3 center = RandomClearPoint(1.4f);
            if (center == Vector3.zero) continue;
            int n = Random.Range(2, 5);
            for (int i = 0; i < n; i++)
            {
                Vector2 r = Random.insideUnitCircle * 1.4f;
                Vector3 p = center + new Vector3(r.x, 0f, r.y);
                p.y = 0.05f;
                var go = new GameObject("ExplosiveBarrel");
                go.transform.position = p;
                go.AddComponent<Rigidbody>();
                go.AddComponent<ExplosiveBarrel>();
            }
        }
    }

    // ------------------------------------------------------------- powerups

    void SpawnPowerups()
    {
        for (int i = 0; i < healthPacks; i++)
        {
            var p = RandomClearPoint(1.2f);
            if (p != Vector3.zero) Powerup.Spawn(p, Powerup.Kind.Health);
        }
        for (int i = 0; i < ammoCrates; i++)
        {
            var p = RandomClearPoint(1.2f);
            if (p != Vector3.zero) Powerup.Spawn(p, Powerup.Kind.Ammo);
        }
        for (int i = 0; i < rampageOrbs; i++)
        {
            var p = RandomClearPoint(1.2f);
            if (p != Vector3.zero) Powerup.Spawn(p, Powerup.Kind.Rampage);
        }
    }

    // ------------------------------------------------------------- crowd

    static readonly Color[] CrowdColors =
    {
        new Color(0.30f, 0.85f, 1.00f),
        new Color(1.00f, 0.45f, 0.80f),
        new Color(0.65f, 1.00f, 0.45f),
        new Color(1.00f, 0.75f, 0.30f),
        new Color(0.75f, 0.55f, 1.00f),
        new Color(0.95f, 0.95f, 0.95f),
    };

    void SpawnCrowd()
    {
        for (int i = 0; i < extraPedestrians; i++)
        {
            Vector3 p = RandomClearPoint(1.1f);
            if (p == Vector3.zero) continue;
            SpawnPed(p, CrowdColors[Random.Range(0, CrowdColors.Length)]);
        }
    }

    void SpawnPed(Vector3 pos, Color bodyCol)
    {
        var ped = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ped.name = "Citizen";
        ped.transform.position = pos;
        var rb = ped.AddComponent<Rigidbody>();
        rb.mass = 60f;
        ped.AddComponent<Health>();
        var ai = ped.AddComponent<PedestrianAI>();
        ai.roamRadius = Random.Range(8f, 18f);
        ai.walkSpeed = Random.Range(2.0f, 3.2f);
        ai.fleeSpeed = Random.Range(5.5f, 7.5f);
        ped.AddComponent<DamageFx>();

        ped.GetComponent<Renderer>().sharedMaterial = NeonFactory.Lit_(bodyCol, bodyCol, 0.3f, 0.4f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var hc = head.GetComponent<Collider>();
        if (hc != null) Destroy(hc);
        head.transform.SetParent(ped.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        head.GetComponent<Renderer>().sharedMaterial =
            NeonFactory.Lit_(new Color(0.95f, 0.85f, 0.72f), new Color(0.95f, 0.85f, 0.72f), 0.2f, 0.4f);
    }

    // ------------------------------------------------------------- helpers

    Vector3 RandomClearPoint(float clearRadius)
    {
        for (int i = 0; i < 40; i++)
        {
            var p = new Vector3(Random.Range(-cityExtent, cityExtent), 1f, Random.Range(-cityExtent, cityExtent));
            if (IsClear(p, clearRadius)) return p;
        }
        return Vector3.zero;
    }

    bool IsClear(Vector3 p, float radius)
    {
        var hits = Physics.OverlapSphere(p + Vector3.up * 0.4f, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.gameObject.name == "Ground") continue;
            if (h.GetComponentInParent<PlayerController>() != null) return false;
            return false;
        }
        return true;
    }
}
