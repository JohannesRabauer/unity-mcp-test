using UnityEngine;

/// <summary>
/// Hitscan weapon used by the player and police. Supports magazines + reloading,
/// multi-pellet spread (shotgun), per-weapon tracer colour and synth fire sound.
/// Police use <see cref="infiniteAmmo"/> so they never need to reload.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Weapon : MonoBehaviour
{
    [Header("Identity")]
    public string weaponName = "Pistol";
    public string fireSfx = "shot_pistol";

    [Header("Ballistics")]
    public float damage = 25f;
    public float range = 40f;
    public float fireRate = 6f;        // shots per second
    public int pellets = 1;            // >1 = shotgun spread
    public float spread = 0f;          // cone half-angle in degrees
    public Color tracerColor = new Color(1f, 0.3f, 0.8f);
    public LayerMask hitMask = ~0;

    [Header("Ammo")]
    public bool infiniteAmmo = true;   // police / fallback
    public bool autoReload = true;
    public int magazineSize = 12;
    public int reserveAmmo = 120;
    public float reloadTime = 1.2f;

    [Header("Tracer")]
    public float tracerTime = 0.04f;

    /// <summary>Fired after any successful shot: (muzzle origin, instigator). Used by crowds to panic.</summary>
    public static event System.Action<Vector3, GameObject> OnFired;

    /// <summary>Global damage scaler (e.g. Rampage powerup). Applied to every hit.</summary>
    public static float DamageMultiplier = 1f;

    /// <summary>When true the player's gun never consumes ammo (Rampage powerup).</summary>
    public static bool ForceInfinite = false;

    float _nextFire;
    int _mag;
    bool _reloading;
    float _reloadTimer;
    LineRenderer _line;
    float _tracerTimer;

    public int Magazine => _mag;
    public int Reserve => reserveAmmo;
    public bool IsReloading => _reloading;
    public float ReloadProgress => _reloading && reloadTime > 0f ? 1f - _reloadTimer / reloadTime : 1f;
    public bool CanFire => Time.time >= _nextFire && !_reloading;

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.enabled = false;
        _line.positionCount = 2;
        _line.widthMultiplier = 0.08f;
        _line.material = NeonFactory.TracerMaterial(tracerColor);
        _line.numCapVertices = 2;
        _mag = magazineSize;
    }

    /// <summary>Set live ammo (used by the loadout when switching weapons).</summary>
    public void SetAmmo(int magazine, int reserve)
    {
        _mag = Mathf.Clamp(magazine, 0, magazineSize);
        reserveAmmo = Mathf.Max(0, reserve);
        _reloading = false;
        _line.material = NeonFactory.TracerMaterial(tracerColor);
    }

    public void CancelReload() { _reloading = false; }

    /// <summary>Attempts to fire from origin toward dir (world space). Returns true if a shot went out.</summary>
    public bool TryFire(Vector3 origin, Vector3 dir, GameObject instigator)
    {
        if (_reloading || Time.time < _nextFire) return false;

        bool isPlayer = PlayerController.Instance != null && instigator == PlayerController.Instance.gameObject;

        bool freeAmmo = infiniteAmmo || (isPlayer && ForceInfinite);

        if (!freeAmmo && _mag <= 0)
        {
            if (autoReload && reserveAmmo > 0) StartReload();
            else if (isPlayer) SfxManager.Play("dry", 0.6f);
            _nextFire = Time.time + 0.25f;
            return false;
        }

        _nextFire = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        if (!freeAmmo) _mag--;

        dir.y = 0f;
        dir.Normalize();

        int shots = Mathf.Max(1, pellets);
        Vector3 lastEnd = origin + dir * range;
        for (int i = 0; i < shots; i++)
        {
            Vector3 d = spread > 0f ? ConeSpread(dir, spread) : dir;
            lastEnd = FireRay(origin, d, instigator);
        }

        ShowTracer(origin, lastEnd);

        OnFired?.Invoke(origin, instigator);

        // Muzzle flash + kick.
        FxPop.Spawn(origin + dir * 0.3f, tracerColor, 0.9f, 0.07f, 6f);
        if (isPlayer)
        {
            CameraShake.Shake(pellets > 1 ? 0.12f : 0.05f);
            SfxManager.Play(fireSfx, 0.85f, Random.Range(0.96f, 1.05f));
        }
        else
        {
            SfxManager.Play(fireSfx, 0.4f, Random.Range(0.9f, 1.0f));
        }

        return true;
    }

    Vector3 FireRay(Vector3 origin, Vector3 dir, GameObject instigator)
    {
        Vector3 end = origin + dir * range;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null && health.gameObject != instigator)
            {
                bool wasAlive = !health.IsDead;
                health.TakeDamage(damage * DamageMultiplier, instigator);
                if (wasAlive && health.IsDead) SfxManager.Play("explosion", 0.7f, Random.Range(0.9f, 1.1f));
                else SfxManager.Play("hit", 0.5f, Random.Range(0.95f, 1.1f));
            }
            FxPop.Spawn(end, new Color(1f, 0.9f, 0.5f), 0.8f, 0.12f, 5f);
        }
        return end;
    }

    Vector3 ConeSpread(Vector3 dir, float degrees)
    {
        float a = Random.Range(-degrees, degrees) * Mathf.Deg2Rad;
        return new Vector3(
            dir.x * Mathf.Cos(a) - dir.z * Mathf.Sin(a),
            0f,
            dir.x * Mathf.Sin(a) + dir.z * Mathf.Cos(a)).normalized;
    }

    public void StartReload()
    {
        if (infiniteAmmo || _reloading) return;
        if (_mag >= magazineSize || reserveAmmo <= 0) return;
        _reloading = true;
        _reloadTimer = reloadTime;
        SfxManager.Play("reload", 0.7f);
    }

    void FinishReload()
    {
        int need = magazineSize - _mag;
        int take = Mathf.Min(need, reserveAmmo);
        _mag += take;
        reserveAmmo -= take;
        _reloading = false;
    }

    void ShowTracer(Vector3 a, Vector3 b)
    {
        _line.SetPosition(0, a);
        _line.SetPosition(1, b);
        _line.enabled = true;
        _tracerTimer = tracerTime;
    }

    void Update()
    {
        if (_reloading)
        {
            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer <= 0f) FinishReload();
        }

        if (_tracerTimer > 0f)
        {
            _tracerTimer -= Time.deltaTime;
            if (_tracerTimer <= 0f) _line.enabled = false;
        }
    }
}
