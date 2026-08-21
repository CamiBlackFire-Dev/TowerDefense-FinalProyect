using UnityEngine;
using DamageNumbersPro;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float hitDistance = 0.2f;

    [Header("Impact Effect")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private AudioClip impactSound;

    private Transform target;
    private float damage;
    private DamageNumber popupPrefab;

    public void Setup(Transform target, float damage, float speed, DamageNumber popupPrefab)
    {
        this.target = target;

        this.damage = damage;

        this.speed = speed;

        this.popupPrefab = popupPrefab;
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

        if (Vector3.Distance(transform.position, target.position) <= hitDistance)
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
        }

        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, target.position, Quaternion.identity);

            Destroy(effect, 3f);
        }

        if (AudioManager.Instance != null && impactSound != null)
        {
            AudioManager.Instance.PlaySFX(impactSound, true);
        }

        Destroy(gameObject);
    }
}