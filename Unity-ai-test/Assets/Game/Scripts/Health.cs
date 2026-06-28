using System;
using UnityEngine;

/// <summary>
/// Shared damageable component for the player, pedestrians and police.
/// </summary>
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float current = 100f;
    public bool invulnerable = false;
    public bool IsDead => current <= 0f;

    public event Action<Health> OnDied;
    public event Action<Health, float> OnDamaged;

    /// <summary>Global kill feed: fired whenever any Health dies, with the instigator
    /// (the GameObject credited with the kill, may be null). Used by the style system.</summary>
    public static event Action<Health, GameObject> OnAnyDeath;

    GameObject _lastInstigator;

    void Awake()
    {
        current = maxHealth;
    }

    public void ResetHealth()
    {
        current = maxHealth;
    }

    public void TakeDamage(float amount, GameObject instigator = null)
    {
        if (invulnerable || IsDead) return;
        current = Mathf.Max(0f, current - amount);
        _lastInstigator = instigator;
        OnDamaged?.Invoke(this, amount);
        if (current <= 0f)
        {
            OnDied?.Invoke(this);
            OnAnyDeath?.Invoke(this, _lastInstigator);
        }
    }
}
