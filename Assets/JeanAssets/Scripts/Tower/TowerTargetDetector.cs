using UnityEngine;

public class TowerTargetDetector : MonoBehaviour
{
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private LayerMask enemyLayer;

    private Transform currentTarget;

    // Enemigo elegido en este momento. Lo usa TowerAttack para dispararle.
    public Transform CurrentTarget
    {
        get { return currentTarget; }
    }

    // El alcance y la capa los define TowerAttack segun el nivel de la torre.
    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }

    public void SetEnemyLayer(LayerMask layer)
    {
        enemyLayer = layer;
    }

    private void Update()
    {
        FindClosestEnemy();
        CheckLineOfSight();
        AimAtTarget();
    }

    private void FindClosestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            detectionRange,
            enemyLayer
        );

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (Collider enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        currentTarget = closestEnemy;

        if (currentTarget != null)
        {
            //Debug.Log("Objetivo actual: " + currentTarget.name);
        }
    }

    private void CheckLineOfSight()
    {
        if (currentTarget == null)
            return;

        Vector3 direction = currentTarget.position - transform.position;

        if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == currentTarget)
            {
                //Debug.Log("Tengo línea de visión: " + currentTarget.name);
            }
            else
            {
                //Debug.Log("Hay un obstáculo: " + hit.transform.name);
            }

            Debug.DrawRay(transform.position, direction.normalized * hit.distance);
        }
    }

    private void AimAtTarget()
    {
        if (currentTarget == null)
            return;

        Vector3 direction = currentTarget.position - transform.position;

        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            float rotationSpeed = 5f;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}