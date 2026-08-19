using UnityEngine;

public class RepairBarrel : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    private TowerHealth targetTower;
    private RepairAbility repairAbility;

    public void Setup(TowerHealth target, RepairAbility repair)
    {
        targetTower = target;

        repairAbility = repair;
    }

    private void Update()
    {
        if (targetTower == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetTower.transform.position, moveSpeed * Time.deltaTime);

        float distance = Vector3.Distance(transform.position, targetTower.transform.position);

        if (distance < 0.5f)
        {
            repairAbility.RepairTower(targetTower);

            Destroy(gameObject);
        }
    }
}