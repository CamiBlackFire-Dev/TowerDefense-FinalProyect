using System.Collections;
using UnityEngine;

public class EMPDragAbility : MonoBehaviour, IDragAbility
{
    // Lo usa el HUD para saber si cobrar el uso al soltar.
    public bool HasValidTarget => hasValidTarget;

    [Header("References")]
    [SerializeField] private EMPAbility empAbility;
    [SerializeField] private GameObject empPrefab;

    [Header("Visual Effect")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float effectDuration = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip impactSound;

    [Header("Throw")]
    [SerializeField] private float throwHeight = 2f;
    [SerializeField] private float fallDuration = 0.5f;

    [Header("Map")]
    [SerializeField] private LayerMask mapLayer;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Indicator")]
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private float indicatorSize = 1.5f;
    [SerializeField] private float indicatorHeight = 0.05f;
    [SerializeField] private float blinkSpeed = 0.2f;

    private Camera mainCamera;

    private GameObject emp;
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
        if (emp == null)
        {
            emp = Instantiate(empPrefab);
        }

        isDragging = true;

        hasValidTarget = false;

        emp.SetActive(true);

        indicator.SetActive(false);

        blinkTimer = 0f;
    }

    public void UpdateDrag(Vector2 screenPosition)
    {
        if (!isDragging)
            return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, mapLayer))
        {
            hasValidTarget = true;

            targetPosition = hit.point;

            emp.transform.position = targetPosition + Vector3.up * throwHeight;

            indicator.SetActive(true);

            indicator.transform.position = targetPosition + Vector3.up * indicatorHeight;

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
            emp.SetActive(false);

            Debug.Log("EMP cancelado: fuera del mapa.");

            return;
        }

        StartCoroutine(DropEMP());
    }

    public void CancelDrag()
    {
        isDragging = false;

        hasValidTarget = false;

        if (emp != null)
            emp.SetActive(false);

        if (indicator != null)
            indicator.SetActive(false);
    }

    private IEnumerator DropEMP()
    {
        Vector3 startPosition = emp.transform.position;

        float timer = 0f;

        while (timer < fallDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / fallDuration);

            emp.transform.position =
                Vector3.Lerp(startPosition, targetPosition, progress);

            yield return null;
        }

        emp.transform.position = targetPosition;

        emp.SetActive(false);

        if (empAbility != null)
        {
            empAbility.ActivateEMP(targetPosition);
        }

        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, targetPosition, Quaternion.identity);

            Destroy(effect, effectDuration);
        }

        if (AudioManager.Instance != null && impactSound != null)
        {
            AudioManager.Instance.PlaySFX(impactSound);
        }
    }

    private void UpdateIndicatorBlink()
    {
        blinkTimer += Time.deltaTime;

        if (blinkTimer >= blinkSpeed)
        {
            blinkTimer = 0f;

            indicatorRenderer.enabled = !indicatorRenderer.enabled;
        }
    }

    private void CreateIndicator()
    {
        indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);

        indicator.name = "EMP Indicator";

        Destroy(indicator.GetComponent<Collider>());

        indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        indicator.transform.localScale = Vector3.one * indicatorSize;

        indicatorRenderer = indicator.GetComponent<Renderer>();

        if (indicatorMaterial != null)
        {
            indicatorRenderer.material = indicatorMaterial;
        }

        indicator.SetActive(false);
    }
}