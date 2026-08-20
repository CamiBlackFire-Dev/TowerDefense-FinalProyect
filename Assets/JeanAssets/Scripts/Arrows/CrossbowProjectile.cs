using UnityEngine;

public class CrossbowProjectile : MonoBehaviour
{
    private Transform target;
    private ArrowData arrowData;

    private void Update()
    {
        if (target == null ||
            !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        MoveTowardsTarget();
    }

    public void Setup(Transform newTarget, ArrowData newArrowData)
    {
        target = newTarget;
        arrowData = newArrowData;
    }

    private void MoveTowardsTarget()
    {
        Vector3 direction =
            target.position - transform.position;

        float distance =
            direction.magnitude;

        float movement =
            arrowData.speed * Time.deltaTime;

        if (movement >= distance)
        {
            transform.position = target.position;

            HitTarget();

            return;
        }

        transform.position +=
            direction.normalized * movement;

        if (direction != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }
    }

    private void HitTarget()
    {
        if (target == null ||
            arrowData == null)
        {
            Destroy(gameObject);
            return;
        }

        EnemyHealth health =
            target.GetComponentInParent<EnemyHealth>();

        if (health == null)
        {
            Destroy(gameObject);
            return;
        }

        // Daño base.
        health.TakeDamage(arrowData.damage);

        // Buscar efectos de estado.
        EnemyStatusEffects statusEffects =
            target.GetComponentInParent<EnemyStatusEffects>();

        if (statusEffects != null)
        {
            ApplyArrowEffect(statusEffects);
        }

        // Efecto visual del impacto.
        if (arrowData.effectPrefab != null)
        {
            GameObject effect = Instantiate(
                arrowData.effectPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(
                effect,
                arrowData.effectDuration
            );
        }

        Destroy(gameObject);
    }

    private void ApplyArrowEffect(
        EnemyStatusEffects statusEffects)
    {
        switch (arrowData.effectType)
        {
            case ArrowEffectType.Normal:
                break;

            case ArrowEffectType.Fire:

                statusEffects.ApplyBurn(
                    arrowData.effectValue,
                    arrowData.effectDuration
                );

                break;

            case ArrowEffectType.Poison:

                statusEffects.ApplyPoison(
                    arrowData.effectValue,
                    1f,
                    arrowData.effectDuration
                );

                break;

            case ArrowEffectType.Ice:

                statusEffects.ApplyIce(
                    arrowData.effectValue,
                    arrowData.effectDuration
                );

                break;
        }
    }
}