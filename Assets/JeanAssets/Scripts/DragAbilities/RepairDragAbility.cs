using System.Collections;
using UnityEngine;

public class RepairDragAbility : MonoBehaviour, IDragAbility
{
    // Lo usa el HUD para saber si cobrar el uso al soltar.
    public bool HasValidTarget => hasValidTarget;

    [Header("References")]
    [SerializeField] private GameObject barrelPrefab;
    [SerializeField] private GameObject repairEffectPrefab;

    [Header("Repair")]
    [SerializeField] private float repairDuration = 1f;
    // Vida que recupera la torre reparada.
    [SerializeField] private float repairAmount = 25f;
    // Que tan lejos del punto donde se solto se busca una torre herida.
    [SerializeField] private float repairRadius = 20f;

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
        TowerHealth targetTower = FindMostDamagedTower();

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
                targetTower.transform.position,
                progress
            );

            yield return null;
        }

        barrel.transform.position = targetTower.transform.position;

        // Barril desaparece
        barrel.SetActive(false);

        // Aquí se cura de verdad (TowerHealth ya no deja pasar de maxHealth).
        float healthBefore = targetTower.CurrentHealth;
        targetTower.Repair(repairAmount);

        // Efecto de reparación
        if (repairEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                repairEffectPrefab,
                targetTower.transform.position,
                Quaternion.identity
            );

            Destroy(effect, 2f);
        }

        Debug.Log(
            $"Torre reparada: {targetTower.name} " +
            $"({healthBefore} -> {targetTower.CurrentHealth})"
        );
    }

    // Torre mas herida dentro del radio, o null si no hay ninguna a la que
    // le falte vida. Se buscan los componentes TowerHealth directamente en
    // vez de por collider/tag: las torres del tablero no tienen collider
    // propio (BoardManager solo les pone el modelo), asi que un
    // OverlapSphere no encontraria ninguna.
    private TowerHealth FindMostDamagedTower()
    {
        TowerHealth[] towers = Object.FindObjectsByType<TowerHealth>(FindObjectsSortMode.None);

        TowerHealth mostDamaged = null;
        float lowestRatio = 1f;

        foreach (TowerHealth tower in towers)
        {
            float distance = Vector3.Distance(targetPosition, tower.transform.position);

            if (distance > repairRadius)
                continue;

            // Se compara por porcentaje y no por vida absoluta: si no, una
            // torre de nivel alto con mucha vida maxima siempre ganaria
            // aunque este casi intacta.
            float ratio = tower.CurrentHealth / Mathf.Max(1f, tower.maxHealth);

            if (ratio < lowestRatio)
            {
                lowestRatio = ratio;
                mostDamaged = tower;
            }
        }

        return mostDamaged;
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