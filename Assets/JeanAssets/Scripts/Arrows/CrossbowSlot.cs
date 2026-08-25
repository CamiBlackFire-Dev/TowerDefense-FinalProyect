using UnityEngine;

public class CrossbowSlot : MonoBehaviour
{
    [Header("Crossbow")]
    [SerializeField] private GameObject crossbow;

    [Header("Arrow Selector")]
    [SerializeField] private CrossbowArrowSelector arrowSelector;

    private bool isUnlocked;

    public bool IsUnlocked => isUnlocked;

    public Vector3 VisualPosition 
    {
        get 
        {
            if (crossbow != null) return crossbow.transform.position;
            return transform.position;
        }
    }

    private void Awake()
    {
        if (crossbow != null)
            crossbow.SetActive(false);

        isUnlocked = false;

        if (arrowSelector != null)
            arrowSelector.SetUnlocked(false);
    }
    public void Unlock()
    {
        if (isUnlocked)
            return;

        isUnlocked = true;

        if (crossbow != null)
            crossbow.SetActive(true);

        if (arrowSelector != null)
            arrowSelector.SetUnlocked(true);

        Debug.Log($"Ballesta desbloqueada: {gameObject.name}");
    }

    public void Lock()
    {
        isUnlocked = false;

        if (crossbow != null)
            crossbow.SetActive(false);

        if (arrowSelector != null)
            arrowSelector.SetUnlocked(false);
    }
    public void UnlockArrow(ArrowData arrowData)
    {
        if (!isUnlocked)
        {
            Debug.LogWarning("No se puede desbloquear una flecha porque la ballesta está bloqueada.");
            return;
        }

        if (arrowSelector != null)
            arrowSelector.UnlockArrow(arrowData);
    }
    public bool SelectArrow(ArrowData arrowData)
    {
        if (!isUnlocked || arrowSelector == null)
            return false;

        return arrowSelector.SelectArrow(arrowData);
    }
}