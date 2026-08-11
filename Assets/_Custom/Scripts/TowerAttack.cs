using UnityEngine;

// Hace que la torre dispare al enemigo que tiene mas cerca.
// La deteccion y la punteria las hace TowerTargetDetector (script de Jean);
// aqui solo se decide cada cuanto dispara y cuanto dano hace.
// El dano, el alcance y la cadencia salen del TowerCatalog segun el nivel,
// asi una torre fusionada pega mas fuerte sin tocar codigo.
[RequireComponent(typeof(TowerTargetDetector))]
public class TowerAttack : MonoBehaviour
{
    [Header("Datos por nivel")]
    public TowerCatalog catalog;   // si esta vacio se usan los valores de abajo

    [Header("Valores por defecto")]
    public float damage = 5f;
    public float range = 4f;
    public float attackRate = 1f;  // disparos por segundo

    [Header("Deteccion")]
    public LayerMask enemyLayer;   // vacio = se usa la capa "Enemy"

    [Header("Efectos")]
    public GameObject impactEffect; // efecto opcional donde pega el disparo

    private TowerTargetDetector _detector;
    private Tower _tower;
    private float _cooldown;
    private int _appliedLevel = -1;

    private void OnEnable()
    {
        EnsureSetup();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        EnsureSetup();
        RefreshStats();

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f)
            return;

        Transform target = _detector.CurrentTarget;
        if (target == null || !IsInRange(target))
            return;

        Shoot(target);
    }

    // El enemigo esta lo bastante cerca para dispararle.
    public bool IsInRange(Transform target)
    {
        if (target == null)
            return false;

        return Vector3.Distance(transform.position, target.position) <= range;
    }

    // Le pega al enemigo y espera al siguiente disparo.
    public void Shoot(Transform target)
    {
        _cooldown = attackRate > 0f ? 1f / attackRate : 1f;

        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health != null)
            health.TakeDamage(damage);

        if (impactEffect != null)
            Instantiate(impactEffect, target.position, Quaternion.identity);
    }

    // Copia el dano, el alcance y la cadencia que le tocan al nivel actual,
    // y le pasa el alcance al detector.
    public void RefreshStats()
    {
        EnsureSetup();

        int level = _tower != null ? _tower.Level : 1;
        if (level == _appliedLevel)
            return;

        _appliedLevel = level;

        if (catalog != null)
        {
            TowerData data = catalog.GetLevelData(level);
            if (data != null)
            {
                damage = data.damage;
                range = data.range;
                attackRate = data.attackRate;
            }
        }

        _detector.SetDetectionRange(range);
    }

    private void EnsureSetup()
    {
        if (_detector == null)
        {
            _detector = GetComponent<TowerTargetDetector>();
            if (_detector == null)
                _detector = gameObject.AddComponent<TowerTargetDetector>();
        }

        if (_tower == null)
            _tower = GetComponent<Tower>();

        // Sin capa elegida se busca la capa "Enemy" del proyecto.
        if (enemyLayer.value == 0)
        {
            int enemy = LayerMask.NameToLayer("Enemy");
            if (enemy >= 0)
                enemyLayer = 1 << enemy;
        }

        _detector.SetEnemyLayer(enemyLayer);
    }
}
