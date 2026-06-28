using UnityEngine;

/// <summary>
/// A small floating neon "$" that drops from a kill. The player collects it on
/// touch (or by driving over it) for cash. Self-builds its visual, bobs and spins,
/// drifts toward the player when close (magnet), and expires after a while.
/// </summary>
public class CashPickup : MonoBehaviour
{
    public int amount = 25;
    public float life = 14f;
    public float magnetRange = 4.5f;
    public float magnetSpeed = 9f;

    float _baseY;
    float _spin;
    bool _collected;

    public static CashPickup Spawn(Vector3 pos, int amount)
    {
        var go = new GameObject("CashPickup");
        go.transform.position = pos + Vector3.up * 0.5f;
        var c = go.AddComponent<CashPickup>();
        c.amount = amount;
        return c;
    }

    void Start()
    {
        _baseY = transform.position.y;
        _spin = Random.Range(0f, 360f);
        Build();
        // A gentle pop so the drop reads as an event.
        FxPop.Spawn(transform.position, new Color(0.4f, 1f, 0.55f), 1.1f, 0.18f, 5f);
    }

    void Build()
    {
        var col = new Color(0.35f, 1f, 0.5f);
        var mat = NeonFactory.Lit_(col, col, 3.2f, 0.6f);

        // Coin-ish flat cylinder.
        var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Coin";
        coin.transform.SetParent(transform, false);
        coin.transform.localScale = new Vector3(0.42f, 0.05f, 0.42f);
        coin.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        coin.GetComponent<Renderer>().sharedMaterial = mat;
        var cc = coin.GetComponent<Collider>();
        if (cc != null) Destroy(cc);

        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.radius = 0.7f;
        trigger.isTrigger = true;

        Destroy(gameObject, life);
    }

    void Update()
    {
        if (_collected) return;

        _spin += 180f * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, _spin, 0f);

        var player = PlayerController.Instance;
        if (player != null)
        {
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            float d = to.magnitude;
            if (d < magnetRange)
            {
                transform.position += to.normalized * magnetSpeed * Time.deltaTime;
                if (d < 0.9f) Collect();
                return;
            }
        }

        var p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null) Collect();
    }

    void Collect()
    {
        if (_collected) return;
        _collected = true;
        GameManager.Instance?.AddCash(amount);
        GameManager.Instance?.ShowBanner($"+${amount}", 0.7f);
        SfxManager.Play("coin", 0.7f, Random.Range(0.97f, 1.08f));
        FxPop.Spawn(transform.position + Vector3.up * 0.3f, new Color(0.5f, 1f, 0.6f), 1.2f, 0.2f, 6f);
        Destroy(gameObject);
    }
}
