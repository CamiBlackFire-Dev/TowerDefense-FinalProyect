using UnityEngine;

public class EnemyTargetDetector : MonoBehaviour
{
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask towerLayer;

    private Transform currentTarget;

    public Transform CurrentTarget => currentTarget;

    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }

    public void SetTowerLayer(LayerMask layer)
    {
        towerLayer = layer;
    }

    private void Update()
    {
        FindClosestTower();
    }

    private void FindClosestTower()
    {
        Collider[] towers = Physics.OverlapSphere(transform.position, detectionRange, towerLayer);

        Transform closestTower = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider tower in towers)
        {
            float distance = Vector3.Distance(transform.position, tower.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;

                closestTower = tower.transform;
            }
        }

        currentTarget = closestTower;

        if (currentTarget != null)
        {
            Debug.Log($"{gameObject.name} detectó: {currentTarget.name}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );
    }
}