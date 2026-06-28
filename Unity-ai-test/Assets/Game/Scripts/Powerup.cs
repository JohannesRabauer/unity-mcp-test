using UnityEngine;

/// <summary>
/// World powerup the player picks up on touch. Two kinds:
///  - Health: restores a chunk of HP.
///  - Ammo:   refills the current weapon's reserve ammo.
/// Builds its own floating neon visual; no prefab assets needed.
/// </summary>
public class Powerup : MonoBehaviour
{
    public enum Kind { Health, Ammo }
    public Kind kind = Kind.Health;
    public float healAmount = 45f;
    public int ammoAmount = 120;
    public float respawnSeconds = 20f;

    float _baseY;
    bool _cooling;
    Renderer[] _renderers;
    Collider _trigger;

    public static Powerup Spawn(Vector3 pos, Kind kind)
    {
        var go = new GameObject(kind == Kind.Health ? "HealthPack" : "AmmoCrate");
        go.transform.position = pos;
        var p = go.AddComponent<Powerup>();
        p.kind = kind;
        return p;
    }

    void Start()
    {
        _baseY = transform.position.y + 0.6f;
        Build();
        _renderers = GetComponentsInChildren<Renderer>();
    }

    void Build()
    {
        Color col = kind == Kind.Health ? new Color(0.3f, 1f, 0.45f) : new Color(0.35f, 0.7f, 1f);
        var mat = NeonFactory.Lit_(col, col, 2.6f, 0.5f);

        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Core";
        box.transform.SetParent(transform, false);
        box.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        box.transform.localScale = Vector3.one * 0.7f;
        box.transform.localEulerAngles = new Vector3(0f, 45f, 0f);
        box.GetComponent<Renderer>().sharedMaterial = mat;
        var bc = box.GetComponent<Collider>();
        if (bc != null) Destroy(bc);

        // Cross / shells marker on top.
        var markCol = Color.white;
        var markMat = NeonFactory.Lit_(markCol, markCol, 3.2f, 0.5f);
        if (kind == Kind.Health)
        {
            MakeMark(new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.16f, 0.16f), markMat);
            MakeMark(new Vector3(0f, 0.6f, 0f), new Vector3(0.16f, 0.5f, 0.16f), markMat);
        }
        else
        {
            MakeMark(new Vector3(-0.13f, 0.6f, 0f), new Vector3(0.12f, 0.42f, 0.12f), markMat);
            MakeMark(new Vector3(0.13f, 0.6f, 0f), new Vector3(0.12f, 0.42f, 0.12f), markMat);
        }

        // Trigger volume.
        _trigger = gameObject.AddComponent<SphereCollider>();
        ((SphereCollider)_trigger).radius = 1.2f;
        ((SphereCollider)_trigger).center = new Vector3(0f, 0.6f, 0f);
        _trigger.isTrigger = true;
    }

    void MakeMark(Vector3 lpos, Vector3 lscale, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = lpos + Vector3.forward * -0.42f;
        g.transform.localScale = lscale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        var c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    void Update()
    {
        transform.Rotate(Vector3.up, 70f * Time.deltaTime, Space.World);
        if (!_cooling)
        {
            var p = transform.position;
            p.y = _baseY + Mathf.Sin(Time.time * 2f) * 0.18f - 0.6f;
            transform.position = p;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_cooling) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        bool used = false;
        if (kind == Kind.Health)
        {
            var h = player.Health;
            if (h != null && h.current < h.maxHealth)
            {
                h.current = Mathf.Min(h.maxHealth, h.current + healAmount);
                GameManager.Instance?.ShowBanner($"HEALTH +{Mathf.RoundToInt(healAmount)}", 1.2f);
                used = true;
            }
        }
        else
        {
            var wp = player.weapon;
            if (wp != null && !wp.infiniteAmmo)
            {
                wp.SetAmmo(wp.Magazine, wp.Reserve + ammoAmount);
                GameManager.Instance?.ShowBanner($"AMMO +{ammoAmount}", 1.2f);
                used = true;
            }
        }

        if (!used) return;
        SfxManager.Play("pickup", 0.8f, kind == Kind.Health ? 1.1f : 0.85f);
        FxPop.Spawn(transform.position + Vector3.up * 0.6f,
            kind == Kind.Health ? new Color(0.3f, 1f, 0.45f) : new Color(0.35f, 0.7f, 1f), 1.4f, 0.25f, 4f);
        StartCooldown();
    }

    void StartCooldown()
    {
        _cooling = true;
        foreach (var r in _renderers) if (r != null) r.enabled = false;
        if (_trigger != null) _trigger.enabled = false;
        Invoke(nameof(Respawn), respawnSeconds);
    }

    void Respawn()
    {
        _cooling = false;
        foreach (var r in _renderers) if (r != null) r.enabled = true;
        if (_trigger != null) _trigger.enabled = true;
    }
}
