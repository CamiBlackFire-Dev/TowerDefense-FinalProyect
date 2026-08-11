using UnityEngine;
using UnityEngine.InputSystem;

public class DroneController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 8f;
    [SerializeField] private DroneFlightArea flightArea;
    private PlayerInput playerInput;
    private DroneAbilityController abilityController;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        abilityController = GetComponent<DroneAbilityController>();
    }

    private void Update()
    {
        MoveDrone();

        HandleAbilities();
    }

    private void MoveDrone()
    {
        Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();

        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        transform.position += movement * movementSpeed * Time.deltaTime;

        transform.position = flightArea.ClampPosition(transform.position);
    }

    private void HandleAbilities()
    {
        if (playerInput.actions["Next"].WasPressedThisFrame())
        {
            abilityController.SwitchAbility();
        }

        if (playerInput.actions["Attack"].WasPressedThisFrame())
        {
            abilityController.UseAbility();
        }
    }
}