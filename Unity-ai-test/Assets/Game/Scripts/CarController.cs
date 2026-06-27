using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arcade top-down car. Snappy acceleration, speed-scaled steering, and lateral grip
/// so it feels grippy rather than simulation-heavy. Driven by the occupying player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public static readonly List<CarController> All = new List<CarController>();

    [Header("Arcade tuning")]
    public float enginePower = 22f;
    public float maxSpeed = 20f;
    public float maxReverse = 7f;
    public float turnSpeed = 150f;
    public float grip = 6f;          // higher = stickier
    public float idleDrag = 2.5f;

    public PlayerController Driver { get; private set; }
    public bool IsOccupied => Driver != null;

    Rigidbody _rb;
    float _throttle, _steer;
    bool _handbrake;
    bool _hasIntent;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        _rb.linearDamping = 0.4f;
        _rb.angularDamping = 4f;
    }

    public void SetDriver(PlayerController driver)
    {
        Driver = driver;
        if (driver == null)
        {
            _throttle = _steer = 0f;
            _handbrake = false;
        }
    }

    /// <summary>Called by the driver each FixedUpdate.</summary>
    public void Drive(float throttle, float steer, bool handbrake)
    {
        _throttle = Mathf.Clamp(throttle, -1f, 1f);
        _steer = Mathf.Clamp(steer, -1f, 1f);
        _handbrake = handbrake;
        _hasIntent = true;
    }

    void FixedUpdate()
    {
        Vector3 vel = _rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(vel, transform.forward);

        if (IsOccupied && _hasIntent)
        {
            // Accelerate / brake
            float cap = _throttle >= 0f ? maxSpeed : maxReverse;
            if (Mathf.Abs(_throttle) > 0.01f && Mathf.Abs(forwardSpeed) < cap)
                vel += transform.forward * (_throttle * enginePower * Time.fixedDeltaTime);

            // Steering scales with speed and reverses when backing up
            float speedFactor = Mathf.Clamp(forwardSpeed / 4f, -1f, 1f);
            float yaw = _steer * turnSpeed * speedFactor * Time.fixedDeltaTime;
            if (Mathf.Abs(yaw) > 0.0001f)
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));

            if (_handbrake)
                vel = Vector3.MoveTowards(vel, Vector3.zero, 14f * Time.fixedDeltaTime);
        }
        else
        {
            // No driver: coast to a stop.
            vel = Vector3.MoveTowards(vel, new Vector3(0f, vel.y, 0f), idleDrag * Time.fixedDeltaTime);
        }

        // Lateral grip: bleed off sideways velocity.
        Vector3 right = transform.right;
        float lateral = Vector3.Dot(vel, right);
        float gripNow = _handbrake ? grip * 0.25f : grip;
        vel -= right * (lateral * Mathf.Clamp01(gripNow * Time.fixedDeltaTime));

        _rb.linearVelocity = vel;
        _hasIntent = false;
    }

    public Vector3 GetExitPoint()
    {
        return transform.position - transform.right * 2.2f + Vector3.up * 0.5f;
    }
}
