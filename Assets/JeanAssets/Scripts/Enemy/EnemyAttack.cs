using UnityEngine;
using DamageNumbersPro;

[RequireComponent(typeof(EnemyTargetDetector))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRate = 1f;

    [Header("Projectile")]
    // Bala visible. Sin prefab asignado el dano es instantaneo, igual que
    // hace TowerAttack cuando no tiene proyectil.
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float muzzleHeight = 1f;

    [Header("Effects")]
    [SerializeField] private DamageNumber popupPrefab;
    [SerializeField] private GameObject impactEffect;

    private EnemyTargetDetector detector;
    private float cooldown;

    private void Awake()
    {
        detector = GetComponent<EnemyTargetDetector>();
    }

    // El spawner configura al enemigo recien creado, para que el prefab no
    // necesite saber nada de la escena (mismo criterio que EnemyHealth.Setup).
    public void Setup(float newDamage, float newAttackRate,
        GameObject newProjectilePrefab, float newProjectileSpeed,
        DamageNumber newPopup, GameObject newImpactEffect)
    {
        damage = newDamage;
        attackRate = newAttackRate;
        projectilePrefab = newProjectilePrefab;
        projectileSpeed = newProjectileSpeed;
        popupPrefab = newPopup;
        impactEffect = newImpactEffect;
    }

    private void Update()
    {
        Transform target = detector.CurrentTarget;

        if (target == null)
            return;

        cooldown -= Time.deltaTime;

        if (cooldown > 0f)
            return;

        Attack(target);

        cooldown = attackRate > 0f
            ? 1f / attackRate
            : 1f;
    }

    public bool HasTarget()
    {
        return detector.CurrentTarget != null;
    }

    private void Attack(Transform target)
    {
        TowerHealth towerHealth =
            target.GetComponentInParent<TowerHealth>();

        // Puede pasar sin que sea un error: el detector encuentra cualquier
        // collider de la capa de torres, y no todo lo que este ahi tiene
        // por que tener vida.
        if (towerHealth == null)
            return;

        // Con bala asignada el dano se aplica al impactar, no al disparar.
        if (projectilePrefab != null)
        {
            Vector3 origen = transform.position + Vector3.up * muzzleHeight;

            GameObject bala = Instantiate(
                projectilePrefab,
                origen,
                Quaternion.identity
            );

            EnemyProjectile projectile = bala.GetComponent<EnemyProjectile>();

            if (projectile != null)
            {
                projectile.speed = projectileSpeed;
                projectile.Setup(target, damage, popupPrefab, impactEffect);
                return;
            }

            // Si el prefab no trae EnemyProjectile no sirve de bala: se
            // borra y se sigue con el dano instantaneo de abajo.
            Destroy(bala);
        }

        towerHealth.TakeDamage(damage);

        if (popupPrefab != null)
        {
            popupPrefab.Spawn(
                target.position,
                damage
            );
        }

        if (impactEffect != null)
        {
            Instantiate(
                impactEffect,
                target.position,
                Quaternion.identity
            );
        }
    }
}
