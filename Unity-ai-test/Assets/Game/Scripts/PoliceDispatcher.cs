using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns police units near the player scaled to the current wanted level, so heat
/// actually escalates into a pursuit. Units are built procedurally (body, head, cap +
/// light bar via <see cref="PoliceLight"/>, and a <see cref="Weapon"/>). When the
/// wanted level clears, dispatched units stand down and are removed.
/// </summary>
public class PoliceDispatcher : MonoBehaviour
{
    [Header("Dispatch")]
    public int copsPerStar = 1;
    public int maxCops = 7;
    public float spawnInterval = 2.5f;
    public float spawnRing = 34f;

    readonly List<GameObject> _units = new();
    float _timer;

    void Update()
    {
        var gm = GameManager.Instance;
        var player = PlayerController.Instance;
        if (gm == null || player == null) return;

        _units.RemoveAll(u => u == null);

        if (gm.wanted <= 0)
        {
            if (_units.Count > 0) StandDown();
            return;
        }

        int desired = Mathf.Min(maxCops, gm.wanted * copsPerStar);

        _timer -= Time.deltaTime;
        if (_timer <= 0f && _units.Count < desired)
        {
            _timer = spawnInterval;
            SpawnCop(player.transform.position);
        }
    }

    void StandDown()
    {
        foreach (var u in _units)
            if (u != null) Destroy(u);
        _units.Clear();
    }

    void SpawnCop(Vector3 around)
    {
        float a = Random.value * Mathf.PI * 2f;
        Vector3 pos = around + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spawnRing;
        pos.y = 1f;

        var cop = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cop.name = "PoliceUnit";
        cop.transform.position = pos;

        var bodyCol = new Color(0.10f, 0.16f, 0.42f);
        cop.GetComponent<Renderer>().sharedMaterial = NeonFactory.Lit_(bodyCol, bodyCol, 0.5f, 0.4f);

        var rb = cop.AddComponent<Rigidbody>();
        rb.mass = 70f;

        var health = cop.AddComponent<Health>();
        health.maxHealth = 70f;

        cop.AddComponent<DamageFx>();

        // Head.
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var hc = head.GetComponent<Collider>();
        if (hc != null) Destroy(hc);
        head.transform.SetParent(cop.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.62f, 0f);
        head.transform.localScale = new Vector3(0.62f, 0.62f, 0.62f);
        head.GetComponent<Renderer>().sharedMaterial =
            NeonFactory.Lit_(new Color(0.95f, 0.85f, 0.72f), new Color(0.95f, 0.85f, 0.72f), 0.2f, 0.4f);

        // Weapon (mounted as a child; PoliceAI finds it via GetComponentInChildren).
        var gun = new GameObject("Gun");
        gun.transform.SetParent(cop.transform, false);
        gun.transform.localPosition = new Vector3(0f, 0.5f, 0.4f);
        var weapon = gun.AddComponent<Weapon>();
        weapon.weaponName = "Service Pistol";
        weapon.fireSfx = "shot_pistol";
        weapon.damage = 9f;
        weapon.range = 18f;
        weapon.fireRate = 1.6f;
        weapon.infiniteAmmo = true;
        weapon.tracerColor = new Color(1f, 0.3f, 0.3f);

        cop.AddComponent<PoliceLight>();
        var ai = cop.AddComponent<PoliceAI>();
        ai.chaseSpeed = 5.8f;

        _units.Add(cop);
        FxPop.Spawn(pos + Vector3.up * 0.8f, new Color(0.3f, 0.4f, 1f), 1.6f, 0.3f, 4f);
    }
}
