using UnityEngine;

public class CrossbowArrowSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CrossbowRainAttack crossbow;

    [Header("Arrow Slots")]
    [SerializeField] private CrossbowArrowSlot[] arrowSlots;

    [Header("Active Arrow Visual")]
    [SerializeField] private GameObject normalArrowVisual;
    [SerializeField] private GameObject fireArrowVisual;
    [SerializeField] private GameObject iceArrowVisual;
    [SerializeField] private GameObject poisonArrowVisual;

    private bool isUnlocked;

    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        SetUnlocked(false);
        HideAllArrowVisuals();
    }
    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;

        for (int i = 0; i < arrowSlots.Length; i++)
        {
            if (arrowSlots[i] != null)
                arrowSlots[i].SetSelectorEnabled(unlocked);
        }

        if (!unlocked)
            HideAllArrowVisuals();
    }

    public void UnlockArrow(ArrowData arrowData)
    {
        if (!isUnlocked)
            return;

        if (arrowData == null)
            return;

        for (int i = 0; i < arrowSlots.Length; i++)
        {
            if (arrowSlots[i] == null)
                continue;

            if (arrowSlots[i].ArrowData == arrowData)
            {
                arrowSlots[i].Unlock();
                return;
            }
        }
    }

    public bool SelectArrow(ArrowData arrowData)
    {
        if (!isUnlocked || arrowData == null)
            return false;

        for (int i = 0; i < arrowSlots.Length; i++)
        {
            if (arrowSlots[i] == null)
                continue;

            if (arrowSlots[i].ArrowData == arrowData)
            {
                if (!arrowSlots[i].IsUnlocked)
                    return false;

                if (crossbow == null)
                    return false;

                bool selected = crossbow.SetArrowData(arrowData);

                if (!selected)
                    return false;

                for (int j = 0; j < arrowSlots.Length; j++)
                {
                    if (arrowSlots[j] != null)
                    {
                        arrowSlots[j].SetSelected(
                            arrowSlots[j] == arrowSlots[i]
                        );
                    }
                }

                UpdateActiveArrowVisual(arrowData);

                return true;
            }
        }

        return false;
    }

    private void UpdateActiveArrowVisual(ArrowData arrowData)
    {
        HideAllArrowVisuals();

        switch (arrowData.effectType)
        {
            case ArrowEffectType.Normal:
                if (normalArrowVisual != null)
                    normalArrowVisual.SetActive(true);
                break;

            case ArrowEffectType.Fire:
                if (fireArrowVisual != null)
                    fireArrowVisual.SetActive(true);
                break;

            case ArrowEffectType.Ice:
                if (iceArrowVisual != null)
                    iceArrowVisual.SetActive(true);
                break;

            case ArrowEffectType.Poison:
                if (poisonArrowVisual != null)
                    poisonArrowVisual.SetActive(true);
                break;
        }
    }

    private void HideAllArrowVisuals()
    {
        if (normalArrowVisual != null)
            normalArrowVisual.SetActive(false);

        if (fireArrowVisual != null)
            fireArrowVisual.SetActive(false);

        if (iceArrowVisual != null)
            iceArrowVisual.SetActive(false);

        if (poisonArrowVisual != null)
            poisonArrowVisual.SetActive(false);
    }
}