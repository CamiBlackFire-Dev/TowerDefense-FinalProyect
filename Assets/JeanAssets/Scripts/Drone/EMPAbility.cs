using UnityEngine;

public class EMPAbility : MonoBehaviour
{
    [Header("EMP")]
    [SerializeField] private float empRadius = 10f;
    [SerializeField] private float slowDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float slowMultiplier = 0.4f;

    [Header("EMP Sphere")]
    [SerializeField] private LayerMask enemyLayer;
    private bool showGizmo;

    [Header("Enemy")]
    [SerializeField] private TestEnemyMovement testEnemyMovement;

    public void ActivateEMP()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, empRadius, enemyLayer);

        Debug.Log($"EMP: {enemies.Length} enemigos afectados.");

        foreach (Collider enemy in enemies)
        {
            TestEnemyMovement testEnemyMovement = enemy.GetComponent<TestEnemyMovement>();

            if (testEnemyMovement != null)
            {
                testEnemyMovement.ApplySlow(slowMultiplier, slowDuration);
            }
        }
    }

    public void SetGizmoVisible(bool visible)
    {
        showGizmo = visible;
    }

    private void OnDrawGizmosSelected()
    {
        if(!showGizmo)
        return;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(transform.position, empRadius);
    }

}
