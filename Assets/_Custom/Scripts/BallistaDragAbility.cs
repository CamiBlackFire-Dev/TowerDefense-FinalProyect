using System;
using System.Collections.Generic;
using UnityEngine;

public class BallistaDragAbility : MonoBehaviour, IDragAbility
{
    #region Configuration
    [Header("Referencias")]
    public GameObject ballistaPreviewPrefab;
    public Camera worldCamera;

    [Header("Targeting")]
    public float snapRadius = 4f;
    public float indicatorHeightOffset = 0.5f;

    [Header("Visuals")]
    public Material indicatorMaterial;
    public float indicatorRadius = 1.5f;
    public float blinkInterval = 0.15f;

    [Header("Drag Raycast")]
    public float groundHeight = 0.61f;
    public float snapScreenRadius = 50f;
    #endregion

    #region State Variables
    private GameObject _previewInstance;
    private GameObject _indicator;
    private Renderer _indicatorRenderer;
    private bool _dragging;
    private bool _hasValidTarget;
    private CrossbowSlot _currentTargetSlot;
    private float _blinkTimer;
    private List<CrossbowSlot> _allSlots;
    private List<GameObject> _slotHighlights = new List<GameObject>();
    #endregion

    #region Properties & Events
    public bool IsDragging => _dragging;
    public bool HasValidTarget => _hasValidTarget;
    public event Action<CrossbowSlot> OnBallistaPlaced;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }
    #endregion

    #region Drag Logic
    public void BeginDrag()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        _allSlots = new List<CrossbowSlot>(FindObjectsByType<CrossbowSlot>(FindObjectsSortMode.None));

        EnsureVisuals();
        _dragging = true;
        _hasValidTarget = false;
        _currentTargetSlot = null;
        _blinkTimer = 0f;

        if (_previewInstance != null)
            _previewInstance.SetActive(true);
        if (_indicator != null)
        {
            _indicator.SetActive(true);
            _indicatorRenderer.enabled = true;
        }

        foreach (var slot in _allSlots)
        {
            if (!slot.IsUnlocked)
            {
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                highlight.name = "SlotHighlight";
                highlight.transform.position = slot.VisualPosition + Vector3.up * 0.1f;
                highlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                highlight.transform.localScale = Vector3.one * indicatorRadius;

                Destroy(highlight.GetComponent<Collider>());

                Renderer r = highlight.GetComponent<Renderer>();
                if (indicatorMaterial != null)
                {
                    r.sharedMaterial = indicatorMaterial;
                }
                else
                {
                    Material mat = new Material(Shader.Find("Sprites/Default"));
                    r.sharedMaterial = mat;
                }
                r.material.color = new Color(1f, 1f, 0f, 0.4f);

                _slotHighlights.Add(highlight);
            }
        }
    }

    public void UpdateDrag(Vector2 screenPosition)
    {
        if (!_dragging || worldCamera == null)
            return;

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
        Vector3 groundPoint = Vector3.zero;

        if (ground.Raycast(ray, out float distance))
        {
            groundPoint = ray.GetPoint(distance);
        }

        _currentTargetSlot = null;
        float minScreenDistance = snapScreenRadius;

        foreach (var slot in _allSlots)
        {
            Vector3 slotScreenPos = worldCamera.WorldToScreenPoint(slot.VisualPosition);

            if (slotScreenPos.z < 0) continue;

            Vector2 slotScreen2D = new Vector2(slotScreenPos.x, slotScreenPos.y);
            float dist = Vector2.Distance(slotScreen2D, screenPosition);

            if (dist < minScreenDistance)
            {
                minScreenDistance = dist;
                _currentTargetSlot = slot;
            }
        }

        _hasValidTarget = (_currentTargetSlot != null && !_currentTargetSlot.IsUnlocked);

        if (_indicatorRenderer != null && _indicatorRenderer.material != null)
        {
            if (_hasValidTarget)
                _indicatorRenderer.material.color = new Color(0f, 1f, 0f, 0.5f);
            else
                _indicatorRenderer.material.color = new Color(1f, 0f, 0f, 0.5f);
        }

        if (_hasValidTarget)
        {
            Vector3 targetPos = _currentTargetSlot.VisualPosition;
            if (_previewInstance != null)
                _previewInstance.transform.position = targetPos;
            if (_indicator != null)
            {
                _indicator.transform.position = targetPos + Vector3.up * 0.05f;
                _indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
        else
        {
            if (_previewInstance != null)
                _previewInstance.transform.position = groundPoint;
            if (_indicator != null)
            {
                _indicator.transform.position = groundPoint + Vector3.up * 0.05f;
                _indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        _blinkTimer += Time.deltaTime;
        if (_blinkTimer >= blinkInterval)
        {
            _blinkTimer = 0f;
            if (_indicatorRenderer != null)
                _indicatorRenderer.enabled = !_indicatorRenderer.enabled;
        }
    }

    public void EndDrag()
    {
        if (!_dragging)
            return;

        _dragging = false;
        ClearHighlights();

        if (_previewInstance != null)
            _previewInstance.SetActive(false);
        if (_indicator != null)
            _indicator.SetActive(false);

        if (_hasValidTarget && _currentTargetSlot != null)
        {
            _currentTargetSlot.Unlock();
            OnBallistaPlaced?.Invoke(_currentTargetSlot);
        }
        else
        {
            _hasValidTarget = false;
        }
    }

    public void CancelDrag()
    {
        _dragging = false;
        ClearHighlights();

        if (_previewInstance != null)
            _previewInstance.SetActive(false);
        if (_indicator != null)
            _indicator.SetActive(false);
    }
    #endregion

    #region Visuals Management
    private void ClearHighlights()
    {
        foreach (var highlight in _slotHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }
        _slotHighlights.Clear();
    }

    private void EnsureVisuals()
    {
        if (_previewInstance == null && ballistaPreviewPrefab != null)
        {
            _previewInstance = Instantiate(ballistaPreviewPrefab, transform);
            _previewInstance.name = "HeldBallistaPreview";
            _previewInstance.SetActive(false);
        }

        if (_indicator == null)
        {
            _indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _indicator.name = "BallistaDropIndicator";
            _indicator.transform.SetParent(transform, false);

            Collider indicatorCollider = _indicator.GetComponent<Collider>();
            if (indicatorCollider != null)
                Destroy(indicatorCollider);

            _indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _indicator.transform.localScale = Vector3.one * indicatorRadius;

            _indicatorRenderer = _indicator.GetComponent<Renderer>();
            if (indicatorMaterial != null)
            {
                _indicatorRenderer.sharedMaterial = indicatorMaterial;
            }
            else
            {
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(1f, 0.92f, 0.016f, 0.5f);
                _indicatorRenderer.sharedMaterial = mat;
            }

            _indicator.SetActive(false);
        }
    }
    #endregion
}
