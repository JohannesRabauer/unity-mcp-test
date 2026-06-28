using UnityEngine;

/// <summary>
/// Emits escalating smoke (and small flames) from a damaged car. Reads the car's
/// shared <see cref="Health"/>; once health drops below a threshold it puffs dark
/// smoke at an interval that tightens as the wreck nears death. Purely cosmetic -
/// the actual explosion is still handled by <see cref="CarHealth"/>.
/// </summary>
[RequireComponent(typeof(Health))]
public class CarSmoke : MonoBehaviour
{
    public float smokeBelowFraction = 0.45f;
    public float fireBelowFraction = 0.2f;

    Health _health;
    float _timer;

    void Awake()
    {
        _health = GetComponent<Health>();
    }

    void Update()
    {
        if (_health == null || _health.IsDead) return;
        float frac = _health.current / Mathf.Max(1f, _health.maxHealth);
        if (frac >= smokeBelowFraction) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        // Tighter interval (more smoke) the closer to death.
        float severity = Mathf.InverseLerp(smokeBelowFraction, 0f, frac); // 0..1
        _timer = Mathf.Lerp(0.5f, 0.14f, severity);

        Vector3 c = transform.position + Vector3.up * 0.8f
            + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.3f, 0.3f));

        // Smoke: dark, rising-ish puff.
        Color smoke = Color.Lerp(new Color(0.25f, 0.25f, 0.28f), new Color(0.08f, 0.08f, 0.1f), severity);
        FxPop.Spawn(c + Vector3.up * 0.4f, smoke, Random.Range(1.2f, 2.2f), Random.Range(0.4f, 0.7f), 1.2f);

        // Flames once critical.
        if (frac < fireBelowFraction)
            FxPop.Spawn(c, new Color(1f, Random.Range(0.4f, 0.7f), 0.15f), Random.Range(0.7f, 1.3f), 0.25f, 6f);
    }
}
