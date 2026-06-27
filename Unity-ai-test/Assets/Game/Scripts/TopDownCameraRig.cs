using UnityEngine;

/// <summary>
/// Straight-overhead follow camera (classic GTA 1/2 framing).
/// Follows the current target with smoothing; target swaps when entering/leaving a car.
/// </summary>
public class TopDownCameraRig : MonoBehaviour
{
    public static TopDownCameraRig Instance { get; private set; }

    public Transform target;
    public float height = 26f;
    public float followLerp = 8f;
    public float lookAhead = 3.5f;

    Camera _cam;

    void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        // Straight down.
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 vel = Vector3.zero;
        var rb = target.GetComponentInParent<Rigidbody>();
        if (rb != null) vel = rb.linearVelocity;

        Vector3 lead = new Vector3(vel.x, 0f, vel.z).normalized * lookAhead * Mathf.Clamp01(vel.magnitude / 12f);
        Vector3 desired = new Vector3(target.position.x + lead.x, height, target.position.z + lead.z);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    /// <summary>Project a screen point onto the world ground plane at the given height.</summary>
    public bool ScreenToGround(Vector2 screen, float groundY, out Vector3 world)
    {
        world = Vector3.zero;
        if (_cam == null) _cam = GetComponent<Camera>();
        Ray ray = _cam.ScreenPointToRay(screen);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }
        return false;
    }
}
