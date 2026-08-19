using System.Collections;
using UnityEngine;

public class RepulsionDragAbility : MonoBehaviour, IDragAbility
{
    // Lo usa el HUD para saber si cobrar el uso al soltar.
    public bool HasValidTarget => hasValidTarget;

    [Header("References")]
    [SerializeField] private RepulsionAbility repulsionAbility;
    [SerializeField] private GameObject repulsionPrefab;

    [Header("Visual Effect")]
    [SerializeField] private GameObject effectPrefab;

    [Header("Throw")]
    [SerializeField] private float throwHeight = 2f;
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float effectDuration = 3f;

    [Header("Map")]
    [SerializeField] private LayerMask mapLayer;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Indicator")]
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private float indicatorSize = 1.5f;
    [SerializeField] private float indicatorHeight = 0.05f;
    [SerializeField] private float blinkSpeed = 0.2f;

    private Camera mainCamera;

    private GameObject repulsion;
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
        if (repulsion == null)
        {
            repulsion = Instantiate(repulsionPrefab);
        }

        isDragging = true;
        hasValidTarget = false;

        repulsion.SetActive(true);
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

            // La esfera sigue al dedo
            repulsion.transform.position =
                targetPosition + Vector3.up * throwHeight;

            // Mostrar indicador
            indicator.SetActive(true);

            indicator.transform.position =
                targetPosition + Vector3.up * indicatorHeight;

            UpdateIndicatorBlink();
        }
        else
        {
            // Fuera del mapa
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

        // Soltó fuera del mapa
        if (!hasValidTarget)
        {
            repulsion.SetActive(false);

            Debug.Log("Repulsión cancelada: fuera del mapa.");

            return;
        }

        StartCoroutine(DropRepulsion());
    }

    public void CancelDrag()
    {
        isDragging = false;
        hasValidTarget = false;

        if (repulsion != null)
            repulsion.SetActive(false);

        if (indicator != null)
            indicator.SetActive(false);
    }

    private IEnumerator DropRepulsion()
    {
        Vector3 startPosition = repulsion.transform.position;

        float timer = 0f;

        while (timer < fallDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(timer / fallDuration);

            repulsion.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress
                );

            yield return null;
        }

        // La bola llegó al suelo
        repulsion.transform.position = targetPosition;

        // Activar la repulsión
        if (repulsionAbility != null)
        {
            repulsionAbility.ActivateRepulsion(targetPosition);
        }

        // Mostrar efecto visual
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(
                effectPrefab,
                targetPosition,
                Quaternion.identity
            );

            Destroy(effect, effectDuration);
        }

        // Ocultar la bola
        repulsion.SetActive(false);
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

        indicator.name = "Repulsion Indicator";

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
            indicatorRenderer.material = indicatorMaterial;
        }

        indicator.SetActive(false);
    }
}
