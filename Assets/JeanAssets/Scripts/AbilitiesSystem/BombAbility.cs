using System;
using UnityEngine;
using DamageNumbersPro;

public class BombAbility : MonoBehaviour
{
    [Header("Bomb")]
    [SerializeField] private Transform bombDropPoint;
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float explosionDuration = 1f;
    [SerializeField] private float explosionDamage = 50f;

    [Header("Explosion visual")]
    // Disco que crece hasta explosionRadius y se desvanece, para que se
    // vea el area exacta que cubre la explosion. Sin material asignado
    // no se dibuja nada (el dano y la deteccion siguen funcionando igual).
    [SerializeField] private Material explosionVisualMaterial;
    [SerializeField] private float explosionVisualDuration = 0.4f;
    // VFX de fuego (Casual RPG VFX) que se instancia encima del disco, para
    // que la explosion se vea como una bola de fuego real y no solo el
    // area marcada. Sin prefab asignado no aparece nada, el resto sigue igual.
    [SerializeField] private GameObject explosionVfxPrefab;
    [SerializeField] private float explosionVfxDuration = 6.5f;
    // Texto "Boom" flotante (Damage Numbers Pro). Opcional: sin prefab
    // asignado no aparece nada, el resto de la explosion sigue igual.
    [SerializeField] private DamageNumber boomTextPrefab;
    [SerializeField] private float boomTextHeightOffset = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip explosionSound;

    [Header("Raycast")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask floorLayer;

    [Header("Bomb Sphere")]
    [SerializeField] private LayerMask enemyLayer;
    private Vector3 explosionPosition;
    private bool showGizmo;
    private bool hasExplosionPosition;
    private float explosionTimer;

    void Update()
    {
        if (!hasExplosionPosition)
        return;

        explosionTimer -= Time.deltaTime;

        if (explosionTimer <= 0f)
            {
                hasExplosionPosition = false;
            }
    }

    public void DropBomb()
    {
        if (bombDropPoint == null)
        {
            Debug.LogWarning("BombDropPoint no está asignado.");
            return;
        }

        if (Physics.Raycast(bombDropPoint.position, Vector3.down, out RaycastHit hit, raycastDistance, floorLayer))
        {
            explosionPosition = hit.point;

            hasExplosionPosition = true;

            explosionTimer = explosionDuration;

            DetectEnemies(explosionPosition);
        }
    }

    // Dispara la explosion en una posicion ya conocida, sin pasar por el
    // raycast hacia abajo de DropBomb. Sirve para otros sistemas que ya
    // saben donde cae la bomba (por ejemplo el arrastre desde la ranura
    // de habilidades, que calcula el punto de caida por su cuenta).
    public void ExplodeAt(Vector3 position)
    {
        explosionPosition = position;
        hasExplosionPosition = true;
        explosionTimer = explosionDuration;

        DetectEnemies(position);
    }

    private void DetectEnemies(Vector3 explosionPosition)
    {
        ExplosionVisual.Spawn(explosionPosition, explosionRadius, explosionVisualMaterial, explosionVisualDuration);

        if (explosionVfxPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVfxPrefab, explosionPosition, Quaternion.identity);
            Destroy(vfx, explosionVfxDuration);
        }

        if (boomTextPrefab != null)
            boomTextPrefab.Spawn(explosionPosition + Vector3.up * boomTextHeightOffset);

        if (AudioManager.Instance != null && explosionSound != null)
            AudioManager.Instance.PlaySFX(explosionSound);

        Collider[] enemies = Physics.OverlapSphere(explosionPosition, explosionRadius, enemyLayer);

        Debug.Log($"Enemigos afectados: {enemies.Length}");

        foreach (Collider enemy in enemies)
        {
            EnemyHealth health = enemy.GetComponentInParent<EnemyHealth>();
            if (health != null)
                health.TakeDamage(explosionDamage);
        }
    }

    public void SetGizmoVisible(bool visible)
    {
        showGizmo = visible;
    }

    private void OnDrawGizmosSelected()
    {
        if (bombDropPoint == null || !showGizmo)
        return;

        // Raycast

            Gizmos.color = Color.red;

            Gizmos.DrawRay(bombDropPoint.position, Vector3.down * raycastDistance);

        // Explosion radius

        if (!hasExplosionPosition)
            return;

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(explosionPosition, explosionRadius);
    }
}