using UnityEngine;

/// <summary>
/// Floating quest marker: gently bobs up and down and spins so it is easy to spot
/// from the top-down camera. Purely cosmetic.
/// </summary>
public class BeaconBob : MonoBehaviour
{
    public float bobHeight = 0.4f;
    public float bobSpeed = 2f;
    public float spinSpeed = 110f;

    Vector3 _base;

    void Start()
    {
        _base = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition = _base + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}
