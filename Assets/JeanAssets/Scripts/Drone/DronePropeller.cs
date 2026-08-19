using UnityEngine;

public class DronePropeller : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 1000f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}