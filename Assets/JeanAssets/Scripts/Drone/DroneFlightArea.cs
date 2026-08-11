using UnityEngine;

public class DroneFlightArea : MonoBehaviour
{
    [Header("Area Center")]
    [SerializeField] private Vector3 areaCenter;

    [Header("Flight Limits")]
    [SerializeField] private float horizontalLimit = 10f;
    [SerializeField] private float depthLimit = 6f;

    public Vector3 ClampPosition(Vector3 position)
    {
        float minX = areaCenter.x - horizontalLimit;
        float maxX = areaCenter.x + horizontalLimit;

        float minZ = areaCenter.z - depthLimit;
        float maxZ = areaCenter.z + depthLimit;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);

        return position;
    }
}