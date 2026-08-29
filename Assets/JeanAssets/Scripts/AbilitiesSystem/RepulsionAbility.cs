using UnityEngine;

public class RepulsionAbility : MonoBehaviour
{
    [Header("Repulsion")]
    [SerializeField] private float repulsionRadius = 5f;
    [SerializeField] private float repulsionDuration = 5f;

    [Header("Repulsion Sphere")]
    [SerializeField] private LayerMask enemyLayer;
    private bool showGizmo;

    public void ActivateRepulsion(Vector3 position)
    {
        Collider[] enemies = Physics.OverlapSphere(position, repulsionRadius, enemyLayer);

        Debug.Log($"Repulsión: {enemies.Length} enemigos detectados.");

        foreach (Collider enemy in enemies)
        {
            TestEnemyMovement enemyMovement = enemy.GetComponent<TestEnemyMovement>();

            if (enemyMovement != null)
            {
                enemyMovement.ReversePath(repulsionDuration);
            }
        }
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