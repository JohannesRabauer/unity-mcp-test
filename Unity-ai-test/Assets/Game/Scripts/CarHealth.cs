using System.Collections;
using UnityEngine;

/// <summary>
/// Makes a car destroyable. The shared <see cref="Health"/> takes hitscan damage
/// (weapons already apply it via GetComponentInParent&lt;Health&gt;), and when it
/// reaches zero the car erupts: muzzle-bright pops, an explosion sound, camera
/// shake and a radial blast that hurts nearby actors (and can chain to other
/// cars). The wreck lingers briefly, then the car respawns so traffic stays alive.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CarController))]
public class CarHealth : MonoBehaviour
{
    public float maxHealth = 120f;
    public float blastRadius = 6.5f;
    public float blastDamage = 70f;
    public float respawnDelay = 9f;

    Health _health;
    CarController _car;
    Rigidbody _rb;
    Renderer _bodyRenderer;
    Material _liveMat;
    Vector3 _spawnPos;
    Quaternion _spawnRot;
    bool _dead;

    void Awake()
    {
        _health = GetComponent<Health>();
        _car = GetComponent<CarController>();
        _rb = GetComponent<Rigidbody>();
        _bodyRenderer = GetComponent<Renderer>();
        if (_bodyRenderer == null) _bodyRenderer = GetComponentInChildren<Renderer>();
        if (_bodyRenderer != null) _liveMat = _bodyRenderer.sharedMaterial;
        _spawnPos = transform.position;
        _spawnRot = transform.rotation;

        _health.maxHealth = maxHealth;
        _health.ResetHealth();
        _health.OnDied += _ => Explode();
        _health.OnDamaged += OnDamaged;
    }

    void OnDamaged(Health h, float amount)
    {
        // Small spark + smoke flash when the car is shot.
        FxPop.Spawn(transform.position + Vector3.up * 0.8f, new Color(1f, 0.6f, 0.2f), 0.8f, 0.12f, 4f);
    }

    void Explode()
    {
        if (_dead) return;
        _dead = true;

        // Eject anyone driving before the blast.
        _car.EjectDriver();

        Vector3 c = transform.position + Vector3.up * 0.6f;

        // Visual + audio kaboom.
        FxPop.Spawn(c, new Color(1f, 0.85f, 0.3f), 7f, 0.45f, 8f);
        FxPop.Spawn(c, new Color(1f, 0.4f, 0.1f), 5f, 0.6f, 6f);
        for (int i = 0; i < 6; i++)
        {
            Vector3 o = c + new Vector3(Random.Range(-1.4f, 1.4f), Random.Range(0f, 1.6f), Random.Range(-1.4f, 1.4f));
            FxPop.Spawn(o, new Color(1f, Random.Range(0.4f, 0.8f), 0.15f), Random.Range(1.5f, 3f), Random.Range(0.3f, 0.55f), 6f);
        }
        SfxManager.Play("explosion", 1f, Random.Range(0.85f, 1.05f));
        CameraShake.Shake(0.45f);

        // Radial damage (can chain to nearby cars / hurt actors).
        var hits = Physics.OverlapSphere(transform.position, blastRadius, ~0, QueryTriggerInteraction.Ignore);
        var alreadyHit = new System.Collections.Generic.HashSet<Health>();
        foreach (var col in hits)
        {
            var h = col.GetComponentInParent<Health>();
            if (h == null || h == _health || alreadyHit.Contains(h)) continue;
            alreadyHit.Add(h);
            float dist = Vector3.Distance(transform.position, h.transform.position);
            float falloff = Mathf.Clamp01(1f - dist / blastRadius);
            if (falloff <= 0f) continue;
            h.TakeDamage(blastDamage * falloff, gameObject);
        }

        StartCoroutine(WreckThenRespawn());
    }

    IEnumerator WreckThenRespawn()
    {
        // Charred wreck: dark material, frozen, not enterable or AI-driven.
        if (_bodyRenderer != null)
            _bodyRenderer.sharedMaterial = NeonFactory.Plain(new Color(0.06f, 0.06f, 0.07f), 0.1f);
        var traffic = GetComponent<TrafficAI>();
        if (traffic != null) traffic.enabled = false;
        _car.enabled = false;                 // leaves CarController.All -> not enterable/driveable
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;

        yield return new WaitForSeconds(respawnDelay);

        // Respawn a fresh car at the original spot.
        transform.SetPositionAndRotation(_spawnPos, _spawnRot);
        _rb.isKinematic = false;
        if (_bodyRenderer != null && _liveMat != null) _bodyRenderer.sharedMaterial = _liveMat;
        _health.ResetHealth();
        _car.enabled = true;
        if (traffic != null) traffic.enabled = true;
        _dead = false;
    }
}
