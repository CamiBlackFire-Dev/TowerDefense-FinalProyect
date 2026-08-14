using UnityEngine;

public class DronePropeller : MonoBehaviour
{
    [SerializeField] private DroneDeployment droneDeployment;
    [SerializeField] private float rotationSpeed = 1000f;

    private void Update()
    {
        if (!droneDeployment.IsDeployed)
            return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}