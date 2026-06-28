using UnityEngine;

/// <summary>
/// A shootable / rammable explosive barrel. Builds its own neon visual on Start,
/// takes damage via a <see cref="Health"/> component, and detonates with a radial
/// blast that damages everything nearby - including other barrels, so chains ripple
/// across the city. Robust and self-contained: dropping this on an empty GameObject
/// (or letting <see cref="BarrelSpawner"/> place it) is enough.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Explosion")]
    public float blastRadius = 6f;
    public float blastDamage = 80f;
    public float carDamage = 90f;
    public float force = 9f;

    Health _health;
    Rigidbody _rb;
    bool _exploded;

    void Start()
    {
        gameObject.name = "ExplosiveBarrel";
        _rb = GetComponent<Rigidbody>();
        _rb.mass = 40f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.linearDamping = 1.5f;

        _health = GetComponent<Health>();
        if (_health == null) _health = gameObject.AddComponent<Health>();
        _health.maxHealth = 30f;
        _health.ResetHealth();
        _health.OnDied += _ => Explode();

        BuildVisual();
    }

    void BuildVisual()
    {
        // Replace any primitive renderer/scale with a barrel-shaped body.
        var existing = GetComponent<MeshRenderer>();
        if (existing == null)
        {
            // Build from a cylinder primitive child if this object has no mesh.
            transform.localScale = Vector3.one;
        }

        var bodyCol = new Color(0.9f, 0.35f, 0.08f);
        var stripe = new Color(1f, 0.85f, 0.1f);

        var body = MakeChild("Body", PrimitiveType.Cylinder, new Vector3(0f, 0.7f, 0f),
            new Vector3(0.8f, 0.7f, 0.8f), NeonFactory.Lit_(bodyCol, bodyCol, 1.6f, 0.4f), keepCollider: true);
        // The body's collider acts as the barrel hit volume.
        var cc = body.GetComponent<CapsuleCollider>();
        if (cc != null) cc.radius = 0.5f;

        MakeChild("Stripe", PrimitiveType.Cylinder, new Vector3(0f, 0.95f, 0f),
            new Vector3(0.83f, 0.12f, 0.83f), NeonFactory.Lit_(stripe, stripe, 2.6f, 0.4f), keepCollider: false);
        MakeChild("Stripe2", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f),
            new Vector3(0.83f, 0.12f, 0.83f), NeonFactory.Lit_(stripe, stripe, 2.6f, 0.4f), keepCollider: false);
    }

    GameObject MakeChild(string nm, PrimitiveType prim, Vector3 lpos, Vector3 lscale, Material mat, bool keepCollider)
    {
        var g = GameObject.CreatePrimitive(prim);
        g.name = nm;
        var col = g.GetComponent<Collider>();
        if (!keepCollider && col != null) Destroy(col);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = lpos;
        g.transform.localScale = lscale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    public void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        Vector3 c = transform.position + Vector3.up * 0.6f;
        FxPop.Spawn(c, new Color(1f, 0.55f, 0.12f), blastRadius * 1.3f, 0.4f, 7f);
        FxPop.Spawn(c, new Color(1f, 0.9f, 0.4f), blastRadius * 0.7f, 0.28f, 9f);
        SfxManager.Play("explosion", 0.95f, Random.Range(0.85f, 1.05f));
        CameraShake.Shake(0.55f);

        var hits = Physics.OverlapSphere(c, blastRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.attachedRigidbody != null)
            {
                h.attachedRigidbody.AddExplosionForce(force * 120f, c, blastRadius, 1.2f, ForceMode.Impulse);
            }

            // Chain to other barrels.
            var otherBarrel = h.GetComponentInParent<ExplosiveBarrel>();
            if (otherBarrel != null && otherBarrel != this && !otherBarrel._exploded)
            {
                otherBarrel.Invoke(nameof(Explode), Random.Range(0.04f, 0.16f));
                continue;
            }

            var car = h.GetComponentInParent<Health>();
            if (car != null && car.gameObject != gameObject)
            {
                bool isCar = h.GetComponentInParent<CarController>() != null;
                car.TakeDamage(isCar ? carDamage : blastDamage, gameObject);
            }
        }

        // Leave a brief scorch flash, then remove the barrel.
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        Destroy(gameObject, 0.5f);
    }
}
