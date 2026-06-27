using UnityEngine;

/// <summary>
/// Regenerates health while the owner avoids taking damage for a short window.
/// Listens to <see cref="Health.OnDamaged"/> to reset the delay, so staying out
/// of fights slowly heals you back up — rewards smart, evasive play.
/// </summary>
[RequireComponent(typeof(Health))]
public class HealthRegen : MonoBehaviour
{
    public float delayAfterDamage = 5f;
    public float healPerSecond = 8f;

    Health _health;
    float _cooldown;

    void Awake()
    {
        _health = GetComponent<Health>();
        _health.OnDamaged += (_, __) => _cooldown = delayAfterDamage;
    }

    void Update()
    {
        if (_health.IsDead) return;

        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
            return;
        }

        if (_health.current < _health.maxHealth)
            _health.current = Mathf.Min(_health.maxHealth, _health.current + healPerSecond * Time.deltaTime);
    }
}
