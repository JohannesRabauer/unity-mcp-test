using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On-foot player: move, aim, shoot, take damage, die/respawn, and enter/exit cars.
/// Uses the new Input System (keyboard+mouse and gamepad) via direct device polling.
/// While driving, it forwards driving intent to the occupied CarController.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Move")]
    public float moveSpeed = 7f;
    public float accel = 30f;

    [Header("Jump")]
    public float jumpSpeed = 7.5f;
    public float jumpGravity = 24f;

    [Header("Combat")]
    public float gunHeight = 0.6f;
    public Weapon weapon;

    [Header("Vehicle")]
    public float enterRadius = 3.2f;

    public Health Health { get; private set; }
    public bool IsDriving => _car != null;

    Rigidbody _rb;
    Renderer[] _renderers;
    Collider _col;
    CarController _car;
    Vector3 _moveInput;
    Vector3 _aimDir = Vector3.forward;
    bool _shootHeld;
    bool _interactPressed;
    bool _handbrake;
    bool _boost;
    bool _hornPressed;
    bool _jumpQueued;
    bool _airborne;
    bool _haveGround;
    float _groundY;
    float _vyArc;
    float _respawnTimer;
    bool _dead;

    void Awake()
    {
        Instance = this;
        _rb = GetComponent<Rigidbody>();
        // Grounded Y stays frozen (stable footing); the jump temporarily releases it
        // and drives the vertical arc via direct transform writes.
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        _col = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<Renderer>();
        Health = GetComponent<Health>();
        if (Health == null) Health = gameObject.AddComponent<Health>();
        Health.OnDied += _ => Die();
        if (weapon == null) weapon = GetComponentInChildren<Weapon>();
    }

    void Update()
    {
        if (_dead)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f) Respawn();
            return;
        }

        ReadInput();

        if (_interactPressed)
        {
            if (IsDriving) ExitCar();
            else TryEnterCar();
        }

        // Shooting (on foot or from car).
        if (_shootHeld && weapon != null)
        {
            Vector3 origin = IsDriving
                ? _car.transform.position + _aimDir * 2.4f + Vector3.up * 0.7f
                : transform.position + transform.forward * 0.7f + Vector3.up * gunHeight;
            Vector3 dir = IsDriving && _aimDir.sqrMagnitude < 0.01f ? _car.transform.forward : _aimDir;
            if (weapon.TryFire(origin, dir, gameObject))
            {
                // Shooting near pedestrians draws heat (handled by hits); firing itself is minor noise.
            }
        }
    }

    void FixedUpdate()
    {
        if (_dead) return;

        if (IsDriving)
        {
            float throttle = _moveInput.z;
            float steer = _moveInput.x;
            _car.Drive(throttle, steer, _handbrake, _boost);
            return;
        }

        // On-foot movement (horizontal handled via velocity; vertical via the jump arc).
        Vector3 target = _moveInput.normalized * moveSpeed;
        Vector3 cur = _rb.linearVelocity;
        Vector3 flat = new Vector3(cur.x, 0f, cur.z);
        Vector3 newVel = Vector3.MoveTowards(flat, target, accel * Time.fixedDeltaTime);

        // Jumping. Grounded Y is frozen (stable). A jump releases the freeze and the
        // arc is integrated directly onto the transform so the constraint solver
        // never clamps it; landing re-freezes Y at the take-off height.
        if (!_haveGround) { _groundY = transform.position.y; _haveGround = true; }
        if (!_airborne && _jumpQueued)
        {
            _airborne = true;
            _groundY = transform.position.y;
            _vyArc = jumpSpeed;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            SfxManager.Play("jump", 0.6f);
            FxPop.Spawn(transform.position + Vector3.down * 0.35f, new Color(0.45f, 0.85f, 1f), 1.1f, 0.18f, 3f);
        }
        _jumpQueued = false;
        if (_airborne)
        {
            _vyArc -= jumpGravity * Time.fixedDeltaTime;
            float ny = transform.position.y + _vyArc * Time.fixedDeltaTime;
            if (_vyArc <= 0f && ny <= _groundY)
            {
                ny = _groundY;
                _airborne = false;
                _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
                FxPop.Spawn(new Vector3(transform.position.x, _groundY - 0.35f, transform.position.z), new Color(0.7f, 0.7f, 0.8f), 1.2f, 0.16f, 2.5f);
            }
            Vector3 hp = transform.position;
            transform.position = new Vector3(hp.x, ny, hp.z);
        }
        _rb.linearVelocity = new Vector3(newVel.x, 0f, newVel.z);

        // Face aim direction.
        if (_aimDir.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(_aimDir.x, 0f, _aimDir.z));
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, look, 1f - Mathf.Exp(-15f * Time.fixedDeltaTime)));
        }
    }

    void ReadInput()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        var ms = Mouse.current;

        Vector2 mv = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) mv.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) mv.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) mv.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) mv.x -= 1f;
        }
        if (gp != null)
        {
            Vector2 ls = gp.leftStick.ReadValue();
            if (ls.sqrMagnitude > 0.04f) mv = ls;
        }
        _moveInput = new Vector3(mv.x, 0f, mv.y);

        // Aim
        Vector3 aim = _aimDir;
        Vector3 aimRef = (IsDriving && _car != null) ? _car.transform.position : transform.position;
        bool gamepadAim = false;
        if (gp != null)
        {
            Vector2 rs = gp.rightStick.ReadValue();
            if (rs.sqrMagnitude > 0.06f)
            {
                aim = new Vector3(rs.x, 0f, rs.y);
                gamepadAim = true;
            }
        }
        if (!gamepadAim && ms != null && TopDownCameraRig.Instance != null)
        {
            if (TopDownCameraRig.Instance.ScreenToGround(ms.position.ReadValue(), aimRef.y, out Vector3 wp))
            {
                Vector3 d = wp - aimRef;
                d.y = 0f;
                if (d.sqrMagnitude > 0.04f) aim = d;
            }
        }
        if (aim.sqrMagnitude > 0.01f) _aimDir = aim.normalized;

        // Buttons
        _shootHeld = (ms != null && ms.leftButton.isPressed) || (gp != null && gp.rightTrigger.ReadValue() > 0.4f);
        _interactPressed = (kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
        _handbrake = (kb != null && kb.spaceKey.isPressed) || (gp != null && gp.buttonEast.isPressed);

        // Nitro boost (while driving): Left Shift / gamepad left trigger.
        _boost = (kb != null && kb.leftShiftKey.isPressed) || (gp != null && gp.leftTrigger.ReadValue() > 0.4f);

        // Horn (while driving): H / gamepad left stick press.
        _hornPressed = (kb != null && kb.hKey.wasPressedThisFrame) || (gp != null && gp.leftStickButton.wasPressedThisFrame);
        if (_hornPressed && IsDriving && _car != null) _car.Honk();

        // Jump (on foot only): Space on keyboard, North face button on gamepad.
        bool jumpPressed = (kb != null && kb.spaceKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        if (jumpPressed && !IsDriving) _jumpQueued = true;
    }

    void TryEnterCar()
    {
        CarController best = null;
        float bestDist = enterRadius;
        foreach (var car in CarController.All)
        {
            if (car == null || car.IsOccupied) continue;
            float d = Vector3.Distance(transform.position, car.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = car;
            }
        }
        if (best != null) EnterCar(best);
    }

    void EnterCar(CarController car)
    {
        _car = car;
        ResetAirborne();
        car.SetDriver(this);
        SetOnFootBodyEnabled(false);
        TopDownCameraRig.Instance?.SetTarget(car.transform);
        SfxManager.Play("car_start", 0.7f);
        GameManager.Instance?.ShowBanner("ENTERED VEHICLE", 1f);
    }

    void ExitCar()
    {
        if (_car == null) return;
        Vector3 exit = _car.GetExitPoint();
        _car.SetDriver(null);
        var leaving = _car;
        _car = null;
        SetOnFootBodyEnabled(true);
        transform.position = exit;
        _rb.linearVelocity = Vector3.zero;
        TopDownCameraRig.Instance?.SetTarget(transform);
        SfxManager.Play("car_stop", 0.6f);
        GameManager.Instance?.ShowBanner("LEFT VEHICLE", 1f);
    }

    /// <summary>Public hook for a car that is being destroyed under the player.</summary>
    public void ForceExitCar()
    {
        if (_car == null) return;
        Vector3 exit = _car.GetExitPoint();
        _car.SetDriver(null);
        _car = null;
        SetOnFootBodyEnabled(true);
        transform.position = exit;
        _rb.linearVelocity = Vector3.zero;
        TopDownCameraRig.Instance?.SetTarget(transform);
    }

    /// <summary>Cancel any in-air jump state and re-lock the body to the ground plane.</summary>
    void ResetAirborne()
    {
        _airborne = false;
        _jumpQueued = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
    }

    void SetOnFootBodyEnabled(bool on)
    {        if (_col != null) _col.enabled = on;
        foreach (var r in _renderers) if (r != null) r.enabled = on;
        _rb.isKinematic = !on;
        if (!on) _rb.linearVelocity = Vector3.zero;
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        if (IsDriving) ExitCar();
        ResetAirborne();
        SetOnFootBodyEnabled(true);
        _rb.linearVelocity = Vector3.zero;
        _respawnTimer = 2.5f;
        GameManager.Instance?.OnPlayerDied();
    }

    void Respawn()
    {
        _dead = false;
        ResetAirborne();
        _haveGround = false;
        Health.ResetHealth();
        Vector3 p = GameManager.Instance != null ? GameManager.Instance.respawnPoint : Vector3.zero;
        transform.position = p + Vector3.up * 1f;
        _rb.linearVelocity = Vector3.zero;
        if (GameManager.Instance != null) GameManager.Instance.wanted = 0;
        TopDownCameraRig.Instance?.SetTarget(transform);
    }
}
