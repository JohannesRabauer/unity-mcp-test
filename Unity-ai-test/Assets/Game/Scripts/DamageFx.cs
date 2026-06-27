using UnityEngine;

/// <summary>
/// Visual feedback for damageables: a brief emissive hit-flash, a death pop, and
/// camera shake when the player is the one being hit. Add alongside a <see cref="Health"/>.
/// Uses per-entity material instances so flashing one actor never affects others.
/// </summary>
[RequireComponent(typeof(Health))]
public class DamageFx : MonoBehaviour
{
    public Color flashColor = Color.white;
    public float flashTime = 0.12f;

    MeshRenderer[] _rends;
    Color[] _baseEmis;
    bool[] _hadEmis;
    float _flash;
    bool _isPlayer;

    void Awake()
    {
        _rends = GetComponentsInChildren<MeshRenderer>();
        _baseEmis = new Color[_rends.Length];
        _hadEmis = new bool[_rends.Length];
        for (int i = 0; i < _rends.Length; i++)
        {
            var m = _rends[i].material; // instance so the flash is per-entity
            _hadEmis[i] = m.IsKeywordEnabled("_EMISSION");
            _baseEmis[i] = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
        }

        _isPlayer = GetComponent<PlayerController>() != null;
        var h = GetComponent<Health>();
        h.OnDamaged += (_, amt) => OnDamaged();
        h.OnDied += _ => OnDied();
    }

    void OnDamaged()
    {
        _flash = flashTime;
        if (_isPlayer) CameraShake.Shake(0.3f);
    }

    void OnDied()
    {
        Color c = flashColor;
        if (_rends.Length > 0 && _rends[0].sharedMaterial != null && _rends[0].sharedMaterial.HasProperty("_BaseColor"))
            c = _rends[0].sharedMaterial.GetColor("_BaseColor");
        FxPop.Spawn(transform.position + Vector3.up * 0.6f, Color.Lerp(c, flashColor, 0.6f), 3.2f, 0.4f, 5f);
        if (_isPlayer) CameraShake.Shake(0.7f);
    }

    void Update()
    {
        if (_flash <= 0f) return;
        _flash -= Time.deltaTime;
        float k = Mathf.Clamp01(_flash / flashTime);
        bool done = _flash <= 0f;
        for (int i = 0; i < _rends.Length; i++)
        {
            if (_rends[i] == null) continue;
            var m = _rends[i].material;
            if (done)
            {
                m.SetColor("_EmissionColor", _baseEmis[i]);
                if (!_hadEmis[i]) m.DisableKeyword("_EMISSION");
            }
            else
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.Lerp(_baseEmis[i], flashColor * 3f, k));
            }
        }
    }
}
