using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TowerTargetDetector))]
public class CrossbowRainAttack : MonoBehaviour
{
    [Header("Arrow Data")]
    [SerializeField] private ArrowData arrowData;

    [Header("Projectile")]
    [SerializeField] private Transform firePoint;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 5f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private float attackCooldown = 8f;

    [Header("Prediction")]
    [SerializeField] private float predictionTime = 0.2f;
    [SerializeField] private float maxPredictionDistance = 1.5f;

    private TowerTargetDetector detector;

    private bool isAttacking;
    private float cooldownTimer;

    private Vector3 previousTargetPosition;
    private Vector3 targetVelocity;

    private void Awake()
    {
        detector = GetComponent<TowerTargetDetector>();
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

        Transform target = detector.CurrentTarget;

        if (target == null)
            return;

        if (arrowData == null)
            return;

        StartCoroutine(AttackRoutine(target));
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        isAttacking = true;

        previousTargetPosition = target.position;
        targetVelocity = Vector3.zero;

        float timer = 0f;

        while (timer < attackDuration)
        {
            if (target == null ||
                !target.gameObject.activeInHierarchy)
            {
                break;
            }

            UpdateTargetVelocity(target);

            FireArrow(target);

            timer += damageInterval;

            yield return new WaitForSeconds(damageInterval);
        }

        isAttacking = false;
        cooldownTimer = attackCooldown;
    }

    private void FireArrow(Transform target)
    {
        if (arrowData == null ||
            arrowData.prefab == null)
        {
            Debug.LogWarning(
                "CrossbowRainAttack: ArrowData o prefab de flecha no asignado."
            );

            return;
        }

        Vector3 predictedPosition =
            GetPredictedPosition(target);

        Vector3 direction =
            predictedPosition - GetFirePosition();

        if (direction == Vector3.zero)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(direction);

        GameObject arrow = Instantiate(
            arrowData.prefab,
            GetFirePosition(),
            rotation
        );

        CrossbowProjectile projectile =
            arrow.GetComponent<CrossbowProjectile>();

        if (projectile == null)
        {
            Debug.LogError(
                "El prefab de flecha necesita el componente CrossbowProjectile."
            );

            Destroy(arrow);
            return;
        }

        projectile.Setup(
            target,
            arrowData
        );
    }

    private Vector3 GetFirePosition()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.position;
    }

    private void UpdateTargetVelocity(Transform target)
    {
        Vector3 currentPosition = target.position;

        if (Time.deltaTime > 0f)
        {
            targetVelocity =
                (currentPosition - previousTargetPosition)
                / Time.deltaTime;
        }

        previousTargetPosition = currentPosition;
    }

    private Vector3 GetPredictedPosition(Transform target)
    {
        Vector3 prediction =
            targetVelocity * predictionTime;

        prediction = Vector3.ClampMagnitude(
            prediction,
            maxPredictionDistance
        );

        return target.position + prediction;
    }

    public bool SetArrowData(ArrowData newArrowData)
    {
        if (newArrowData == null)
        {
            Debug.LogWarning(
                "CrossbowRainAttack: ArrowData no puede ser null."
            );

            return false;
        }

        if (isAttacking)
        {
            Debug.LogWarning(
                "CrossbowRainAttack: no se puede cambiar la flecha durante el ataque."
            );

            return false;
        }

        arrowData = newArrowData;

        Debug.Log(
            $"CrossbowRainAttack: flecha seleccionada -> {arrowData.arrowName}"
        );

        return true;
    }
}