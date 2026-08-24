using UnityEngine;

public class CrossbowArrowSlot : MonoBehaviour
{
    [Header("Arrow")]
    [SerializeField] private ArrowData arrowData;

    [Header("Visual")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    [SerializeField] private GameObject selectedVisual;

    private CrossbowArrowSelector selector;

    private bool isUnlocked;
    private bool isSelected;

    public ArrowData ArrowData => arrowData;
    public bool IsUnlocked => isUnlocked;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        selector = GetComponentInParent<CrossbowArrowSelector>();

        UpdateVisual();
    }

    public void SetSelectorEnabled(bool enabled)
    {
        if (!enabled)
        {
            isSelected = false;
        }

        UpdateVisual();
    }

    public void Unlock()
    {
        isUnlocked = true;

        UpdateVisual();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (lockedVisual != null)
            lockedVisual.SetActive(!isUnlocked);

        if (unlockedVisual != null)
            unlockedVisual.SetActive(isUnlocked);

        if (selectedVisual != null)
            selectedVisual.SetActive(isUnlocked && isSelected);
    }

    private void OnMouseDown()
    {
        Debug.Log("CLICK FIRE SLOT");

        if (!isUnlocked)
        {
            Debug.Log("FIRE ESTÁ BLOQUEADA");
            return;
        }

        if (selector == null)
        {
            Debug.LogError("NO HAY SELECTOR");
            return;
        }

        Debug.Log("SELECCIONANDO: " + arrowData.arrowName);

        selector.SelectArrow(arrowData);
    }
}
