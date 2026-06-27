using UnityEngine;

/// <summary>
/// Wandering pedestrian. Strolls between random points near its spawn and flees when shot.
/// Killing pedestrians raises the wanted level.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Health))]
public class PedestrianAI : MonoBehaviour
{
    public float walkSpeed = 2.4f;
    public float fleeSpeed = 5.5f;
    public float roamRadius = 12f;

    Rigidbody _rb;
    Health _health;
    Vector3 _home;
    Vector3 _target;
    float _repathTimer;
    float _fleeTimer;
    bool _dead;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        _health = GetComponent<Health>();
        _health.OnDamaged += (_, __) => { _fleeTimer = 4f; };
        _health.OnDied += _ => Die();
        _home = transform.position;
        PickNewTarget();
    }

    void FixedUpdate()
    {
        if (_dead) return;

        Vector3 dir;
        float speed;

        if (_fleeTimer > 0f)
        {
            _fleeTimer -= Time.fixedDeltaTime;
            Vector3 threat = PlayerController.Instance != null ? PlayerController.Instance.transform.position : _home;
            dir = transform.position - threat;
            speed = fleeSpeed;
        }
        else
        {
            _repathTimer -= Time.fixedDeltaTime;
            if (_repathTimer <= 0f || Vector3.Distance(transform.position, _target) < 1.2f)
                PickNewTarget();
            dir = _target - transform.position;
            speed = walkSpeed;
        }

        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            dir.Normalize();
            Vector3 v = dir * speed;
            _rb.linearVelocity = new Vector3(v.x, _rb.linearVelocity.y, v.z);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, Quaternion.LookRotation(dir), 0.2f));
        }
    }

    void PickNewTarget()
    {
        Vector2 r = Random.insideUnitCircle * roamRadius;
        _target = _home + new Vector3(r.x, 0f, r.y);
        _repathTimer = Random.Range(3f, 7f);
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        GameManager.Instance?.AddWanted(1);
        _rb.linearVelocity = Vector3.zero;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        Destroy(gameObject, 3f);
    }
}
