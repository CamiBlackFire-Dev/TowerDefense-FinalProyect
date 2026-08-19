using UnityEngine;

public class EMPAbility : MonoBehaviour
{
    [Header("EMP")]
    [SerializeField] private float empRadius = 10f;
    [SerializeField] private float slowDuration = 3f;

    [SerializeField, Range(0f, 1f)]
    private float slowMultiplier = 0.4f;

    [Header("Detection")]
    [SerializeField] private LayerMask enemyLayer;

    public void ActivateEMP(Vector3 position)
    {
        Collider[] enemies =
            Physics.OverlapSphere(position, empRadius, enemyLayer);

        Debug.Log($"EMP: {enemies.Length} enemigos afectados.");

        foreach (Collider enemy in enemies)
        {
            TestEnemyMovement enemyMovement = enemy.GetComponent<TestEnemyMovement>();

            if (enemyMovement != null)
            {
                enemyMovement.ApplySlow(slowMultiplier, slowDuration);
            }
        }
    }

    public float GetSlowDuration()
    {
        return slowDuration;
    }

    public float GetRadius()
    {
        return empRadius;
    }

}
