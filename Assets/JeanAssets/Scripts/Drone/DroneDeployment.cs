using UnityEngine.InputSystem;
using UnityEngine;

public class DroneDeployment : MonoBehaviour
{
    [Header("Drone")]
    [SerializeField] private GameObject drone;
    [SerializeField] private float droneDuration = 20f;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;

    private float remainingTime;
    public bool IsDeployed { get; private set; }

    private void Start()
    {
        drone.SetActive(false);
        IsDeployed = false;
    }

    private void Update()
    {
        HandleDeployment();

        if (!IsDeployed)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            RecallDrone();
        }
    }

    private void HandleDeployment()
    {
        if (playerInput.actions["Interact"].WasPressedThisFrame())
        {
            DeployDrone();
        }
    }

    public void DeployDrone()
    {
        if (IsDeployed)
            return;

        drone.SetActive(true);
        IsDeployed = true;

        remainingTime = droneDuration;

        Debug.Log("Drone desplegado");
    }

    public void RecallDrone()
    {
        if (!IsDeployed)
            return;

        drone.SetActive(false);
        IsDeployed = false;

        Debug.Log("Drone retirado");
    }
}