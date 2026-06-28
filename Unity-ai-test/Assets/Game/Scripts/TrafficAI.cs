using UnityEngine;

/// <summary>
/// Lightweight autonomous driver for unoccupied cars. Each car follows its own
/// rectangular loop laid out on the city's clear street grid (set per car via
/// <see cref="loopExtent"/> and <see cref="clockwise"/>). Instead of fighting the
/// arcade CarController (whose steering needs speed to turn, which deadlocks at
/// corners), the AI drives the rigidbody directly: it rotates smoothly toward the
/// next node at any speed and pushes the car forward at a steady cruise. It yields
/// the instant a player occupies the car and resumes from the nearest node when
/// they leave.
/// </summary>
[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(Rigidbody))]
public class TrafficAI : MonoBehaviour
{
    [Tooltip("Half-size of the square street loop this car drives (matches a street ring: 22 = inner, 44 = outer).")]
    public float loopExtent = 22f;
    [Tooltip("Direction around the loop (top-down). True drives +Z up the east side.")]
    public bool clockwise = true;

    public float cruiseSpeed = 10f;   // steady forward speed along the loop
    public float turnRate = 150f;     // deg/sec the car can rotate toward its target
    public float arriveDist = 5f;     // distance at which we advance to the next node
    public float lookAhead = 5.5f;    // forward clearance check for cars/peds

    CarController _car;
    Rigidbody _rb;
    Vector3[] _route;
    int _idx;

    void Awake()
    {
        _car = GetComponent<CarController>();
        _rb = GetComponent<Rigidbody>();
        if (_rb != null) _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        BuildRoute();
    }

    /// <summary>8 waypoints (4 corners + 4 edge midpoints) on a square of half-size loopExtent.</summary>
    void BuildRoute()
    {
        float e = loopExtent;
        var cw = new[]
        {
            new Vector3( e, 0f, -e), new Vector3( e, 0f, 0f), new Vector3( e, 0f,  e), new Vector3( 0f, 0f,  e),
            new Vector3(-e, 0f,  e), new Vector3(-e, 0f, 0f), new Vector3(-e, 0f, -e), new Vector3( 0f, 0f, -e),
        };
        if (clockwise) { _route = cw; return; }
        // Counter-clockwise: reverse the order.
        _route = new Vector3[cw.Length];
        for (int i = 0; i < cw.Length; i++) _route[i] = cw[cw.Length - 1 - i];
    }

    void OnEnable()
    {
        if (_route == null) BuildRoute();
        _idx = NearestNode();
        if (_car != null) _car.aiControlled = true;
        if (_rb != null)
            // Flat city: pin to ground height so nothing can shove a car through the floor.
            _rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    void OnDisable()
    {
        // Hand the car back to the arcade controller (e.g. when wrecked).
        if (_car != null) _car.aiControlled = false;
        if (_rb != null) _rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
    }

    void FixedUpdate()
    {
        // Player (or nothing) is driving via CarController - stay out of the way.
        if (_car == null || _rb == null || _car.IsOccupied || _rb.isKinematic) return;

        // Re-assert the ground-height lock (CarController.Awake resets constraints).
        if ((_rb.constraints & RigidbodyConstraints.FreezePositionY) == 0)
            _rb.constraints |= RigidbodyConstraints.FreezePositionY;

        Vector3 pos = transform.position;
        Vector3 target = _route[_idx]; target.y = pos.y;

        Vector3 to = target - pos; to.y = 0f;
        if (to.magnitude <= arriveDist)
        {
            _idx = (_idx + 1) % _route.Length;
            target = _route[_idx]; target.y = pos.y;
            to = target - pos; to.y = 0f;
        }
        if (to.sqrMagnitude < 0.0001f) return;
        Vector3 dir = to.normalized;

        // Rotate smoothly toward the heading - works at any speed, so no corner stalls.
        Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion next = Quaternion.RotateTowards(_rb.rotation, want, turnRate * Time.fixedDeltaTime);
        _rb.MoveRotation(next);

        // Slow into sharp turns, stop for traffic/peds directly ahead.
        float headingErr = Quaternion.Angle(next, want);
        float speedScale = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(headingErr / 60f));
        float speed = ObstacleAhead() ? 0f : cruiseSpeed * speedScale;

        Vector3 v = transform.forward * speed;
        v.y = 0f;   // Y is frozen; keep velocity planar to avoid solver jitter
        _rb.linearVelocity = v;
    }

    bool ObstacleAhead()
    {
        Vector3 origin = transform.position + transform.forward * 2.4f + Vector3.up * 0.5f;
        if (Physics.SphereCast(origin, 0.7f, transform.forward, out RaycastHit hit, lookAhead, ~0, QueryTriggerInteraction.Ignore))
        {
            // Ignore our own car (all actors share the =ACTORS= root, so match by component).
            if (hit.collider.GetComponentInParent<CarController>() == _car) return false;
            // Only brake for vehicles and people, not the decorative city shell.
            if (hit.collider.GetComponentInParent<CarController>() != null) return true;
            if (hit.collider.GetComponentInParent<PedestrianAI>() != null) return true;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) return true;
            return false;
        }
        return false;
    }

    int NearestNode()
    {
        int best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < _route.Length; i++)
        {
            float d = (_route[i] - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = i; }
        }
        // Aim for the node ahead so the car flows along the loop rather than reversing.
        return (best + 1) % _route.Length;
    }
}
