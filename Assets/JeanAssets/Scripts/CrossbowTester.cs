using UnityEngine;

public class CrossbowTester : MonoBehaviour
{
    [SerializeField] private CrossbowSlot crossbowSlot;

    [Header("Arrow Data")]
    [SerializeField] private ArrowData fireArrow;
    [SerializeField] private ArrowData iceArrow;
    [SerializeField] private ArrowData poisonArrow;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            crossbowSlot.Unlock();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            crossbowSlot.UnlockArrow(fireArrow);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            crossbowSlot.UnlockArrow(iceArrow);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            crossbowSlot.UnlockArrow(poisonArrow);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            crossbowSlot.SelectArrow(fireArrow);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            crossbowSlot.SelectArrow(iceArrow);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            crossbowSlot.SelectArrow(poisonArrow);
        }
    }
}