using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityTestInput : MonoBehaviour
{
    [Header("Abilities")]
    [SerializeField] private BombDragAbility bomb;
    [SerializeField] private EMPDragAbility emp;
    [SerializeField] private RepulsionDragAbility repulsion;
    [SerializeField] private RepairDragAbility repair;

    private MonoBehaviour currentAbility;

    private void Update()
    {
        SelectAbility();

        if (currentAbility == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            BeginDrag();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            UpdateDrag();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }
    }

    private void SelectAbility()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            currentAbility = bomb;
            Debug.Log("Test: BOMB");
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            currentAbility = emp;
            Debug.Log("Test: EMP");
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            currentAbility = repulsion;
            Debug.Log("Test: REPULSION");
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            currentAbility = repair;
            Debug.Log("Test: REPAIR");
        }
    }

    private void BeginDrag()
    {
        if (currentAbility is BombDragAbility bombAbility)
            bombAbility.BeginDrag();

        else if (currentAbility is EMPDragAbility empAbility)
            empAbility.BeginDrag();

        else if (currentAbility is RepulsionDragAbility repulsionAbility)
            repulsionAbility.BeginDrag();

        else if (currentAbility is RepairDragAbility repairAbility)
            repairAbility.BeginDrag();
    }

    private void UpdateDrag()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        if (currentAbility is BombDragAbility bombAbility)
            bombAbility.UpdateDrag(mousePosition);

        else if (currentAbility is EMPDragAbility empAbility)
            empAbility.UpdateDrag(mousePosition);

        else if (currentAbility is RepulsionDragAbility repulsionAbility)
            repulsionAbility.UpdateDrag(mousePosition);

        else if (currentAbility is RepairDragAbility repairAbility)
            repairAbility.UpdateDrag(mousePosition);
    }

    private void EndDrag()
    {
        if (currentAbility is BombDragAbility bombAbility)
            bombAbility.EndDrag();

        else if (currentAbility is EMPDragAbility empAbility)
            empAbility.EndDrag();

        else if (currentAbility is RepulsionDragAbility repulsionAbility)
            repulsionAbility.EndDrag();

        else if (currentAbility is RepairDragAbility repairAbility)
            repairAbility.EndDrag();
    }
}