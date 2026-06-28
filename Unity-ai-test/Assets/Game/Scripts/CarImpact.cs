using UnityEngine;

/// <summary>
/// Physical consequences of a car's mass and momentum:
///  - Running over a pedestrian or the on-foot player at speed deals heavy,
///    speed-scaled damage (be careful crossing the street!).
///  - Slamming into a wall, building or another car at speed damages THIS car,
///    so reckless driving (or ramming) can wreck and explode it - complementing
///    the gunfire destruction handled by <see cref="CarHealth"/>.
/// Ground/floor contacts are ignored so normal driving never self-destructs.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarImpact : MonoBehaviour
{
    [Header("Run over (car -> pedestrian / on-foot player)")]
    public float runOverMinSpeed = 4f;
    public float runOverBaseDamage = 24f;
    public float runOverPerSpeed = 8f;

    [Header("Crash (car -> wall / building / car)")]
    public float crashMinSpeed = 8f;
    public float crashSelfPerSpeed = 2.4f;

    Rigidbody _rb;
    Health _selfHealth;
    CarController _car;
    float _nextHitTime;
    float _carSpeed;     // cached pre-collision speed (callback velocity is already resolved)

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _selfHealth = GetComponent<Health>();
        _car = GetComponent<CarController>();
    }

    void FixedUpdate()
    {
        // Snapshot the true travel speed before any collision response zeroes it.
        _carSpeed = _rb.linearVelocity.magnitude;
    }

    void OnCollisionEnter(Collision col) => Resolve(col);
    void OnCollisionStay(Collision col) => Resolve(col);

    void Resolve(Collision col)
    {
        if (Time.time < _nextHitTime) return;

        var otherCar = col.collider.GetComponentInParent<CarController>();
        var otherHealth = col.collider.GetComponentInParent<Health>();

        // --- Run over a pedestrian or the on-foot player ---
        if (otherHealth != null && otherCar == null && otherHealth != _selfHealth)
        {
            // Never run over our own driver (shouldn't collide, but be safe).
            if (_car != null && _car.Driver != null && otherHealth.gameObject == _car.Driver.gameObject) return;

            // Only a car actually moving deals run-over damage (walking into a
            // parked car is harmless).
            if (_carSpeed < runOverMinSpeed) return;

            _nextHitTime = Time.time + 0.25f;
            float dmg = runOverBaseDamage + (_carSpeed - runOverMinSpeed) * runOverPerSpeed;
            otherHealth.TakeDamage(dmg, gameObject);

            Vector3 p = col.GetContact(0).point;
            FxPop.Spawn(p + Vector3.up * 0.4f, new Color(0.9f, 0.05f, 0.12f), 1.4f, 0.22f, 5f);
            SfxManager.Play("thud", 0.9f, Random.Range(0.9f, 1.1f));
            CameraShake.Shake(0.12f);
            return;
        }

        // --- Hard crash into a wall / building / another car damages this car ---
        if (_selfHealth == null) return;

        ContactPoint cp = col.GetContact(0);
        // Ignore floor/ceiling contacts so driving over ground never hurts.
        if (Mathf.Abs(cp.normal.y) > 0.5f) return;

        // Closing speed along the contact normal (relativeVelocity is the pre-resolve
        // impact velocity, so it survives the solver zeroing our rigidbody).
        float into = Mathf.Abs(Vector3.Dot(col.relativeVelocity, cp.normal));
        if (into < crashMinSpeed) return;

        _nextHitTime = Time.time + 0.2f;
        _selfHealth.TakeDamage((into - crashMinSpeed) * crashSelfPerSpeed, gameObject);

        FxPop.Spawn(cp.point + Vector3.up * 0.3f, new Color(1f, 0.7f, 0.2f), 1.3f, 0.18f, 5f);
        SfxManager.Play("crash", 0.8f, Random.Range(0.9f, 1.1f));
        CameraShake.Shake(0.1f);
    }
}
