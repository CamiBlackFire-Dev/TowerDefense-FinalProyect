using System;
using UnityEngine;

public class BombAbility : MonoBehaviour
{
    [Header("Bomb")]
    [SerializeField] private Transform bombDropPoint;
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float explosionDuration = 1f;
    
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

    private void DetectEnemies(Vector3 explosionPosition)
    {
        Collider[] enemies = Physics.OverlapSphere(explosionPosition, explosionRadius, enemyLayer);

        Debug.Log($"Enemigos afectados: {enemies.Length}");

        foreach (Collider enemy in enemies)
        {
            // Aquí comunicaremos que el enemigo fue afectado
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