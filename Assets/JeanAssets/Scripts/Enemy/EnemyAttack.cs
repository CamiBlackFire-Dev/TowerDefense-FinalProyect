using UnityEngine;
using DamageNumbersPro;

[RequireComponent(typeof(EnemyTargetDetector))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRate = 1f;

    [Header("Effects")]
    [SerializeField] private DamageNumber popupPrefab;
    [SerializeField] private GameObject impactEffect;

    private EnemyTargetDetector detector;
    private float cooldown;

    private void Awake()
    {
        detector = GetComponent<EnemyTargetDetector>();
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
        Debug.Log(
            $"<color=yellow>⚔ ENEMIGO ATACANDO:</color> {target.name}"
        );

        TowerHealth towerHealth =
            target.GetComponentInParent<TowerHealth>();

        if (towerHealth == null)
        {
            Debug.LogError(
                $"❌ {target.name} no tiene TowerHealth."
            );

            return;
        }

        towerHealth.TakeDamage(damage);

        Debug.Log(
            $"<color=green>💥 DAÑO APLICADO:</color> {damage}"
        );

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
