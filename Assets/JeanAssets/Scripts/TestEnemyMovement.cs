using System.Collections;
using UnityEngine;

public class TestEnemyMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private Path path;
    private Coroutine slowCoroutine;
    private float originalSpeed;
    private Transform[] waypoints;
    private int currentWaypoint = 0;

    private void Start()
    {
        waypoints = path.GetWaypoints();

        originalSpeed = movementSpeed;
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentWaypoint];

        Vector3 direction = target.position - transform.position;

        transform.position += direction.normalized * movementSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                Debug.Log("El enemigo llegó al final del Path");
                enabled = false;
            }
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        slowCoroutine = StartCoroutine(SlowCoroutine(multiplier, duration));
    }

    private IEnumerator SlowCoroutine(float multiplier, float duration)
    {
        movementSpeed = originalSpeed * multiplier;

        yield return new WaitForSeconds(duration);

        movementSpeed = originalSpeed;

        slowCoroutine = null;
    }
}
