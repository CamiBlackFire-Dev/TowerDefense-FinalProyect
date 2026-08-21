using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TowerTargetDetector))]
public class CrossbowRainAttack : MonoBehaviour
{
    [Header("Arrow Data")]
    [SerializeField] private ArrowData arrowData;

    [Header("Projectile")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private int arrowsPerShot = 3;

    [Header("Attack")]
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstInterval = 0.5f;
    [SerializeField] private float attackCooldown = 8f;

    [Header("Prediction")]
    [SerializeField] private float predictionTime = 0.2f;
    [SerializeField] private float maxPredictionDistance = 1.5f;

    [Header("Target Search")]
    [SerializeField] private float targetSearchRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visual")]
    [SerializeField] private CrossbowArrowVisual arrowVisual;

    private TowerTargetDetector detector;

    private bool isAttacking;
    private float cooldownTimer;

    private void Awake()
    {
        detector = GetComponent<TowerTargetDetector>();

        detector = GetComponent<TowerTargetDetector>();

        if (arrowVisual != null && arrowData != null)
        {
            arrowVisual.UpdateVisual(arrowData);
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (isAttacking)
            return;

        if (arrowData == null)
            return;

        Transform currentTarget = detector.CurrentTarget;

        if (currentTarget == null)
            return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        for (int i = 0; i < burstCount; i++)
        {
            List<Transform> targets = GetTargets();

            if (targets.Count == 0)
                break;

            FireArrows(targets);

            if (i < burstCount - 1)
                yield return new WaitForSeconds(burstInterval);
        }

        isAttacking = false;

        cooldownTimer = attackCooldown;
    }

    private List<Transform> GetTargets()
    {
        List<Transform> targets = new List<Transform>();

        Collider[] enemies = Physics.OverlapSphere(transform.position, targetSearchRadius, enemyLayer);

        foreach (Collider enemy in enemies)
        {
            Transform target = enemy.GetComponentInParent<EnemyHealth>()?.transform;

            if (target == null)
                continue;

            if (!target.gameObject.activeInHierarchy)
                continue;

            if (targets.Contains(target))
                continue;

            targets.Add(target);

            if (targets.Count >= arrowsPerShot)
                break;
        }

        return targets;
    }

    private void FireArrows(List<Transform> targets)
    {
        foreach (Transform target in targets)
        {
            FireArrow(target);
        }
    }

    private void FireArrow(Transform target)
    {
        if (arrowData == null ||
            arrowData.prefab == null)
        {
            Debug.LogWarning("CrossbowRainAttack: ArrowData o prefab de flecha no asignado.");

            return;
        }

        Vector3 predictedPosition = GetPredictedPosition(target);

        Vector3 direction = predictedPosition - GetFirePosition();

        if (direction == Vector3.zero)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject arrow = Instantiate(arrowData.prefab, GetFirePosition(), rotation);

        CrossbowProjectile projectile = arrow.GetComponent<CrossbowProjectile>();

        if (projectile == null)
        {
            Debug.LogError("El prefab de flecha necesita el componente CrossbowProjectile.");

            Destroy(arrow);
            return;
        }

        projectile.Setup(target, arrowData, predictedPosition);
    }

    private Vector3 GetFirePosition()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.position;
    }

    private Vector3 GetPredictedPosition(Transform target)
    {
        Vector3 velocity = Vector3.zero;

        TestEnemyMovement movement = target.GetComponentInParent<TestEnemyMovement>();

        if (movement != null)
        {
            velocity = movement.transform.forward;
        }

        Vector3 prediction = velocity * predictionTime;

        prediction = Vector3.ClampMagnitude(prediction, maxPredictionDistance);

        return target.position + prediction;
    }

    public bool SetArrowData(ArrowData newArrowData)
    {
        if (newArrowData == null)
        {
            Debug.LogWarning("CrossbowRainAttack: ArrowData no puede ser null.");

            return false;
        }

        if (isAttacking)
        {
            Debug.LogWarning("CrossbowRainAttack: no se puede cambiar la flecha durante el ataque.");

            return false;
        }

        arrowData = newArrowData;

        if (arrowVisual != null)
        {
            arrowVisual.UpdateVisual(arrowData);
        }

        Debug.Log($"CrossbowRainAttack: flecha seleccionada -> {arrowData.arrowName}");

        return true;
    }

    public ArrowData GetCurrentArrowData()
    {
        return arrowData;
    }

    public bool HasArrowData()
    {
        return arrowData != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, targetSearchRadius);
    }
}