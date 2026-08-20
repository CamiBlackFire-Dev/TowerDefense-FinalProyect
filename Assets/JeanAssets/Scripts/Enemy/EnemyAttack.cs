using UnityEngine;
using DamageNumbersPro;

[RequireComponent(typeof(EnemyTargetDetector))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRate = 1f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileHeight = 1f;

    [Header("Effects")]
    [SerializeField] private DamageNumber popupPrefab;

    private EnemyTargetDetector detector;
    private float cooldown;

    private void Awake()
    {
        detector = GetComponent<EnemyTargetDetector>();
    }

    // Puente para EnemySpawner (Assets/_Custom): le permite configurar esta
    // ranura al vuelo por oleada, igual que ya hace con EnemyHealth.Setup.
    // No toca projectileHeight a proposito: eso queda como ajuste fijo del
    // prefab, no algo que varie oleada a oleada.
    public void Setup(float newDamage, float newAttackRate,
        GameObject newProjectilePrefab, float newProjectileSpeed,
        DamageNumber newPopup)
    {
        damage = newDamage;
        attackRate = newAttackRate;
        projectilePrefab = newProjectilePrefab;
        projectileSpeed = newProjectileSpeed;
        popupPrefab = newPopup;
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
        if (projectilePrefab == null)
            return;

        Vector3 spawnPosition = transform.position + Vector3.up * projectileHeight;

        GameObject arrow = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        EnemyProjectile projectile = arrow.GetComponent<EnemyProjectile>();

        if (projectile != null)
        {
            projectile.Setup(target, damage, projectileSpeed, popupPrefab);
        }
        else
        {
            Debug.LogWarning("El projectilePrefab no tiene el componente EnemyProjectile.");
        }
    }
}