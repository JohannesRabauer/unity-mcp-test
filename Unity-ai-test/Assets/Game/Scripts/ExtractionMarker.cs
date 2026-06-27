using UnityEngine;

/// <summary>
/// Extraction zone. Stays dim until all loot is collected, then glows and wins on entry.
/// </summary>
public class ExtractionMarker : MonoBehaviour
{
    public Renderer glowRenderer;
    Color _base = new Color(0.1f, 0.1f, 0.12f);
    Color _active = new Color(0.2f, 1f, 0.5f);
    Material _mat;

    void Start()
    {
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;
        if (glowRenderer == null) glowRenderer = GetComponentInChildren<Renderer>();
        if (glowRenderer != null) _mat = glowRenderer.material;
    }

    void Update()
    {
        bool ready = GameManager.Instance != null && GameManager.Instance.AllCollected;
        if (_mat != null)
        {
            float pulse = ready ? (0.6f + 0.4f * Mathf.Sin(Time.time * 4f)) : 0.15f;
            Color c = ready ? _active : _base;
            _mat.SetColor("_BaseColor", c);
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", c * pulse * 3f);
        }
        float scale = ready ? 1f + 0.08f * Mathf.Sin(Time.time * 4f) : 1f;
                transform.localScale = new Vector3(scale, 1f, scale);
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || !GameManager.Instance.AllCollected) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        GameManager.Instance.Win();
    }
}
