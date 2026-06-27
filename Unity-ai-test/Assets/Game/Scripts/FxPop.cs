using UnityEngine;

/// <summary>
/// Lightweight "juice" pop: a short-lived emissive sphere that scales up and vanishes.
/// Bloom turns it into a bright flash. Used for muzzle flashes, bullet impacts and deaths.
/// Needs no particle assets, so it is robust across pipelines.
/// </summary>
public class FxPop : MonoBehaviour
{
    float _life = 0.3f;
    float _max = 2.5f;
    float _t;

    public static void Spawn(Vector3 pos, Color color, float maxScale = 2.5f, float life = 0.3f, float emission = 4f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.name = "FxPop";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.25f;
        go.GetComponent<Renderer>().sharedMaterial = NeonFactory.Lit_(color * 0.15f, color, emission, 0.3f);
        var f = go.AddComponent<FxPop>();
        f._max = maxScale;
        f._life = life;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = _t / _life;
        if (k >= 1f) { Destroy(gameObject); return; }
        transform.localScale = Vector3.one * Mathf.Lerp(0.25f, _max, k);
    }
}
