using UnityEngine;

public class CrossbowProjectile : MonoBehaviour
{
    private Transform target;
    private ArrowData arrowData;

    private Vector3 targetPosition;
    private Vector3 startPosition;

    private float travelDistance;
    private float elapsedTime;

    [Header("Trajectory")]
    [SerializeField] private float arcHeight = 5f;

    private void Update()
    {
        if (arrowData == null)
        {
            Destroy(gameObject);
            return;
        }

        MoveTowardsTargetPosition();
    }

    public void Setup(Transform newTarget, ArrowData newArrowData, Vector3 predictedPosition)
    {
        target = newTarget;

        arrowData = newArrowData;

        startPosition = transform.position;

        targetPosition = predictedPosition;

        travelDistance = Vector3.Distance(startPosition, targetPosition);

        elapsedTime = 0f;
    }

    private void MoveTowardsTargetPosition()
    {
        if (travelDistance <= 0.01f)
        {
            transform.position = targetPosition;

            HitTarget();

            return;
        }

        float movement = arrowData.speed * Time.deltaTime;

        elapsedTime += movement / travelDistance;

        float progress = Mathf.Clamp01(elapsedTime);

        Vector3 position = Vector3.Lerp(startPosition, targetPosition, progress);

        float arc = Mathf.Sin(progress * Mathf.PI) * arcHeight;

        position += Vector3.up * arc;

        Vector3 direction = position - transform.position;

        transform.position = position;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (progress >= 1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        if (arrowData == null)
        {
            Destroy(gameObject);
            return;
        }

        if (target != null && target.gameObject.activeInHierarchy)
        {
            EnemyHealth health = target.GetComponentInParent<EnemyHealth>();

            if (health != null)
            {
                health.TakeDamage(arrowData.damage);

                EnemyStatusEffects statusEffects = target.GetComponentInParent<EnemyStatusEffects>();

                if (statusEffects != null)
                {
                    ApplyArrowEffect(statusEffects);
                }
            }
        }

        if (arrowData.effectPrefab != null)
        {
            GameObject effect = Instantiate(arrowData.effectPrefab, transform.position, Quaternion.identity);

            Destroy(effect, arrowData.effectDuration);
        }

        Destroy(gameObject);
    }

    private void ApplyArrowEffect(EnemyStatusEffects statusEffects)
    {
        switch (arrowData.effectType)
        {
            case ArrowEffectType.Normal:
                break;

            case ArrowEffectType.Fire:

                statusEffects.ApplyBurn(arrowData.effectValue, arrowData.effectDuration);

                break;

            case ArrowEffectType.Poison:

                statusEffects.ApplyPoison(arrowData.effectValue, 1f, arrowData.effectDuration);

                break;

            case ArrowEffectType.Ice:

                statusEffects.ApplyIce(arrowData.effectValue, arrowData.effectDuration);

                break;
        }
    }
}