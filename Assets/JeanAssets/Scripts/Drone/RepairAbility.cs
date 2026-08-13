using UnityEngine;

public class RepairAbility : MonoBehaviour
{
    [Header("Repair")]
    [SerializeField] private Transform repairDropPoint;
    [SerializeField] private float repairRadius = 5f;
    [SerializeField] private float repairAmount = 25f;

    [Header("Raycast")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask towerLayer;

    [Header("Repair Sphere")]
    private Vector3 repairPosition;
    private bool showGizmo;
    private bool hasRepairPosition;

    public void RepairTowers()
    {
        if (repairDropPoint == null)
        {
            Debug.LogWarning("RepairDropPoint no está asignado.");
            return;
        }

        if (!Physics.Raycast(repairDropPoint.position, Vector3.down, out RaycastHit hit, raycastDistance, towerLayer))
        {
            Debug.Log("No hay ninguna Torre debajo del Drone.");
            return;
        }

        repairPosition = hit.point;

        hasRepairPosition = true;

        Collider[] towers = Physics.OverlapSphere(repairPosition, repairRadius, towerLayer);

        foreach (Collider tower in towers)
        {
            RepairTower(tower.gameObject);
        }
    }

    private void RepairTower(GameObject tower)
    {
        Debug.Log($"Torre detectada: {tower.name} | " + $"Recuperación: {repairAmount}");

        // Aquí conectaremos el sistema de vida de la Torre.
    }

    public void SetGizmoVisible(bool visible)
    {
        showGizmo = visible;
    }

    private void OnDrawGizmosSelected()
    {
        if (repairDropPoint == null || !showGizmo)
            return;

        // Raycast

        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(repairDropPoint.position, Vector3.down * raycastDistance);

        // Sphere
        
        if (!hasRepairPosition)
            return;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(repairPosition, repairRadius);
    }
}