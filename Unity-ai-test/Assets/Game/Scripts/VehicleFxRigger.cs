using System.Collections;
using UnityEngine;

/// <summary>
/// Attaches a <see cref="CarSmoke"/> effect to every car in the scene at runtime so
/// damaged vehicles smoke and catch fire before they explode. Re-scans periodically
/// to cover cars that respawn. Drop on a manager GameObject.
/// </summary>
public class VehicleFxRigger : MonoBehaviour
{
    public float rescanInterval = 4f;

    IEnumerator Start()
    {
        // Let cars register themselves first.
        yield return null;
        while (true)
        {
            Rig();
            yield return new WaitForSeconds(rescanInterval);
        }
    }

    void Rig()
    {
        foreach (var car in CarController.All)
        {
            if (car == null) continue;
            if (car.GetComponent<CarSmoke>() == null && car.GetComponent<Health>() != null)
                car.gameObject.AddComponent<CarSmoke>();
        }
    }
}
