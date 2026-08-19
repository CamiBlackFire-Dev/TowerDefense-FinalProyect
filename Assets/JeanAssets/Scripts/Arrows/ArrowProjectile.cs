using UnityEngine;
using DamageNumbersPro;

public class ArrowProjectile : MonoBehaviour
{
    private Transform target;
    private ArrowData arrowData;
    private DamageNumber popupPrefab;
    private GameObject impactEffect;

    public void Setup(Transform target, ArrowData arrowData, DamageNumber popupPrefab, GameObject impactEffect)
    {
        this.target = target;
        this.arrowData = arrowData;
        this.popupPrefab = popupPrefab;
        this.impactEffect = impactEffect;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = target.position - transform.position;

        transform.position += direction.normalized * arrowData.speed * Time.deltaTime;

        transform.LookAt(target);

        if (Vector3.Distance(transform.position, target.position) <= 0.2f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(arrowData.damage);

            ApplyEffect(enemy);
        }

        if (popupPrefab != null)
        {
            popupPrefab.Spawn(target.position, arrowData.damage);
        }

        if (impactEffect != null)
        {
            Instantiate(impactEffect, target.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void ApplyEffect(EnemyHealth enemy)
    {
        switch (arrowData.effectType)
        {
            case ArrowEffectType.Normal:
                break;

            case ArrowEffectType.Fire:

                Debug.Log("Enemigo quemándose");

                // Aquí conectaremos el efecto de fuego.

                break;

            case ArrowEffectType.Ice:

                Debug.Log("Enemigo ralentizado");

                // Aquí conectaremos el efecto de hielo.

                break;
        }
    }
}