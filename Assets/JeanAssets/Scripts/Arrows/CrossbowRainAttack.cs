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
    // Margen fijo que se suma al tiempo de vuelo (retardo de reaccion).
    [SerializeField] private float predictionTime = 0.05f;
    // Tope de cuanto se adelanta el disparo. Tiene que dar de sobra para el
    // adelanto normal (velocidad del enemigo x tiempo de vuelo); esta solo
    // para que un objetivo lejanisimo no apunte a un punto irreal.
    [SerializeField] private float maxPredictionDistance = 8f;

    [Header("Target Search")]
    [SerializeField] private float targetSearchRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visual")]
    [SerializeField] private CrossbowArrowVisual arrowVisual;

    private TowerTargetDetector detector;

    private bool isAttacking;
    private float cooldownTimer;

    public bool IsRecharging => isAttacking || cooldownTimer > 0f;

    public float CooldownProgress
    {
        get
        {
            if (isAttacking)
                return 0f;
            if (attackCooldown <= 0f || cooldownTimer <= 0f)
                return 1f;

            return Mathf.Clamp01(1f - cooldownTimer / attackCooldown);
        }
    }

    private void Awake()
    {
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

        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            targetSearchRadius,
            enemyLayer
        );

        foreach (Collider enemy in enemies)
        {
            Transform target =
                enemy.GetComponentInParent<EnemyHealth>()?.transform;

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
        if (arrowData == null || arrowData.prefab == null)
        {
            Debug.LogWarning(
                "CrossbowRainAttack: ArrowData o prefab de flecha no asignado."
            );

            return;
        }

        Vector3 predictedPosition = GetPredictedPosition(target);

        Vector3 direction = predictedPosition - GetFirePosition();

        if (direction == Vector3.zero)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction);

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
            arrowData,
            predictedPosition
        );
    }

    private Vector3 GetFirePosition()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.position;
    }

    // A donde hay que apuntar para que la flecha y el enemigo lleguen al
    // mismo punto. Se adelanta el tiempo que la flecha va a tardar en
    // llegar, no una cantidad fija: una flecha lenta a un enemigo lejano
    // necesita mucho mas adelanto que una cercana.
    private Vector3 GetPredictedPosition(Transform target)
    {
        TestEnemyMovement movement =
            target.GetComponentInParent<TestEnemyMovement>();

        if (movement == null)
            return target.position;

        // transform.forward es un vector unitario (mide 1): es solo la
        // direccion. Multiplicarlo por la velocidad real es lo que lo
        // convierte en "cuanto avanza por segundo".
        Vector3 velocity = movement.transform.forward * movement.CurrentSpeed;

        float flightTime = GetFlightTime(target.position);

        Vector3 lead = Vector3.ClampMagnitude(
            velocity * flightTime,
            maxPredictionDistance
        );

        return target.position + lead;
    }

    // Cuanto tarda la flecha en cubrir la distancia hasta el objetivo.
    // predictionTime se suma como margen fijo (retardo de reaccion), y el
    // total se topa para que un objetivo muy lejano no dispare un adelanto
    // absurdo hacia un punto al que el enemigo nunca va a llegar (por
    // ejemplo si el camino dobla antes).
    private float GetFlightTime(Vector3 targetPosition)
    {
        if (arrowData == null || arrowData.speed <= 0f)
            return predictionTime;

        float distance = Vector3.Distance(GetFirePosition(), targetPosition);

        return distance / arrowData.speed + predictionTime;
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

        arrowData = newArrowData;

        if (arrowVisual != null)
        {
            arrowVisual.UpdateVisual(arrowData);
        }

        Debug.Log(
            $"CrossbowRainAttack: flecha seleccionada -> {arrowData.arrowName}"
        );

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
        Gizmos.DrawWireSphere(
            transform.position,
            targetSearchRadius
        );
    }
}