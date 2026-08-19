using UnityEngine;

public class RepairAbility : MonoBehaviour
{
    [Header("Repair")]
    [SerializeField] private float repairAmount = 40f;

    public TowerHealth GetMostDamagedTower()
    {
        TowerHealth[] towers = FindObjectsByType<TowerHealth>(FindObjectsSortMode.None);

        TowerHealth mostDamagedTower = null;

        float lowestHealthPercentage = 1f;

        foreach (TowerHealth tower in towers)
        {
            if (tower == null)
                continue;

            float healthPercentage = tower.CurrentHealth / tower.maxHealth;

            if (healthPercentage >= 1f)
                continue;

            if (healthPercentage < lowestHealthPercentage)
            {
                lowestHealthPercentage = healthPercentage;

                mostDamagedTower = tower;
            }
        }

        return mostDamagedTower;
    }

    public void RepairTower(TowerHealth tower)
    {
        if (tower == null)
            return;

        tower.Repair(repairAmount);

        Debug.Log($"Torre reparada: {tower.name}");
    }
}