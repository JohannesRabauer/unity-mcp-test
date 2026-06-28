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

    [Header("Nitro boost")]
    public float boostPower = 1.9f;       // engine-power multiplier while boosting
    public float boostMaxSpeed = 31f;     // raised speed cap while boosting

    [Header("Engine audio")]
    public float engineVolume = 0.35f;

    public PlayerController Driver { get; private set; }
    public bool IsOccupied => Driver != null;

    /// <summary>When true and unoccupied, an external driver (TrafficAI) owns the
    /// rigidbody, so the internal arcade physics step is skipped to avoid fighting it.</summary>
    public bool aiControlled;

    Rigidbody _rb;
    float _throttle, _steer;
    bool _handbrake;
    bool _boost;
    bool _hasIntent;
    float _boostFxTimer;

    AudioSource _engine;
    static AudioClip _engineClip;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
        _rb.linearDamping = 0.4f;
        _rb.angularDamping = 4f;

        SetupEngineAudio();
    }

    void SetupEngineAudio()
    {
        if (_engineClip == null) _engineClip = BuildEngineClip();
        _engine = gameObject.AddComponent<AudioSource>();
        _engine.clip = _engineClip;
        _engine.loop = true;
        _engine.playOnAwake = false;
        _engine.spatialBlend = 0f;
        _engine.volume = 0f;
        _engine.pitch = 0.6f;
    }

    static AudioClip BuildEngineClip()
    {
        const int sr = 44100;
        int n = sr / 2; // 0.5s loop
        var a = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            // Low sawtooth + a fifth, gives a throaty idle hum.
            float saw = 2f * ((t * 60f) % 1f) - 1f;
            float saw2 = 2f * ((t * 90f) % 1f) - 1f;
            a[i] = saw * 0.6f + saw2 * 0.25f;
        }
        var clip = AudioClip.Create("EngineHum", n, 1, sr, false);
        clip.SetData(a, 0);
        return clip;
    }

    void Update()
    {
        UpdateEngineAudio();
    }

    void UpdateEngineAudio()
    {
        if (_engine == null) return;
        bool playerDriving = IsOccupied && Driver == PlayerController.Instance;
        if (!playerDriving)
        {
            if (_engine.isPlaying) _engine.Stop();
            return;
        }
        if (!_engine.isPlaying) _engine.Play();

        float speed = _rb.linearVelocity.magnitude;
        float speedFrac = Mathf.Clamp01(speed / Mathf.Max(1f, maxSpeed));
        float target = (_boost ? 1.3f : 1f) * (0.55f + speedFrac * 0.9f);
        _engine.pitch = Mathf.Lerp(_engine.pitch, target, 8f * Time.deltaTime);
        float volTarget = engineVolume * (0.5f + speedFrac * 0.5f) * (_boost ? 1.25f : 1f);
        _engine.volume = Mathf.Lerp(_engine.volume, volTarget, 6f * Time.deltaTime);
    }

    public void Honk()
    {
        if (!IsOccupied) return;
        SfxManager.Play("horn", 0.7f, Random.Range(0.97f, 1.03f));
    }

    public void SetDriver(PlayerController driver)
    {
        Driver = driver;
        if (driver == null)
        {
            _throttle = _steer = 0f;
            _handbrake = false;
            _boost = false;
        }
    }

    /// <summary>Called by the driver each FixedUpdate.</summary>
    public void Drive(float throttle, float steer, bool handbrake, bool boost = false)
    {
        _throttle = Mathf.Clamp(throttle, -1f, 1f);
        _steer = Mathf.Clamp(steer, -1f, 1f);
        _handbrake = handbrake;
        _boost = boost && throttle > 0.05f;
        _hasIntent = true;
    }

    void FixedUpdate()
    {
        // External AI driver owns the rigidbody while unoccupied.
        if (aiControlled && !IsOccupied) return;

        Vector3 vel = _rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(vel, transform.forward);

        if (_hasIntent)
        {
            // Accelerate / brake (nitro raises power and the forward speed cap).
            float power = enginePower * (_boost ? boostPower : 1f);
            float cap = _throttle >= 0f ? (_boost ? boostMaxSpeed : maxSpeed) : maxReverse;
            if (Mathf.Abs(_throttle) > 0.01f && Mathf.Abs(forwardSpeed) < cap)
                vel += transform.forward * (_throttle * power * Time.fixedDeltaTime);

            // Nitro exhaust flare behind the car.
            if (_boost)
            {
                _boostFxTimer -= Time.fixedDeltaTime;
                if (_boostFxTimer <= 0f)
                {
                    _boostFxTimer = 0.05f;
                    FxPop.Spawn(transform.position - transform.forward * 2.2f + Vector3.up * 0.4f,
                        new Color(0.4f, 0.8f, 1f), 1.1f, 0.18f, 6f);
                }
            }

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

    /// <summary>Force the current driver (if any) out of the car, e.g. when it explodes.</summary>
    public void EjectDriver()
    {
        if (Driver != null) Driver.ForceExitCar();
    }
}
