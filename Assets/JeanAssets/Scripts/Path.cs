using UnityEngine;

public class Path : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    public Transform[] GetWaypoints()
    {
        return waypoints;
    }

    // Permite que PathBuilder arme los waypoints por codigo.
    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
    }

    private void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {

                if (i < waypoints.Length - 1)
                {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);             
                }
            }
        }
    }
}