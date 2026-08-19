using System.Collections;
using UnityEngine;

public class RepairDragAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RepairAbility repairAbility;
    [SerializeField] private GameObject barrelPrefab;

    [Header("Visual Effect")]
    [SerializeField] private GameObject repairEffect;
    [SerializeField] private float effectDuration = 3f;

    [Header("Throw")]
    [SerializeField] private float throwHeight = 1f;
    [SerializeField] private float moveSpeed = 5f;

    [Header("Map")]
    [SerializeField] private LayerMask mapLayer;
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
            mapLayer))
        {
            hasValidTarget = true;

            targetPosition = hit.point;

            barrel.transform.position =
                targetPosition + Vector3.up * throwHeight;

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

            Debug.Log("Repair cancelado: fuera del mapa.");

            return;
        }

        StartCoroutine(SendBarrel());
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

    private IEnumerator SendBarrel()
    {
        TowerHealth targetTower =
            repairAbility.GetMostDamagedTower();

        if (targetTower == null)
        {
            barrel.SetActive(false);

            Debug.Log("No hay torres dañadas para reparar.");

            yield break;
        }

        while (true)
        {
            if (barrel == null)
                yield break;

            if (targetTower == null)
            {
                barrel.SetActive(false);

                Debug.Log(
                    "Repair cancelado: la torre objetivo fue destruida."
                );

                yield break;
            }

            barrel.transform.position =
                Vector3.MoveTowards(
                    barrel.transform.position,
                    targetTower.transform.position,
                    moveSpeed * Time.deltaTime
                );

            float distance =
                Vector3.Distance(
                    barrel.transform.position,
                    targetTower.transform.position
                );

            if (distance <= 0.5f)
                break;

            yield return null;
        }

        if (targetTower == null)
        {
            barrel.SetActive(false);

            yield break;
        }

        Vector3 effectPosition =
            targetTower.transform.position;

        repairAbility.RepairTower(targetTower);

        if (repairEffect != null)
        {
            GameObject effect =
                Instantiate(
                    repairEffect,
                    effectPosition,
                    Quaternion.identity
                );

            Destroy(effect, effectDuration);
        }

        barrel.SetActive(false);
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
        indicator =
            GameObject.CreatePrimitive(
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