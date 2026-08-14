using UnityEngine;

public class EMPTestInput : MonoBehaviour
{
    [SerializeField] private EMPDragAbility empDragAbility;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            empDragAbility.BeginDrag();
        }

        if (Input.GetMouseButton(0))
        {
            empDragAbility.UpdateDrag(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            empDragAbility.EndDrag();
        }
    }
}