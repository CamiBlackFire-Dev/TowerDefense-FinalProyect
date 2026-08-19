using UnityEngine;

public class RepulsionAbility : MonoBehaviour
{
    [Header("Repulsion")]
    [SerializeField] private float repulsionRadius = 5f;
    [SerializeField] private float repulsionDuration = 5f;

    [Header("Repulsion Sphere")]
    [SerializeField] private LayerMask enemyLayer;
    private bool showGizmo;

    [Header("Detection")]
    [SerializeField] private Path path;

    public void ActivateRepulsion(Vector3 position)
    {
        Collider[] enemies = Physics.OverlapSphere(position, repulsionRadius, enemyLayer);

        Debug.Log($"Repulsión: {enemies.Length} enemigos detectados.");

        foreach (Collider enemy in enemies)
        {
            int closestWaypoint = GetClosestWaypoint(enemy.transform.position);

            int previousWaypointIndex = closestWaypoint - 1;

            if (previousWaypointIndex < 0)
                continue;

            TestEnemyMovement enemyMovement = enemy.GetComponent<TestEnemyMovement>();

            if (enemyMovement != null)
            {
                enemyMovement.ReversePath(repulsionDuration);
            }
        }
    }

    private int GetClosestWaypoint(Vector3 enemyPosition)
    {
        Transform[] waypoints = path.GetWaypoints();

        int closestIndex = 0;

        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(enemyPosition, waypoints[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;

                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public void SetGizmoVisible(bool visible)
    {
        showGizmo = visible;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, repulsionRadius);
    }
}