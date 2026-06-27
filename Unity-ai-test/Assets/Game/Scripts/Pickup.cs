using UnityEngine;

/// <summary>
/// Collectible loot. Registers itself with the GameManager and is picked up on touch.
/// </summary>
public class Pickup : MonoBehaviour
{
    public float spin = 90f;
    public float bobHeight = 0.25f;
    public float bobSpeed = 2f;

    float _baseY;
    bool _taken;

    void Start()
    {
        _baseY = transform.position.y;
        GameManager.Instance?.RegisterPickup();
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
        Vector3 p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_taken) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        _taken = true;
        SfxManager.Play("pickup", 0.8f);
        GameManager.Instance?.CollectPickup();
        Destroy(gameObject);
    }
}
