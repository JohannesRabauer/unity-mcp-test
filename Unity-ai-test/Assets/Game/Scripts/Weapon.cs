using UnityEngine;

/// <summary>
/// Simple hitscan weapon used by the player and police.
/// Fires a raycast, damages the first Health it hits, and draws a brief tracer.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Weapon : MonoBehaviour
{
    public float damage = 25f;
    public float range = 40f;
    public float fireRate = 6f;        // shots per second
    public float tracerTime = 0.04f;
    public Color tracerColor = new Color(1f, 0.3f, 0.8f);
    public LayerMask hitMask = ~0;

    float _nextFire;
    LineRenderer _line;
    float _tracerTimer;

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.enabled = false;
        _line.positionCount = 2;
        _line.widthMultiplier = 0.08f;
        _line.material = NeonFactory.TracerMaterial(tracerColor);
        _line.numCapVertices = 2;
    }

    public bool CanFire => Time.time >= _nextFire;

    /// <summary>Attempts to fire from origin toward dir (world space). Returns true if a shot went out.</summary>
    public bool TryFire(Vector3 origin, Vector3 dir, GameObject instigator)
    {
        if (!CanFire) return false;
        _nextFire = Time.time + 1f / Mathf.Max(0.01f, fireRate);

        dir.y = 0f;
        dir.Normalize();
        Vector3 end = origin + dir * range;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null && health.gameObject != instigator)
            {
                health.TakeDamage(damage, instigator);
            }
        }

        ShowTracer(origin, end);
        return true;
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
        if (_tracerTimer > 0f)
        {
            _tracerTimer -= Time.deltaTime;
            if (_tracerTimer <= 0f) _line.enabled = false;
        }
    }
}
