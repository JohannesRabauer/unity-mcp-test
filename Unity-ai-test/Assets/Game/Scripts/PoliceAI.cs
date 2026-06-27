using System.Collections;
using UnityEngine;

/// <summary>
/// Police unit. Idle until the player has a wanted level, then chases and shoots.
/// Aggression scales with the wanted level, and a busted cop redeploys from a map
/// edge while the heat is still on, keeping the pressure renewable.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Health))]
public class PoliceAI : MonoBehaviour
{
    public float chaseSpeed = 5.5f;
    public float shootRange = 16f;
    public float preferredDistance = 9f;
    public float activateRadius = 60f;

    [Header("Respawn")]
    public float redeployDelay = 4f;
    public float mapEdge = 54f;

    Rigidbody _rb;
    Health _health;
    Weapon _weapon;
    bool _dead;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        _health = GetComponent<Health>();
        _health.OnDied += _ => Die();
        _weapon = GetComponentInChildren<Weapon>();
    }

    void FixedUpdate()
    {
        if (_dead) return;
        var gm = GameManager.Instance;
        var player = PlayerController.Instance;
        if (gm == null || player == null) return;

        bool active = gm.wanted > 0;
        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (!active || dist > activateRadius)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 dir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;

        // Higher wanted = faster, more aggressive cops.
        float speed = chaseSpeed * (1f + (gm.wanted - 1) * 0.18f);

        // Move to keep within shooting distance.
        Vector3 move = Vector3.zero;
        if (dist > preferredDistance) move = dir;
        else if (dist < preferredDistance * 0.6f) move = -dir;

        Vector3 v = move * speed;
        _rb.linearVelocity = new Vector3(v.x, _rb.linearVelocity.y, v.z);
        if (dir.sqrMagnitude > 0.01f)
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, Quaternion.LookRotation(dir), 0.25f));

        // Shoot
        if (dist <= shootRange && _weapon != null)
        {
            Vector3 origin = transform.position + transform.forward * 0.8f + Vector3.up * 0.6f;
            _weapon.TryFire(origin, dir, gameObject);
        }
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        _rb.linearVelocity = Vector3.zero;
        SetVisible(false);

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.AddCash(gm.cashPerBust);
            gm.ShowBanner($"COP DOWN  +${gm.cashPerBust}", 1f);
        }

        StartCoroutine(Redeploy());
    }

    IEnumerator Redeploy()
    {
        yield return new WaitForSeconds(redeployDelay);

        var gm = GameManager.Instance;
        // Only redeploy while the player is still wanted and the run is ongoing.
        if (gm == null || gm.wanted <= 0 || gm.CurrentState != GameManager.State.Playing)
        {
            Destroy(gameObject);
            yield break;
        }

        // Reappear from a random map edge.
        float a = Random.value * Mathf.PI * 2f;
        transform.position = new Vector3(Mathf.Cos(a) * mapEdge, transform.position.y, Mathf.Sin(a) * mapEdge);
        _rb.linearVelocity = Vector3.zero;
        _health.ResetHealth();
        SetVisible(true);
        _dead = false;
    }

    void SetVisible(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = on;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = on;
    }
}
