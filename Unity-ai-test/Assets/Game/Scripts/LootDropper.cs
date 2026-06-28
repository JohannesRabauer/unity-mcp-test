using UnityEngine;

/// <summary>
/// Drops collectible cash whenever an entity is killed by the player (directly or
/// by a car the player is driving). Listens to the global <see cref="Health.OnAnyDeath"/>
/// feed so it works for pedestrians, police and cars without touching their scripts.
/// Bigger, tougher targets pay more. Drop on a manager GameObject.
/// </summary>
public class LootDropper : MonoBehaviour
{
    public int pedReward = 20;
    public int copReward = 60;
    public int carReward = 45;
    public int barrelReward = 15;
    public int defaultReward = 15;

    void OnEnable() => Health.OnAnyDeath += OnAnyDeath;
    void OnDisable() => Health.OnAnyDeath -= OnAnyDeath;

    void OnAnyDeath(Health victim, GameObject instigator)
    {
        if (victim == null) return;
        if (!CreditedToPlayer(instigator)) return;

        int reward = RewardFor(victim);
        if (reward <= 0) return;

        // Slight scatter for multi-drops (e.g. explosions killing a crowd).
        Vector3 pos = victim.transform.position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
        CashPickup.Spawn(pos, reward);
    }

    static bool CreditedToPlayer(GameObject instigator)
    {
        var player = PlayerController.Instance;
        if (player == null || instigator == null) return false;
        if (instigator == player.gameObject) return true;
        // Kills dealt by a car the player drives (ram / car explosion) also count.
        var car = instigator.GetComponentInParent<CarController>();
        if (car != null && car.IsOccupied && car.Driver == player) return true;
        return false;
    }

    int RewardFor(Health victim)
    {
        var go = victim.gameObject;
        if (go.GetComponentInParent<PoliceAI>() != null) return copReward;
        if (go.GetComponentInParent<CarController>() != null) return carReward;
        if (go.GetComponentInParent<PedestrianAI>() != null) return pedReward;
        if (go.GetComponent<ExplosiveBarrel>() != null) return barrelReward;
        return defaultReward;
    }
}
