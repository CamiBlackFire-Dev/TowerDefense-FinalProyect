using UnityEngine;
using DamageNumbersPro;

public class EnemyProjectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    private DamageNumber popupPrefab;
    private GameObject impactEffect;
    private float speed;

    public void Setup(Transform target, float damage, float speed, DamageNumber popupPrefab, GameObject impactEffect)
    {
        this.target = target;

        this.damage = damage;

        this.speed = speed;

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

        transform.position += direction.normalized * speed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        TowerHealth health = target.GetComponentInParent<TowerHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);

            if (popupPrefab != null)
            {
                popupPrefab.Spawn(target.position, damage);
            }

            if (impactEffect != null)
            {
                Instantiate(impactEffect, target.position, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }
}