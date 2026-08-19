using System.Collections;
using UnityEngine;

public class RepairDragAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject barrelPrefab;
    [SerializeField] private GameObject repairEffectPrefab;

    [Header("Repair")]
    [SerializeField] private float repairDuration = 1f;

    [Header("Map")]
    [SerializeField] private LayerMask boardLayer;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Indicator")]
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private float indicatorSize = 1.5f;
    [SerializeField] private float indicatorHeight = 0.05f;
    [SerializeField] private float blinkSpeed = 0.2f;

    private Camera mainCamera;

    private GameObject barrel;
    private GameObject indicator;
    private Renderer indicatorRenderer;

    private Vector3 targetPosition;

    private bool isDragging;
    private bool hasValidTarget;

    private float blinkTimer;

    private void Awake()
    {
        mainCamera = Camera.main;

        CreateIndicator();
    }

    public void BeginDrag()
    {
        if (barrel == null)
        {
            barrel = Instantiate(barrelPrefab);
        }

        isDragging = true;
        hasValidTarget = false;

        barrel.SetActive(true);
        indicator.SetActive(false);

        blinkTimer = 0f;
    }

    public void UpdateDrag(Vector2 screenPosition)
    {
        if (!isDragging)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            raycastDistance,
            boardLayer))
        {
            hasValidTarget = true;
            targetPosition = hit.point;

            // Barril sigue al dedo
            barrel.transform.position =
                targetPosition + Vector3.up * 1f;

            // Indicador verde
            indicator.SetActive(true);

            indicator.transform.position =
                targetPosition + Vector3.up * indicatorHeight;

            UpdateIndicatorBlink();
        }
        else
        {
            hasValidTarget = false;
            indicator.SetActive(false);
        }
    }

    public void EndDrag()
    {
        if (!isDragging)
            return;

        isDragging = false;

        indicator.SetActive(false);

        if (!hasValidTarget)
        {
            barrel.SetActive(false);

            Debug.Log("Repair cancelado: fuera del tablero.");

            return;
        }

        StartCoroutine(RepairSequence());
    }

    public void CancelDrag()
    {
        isDragging = false;
        hasValidTarget = false;

        if (barrel != null)
            barrel.SetActive(false);

        if (indicator != null)
            indicator.SetActive(false);
    }

    private IEnumerator RepairSequence()
    {
        // Por ahora simulamos que encontró
        // la torre más dañada.
        Transform targetTower = FindMostDamagedTower();

        if (targetTower == null)
        {
            Debug.Log("No se encontró ninguna torre para reparar.");

            barrel.SetActive(false);

            yield break;
        }

        Vector3 startPosition = barrel.transform.position;

        float timer = 0f;

        while (timer < repairDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(timer / repairDuration);

            barrel.transform.position = Vector3.Lerp(
                startPosition,
                targetTower.position,
                progress
            );

            yield return null;
        }

        barrel.transform.position = targetTower.position;

        // Barril desaparece
        barrel.SetActive(false);

        // Efecto de reparación
        if (repairEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                repairEffectPrefab,
                targetTower.position,
                Quaternion.identity
            );

            Destroy(effect, 2f);
        }

        Debug.Log(
            $"Torre reparada: {targetTower.name}"
        );
    }

    private Transform FindMostDamagedTower()
    {
        // MAQUETA:
        // Por ahora simplemente busca las torres
        // dentro del tablero y toma la primera.
        //
        // Cuando tengamos TowerHealth,
        // aquí buscaremos la que tenga menor vida.

        Collider[] towers = Physics.OverlapSphere(
            targetPosition,
            20f
        );

        foreach (Collider tower in towers)
        {
            if (tower.CompareTag("Tower"))
            {
                return tower.transform;
            }
        }

        return null;
    }

    private void UpdateIndicatorBlink()
    {
        blinkTimer += Time.deltaTime;

        if (blinkTimer >= blinkSpeed)
        {
            blinkTimer = 0f;

            indicatorRenderer.enabled =
                !indicatorRenderer.enabled;
        }
    }

    private void CreateIndicator()
    {
        indicator = GameObject.CreatePrimitive(
            PrimitiveType.Quad
        );

        indicator.name = "Repair Indicator";

        Destroy(
            indicator.GetComponent<Collider>()
        );

        indicator.transform.rotation =
            Quaternion.Euler(90f, 0f, 0f);

        indicator.transform.localScale =
            Vector3.one * indicatorSize;

        indicatorRenderer =
            indicator.GetComponent<Renderer>();

        if (indicatorMaterial != null)
        {
            indicatorRenderer.material =
                indicatorMaterial;
        }

        indicator.SetActive(false);
    }
}