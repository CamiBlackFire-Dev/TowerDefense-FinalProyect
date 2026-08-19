using System.Collections;
using UnityEngine;

public class TestEnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private Path path;

    private Transform[] waypoints;
    private int currentWaypoint = 0;

    [Header("EMP")]
    private Coroutine slowCoroutine;
    private float originalSpeed;

    [Header("Repulsion")]
    private Coroutine reverseCoroutine;
    private bool isReversing;

    [Header("Attack")]
    [SerializeField] private EnemyAttack enemyAttack;

    // Aviso de que el enemigo llegó al final del recorrido.
    public event System.Action<TestEnemyMovement> Finished;

    private void Start()
    {
        if (path != null)
            waypoints = path.GetWaypoints();

        originalSpeed = movementSpeed;
    }

    // El spawner le pasa el camino recién creado el enemigo.
    public void SetPath(Path newPath)
    {
        path = newPath;
        waypoints = path != null ? path.GetWaypoints() : null;
        currentWaypoint = 0;
    }

    private void Update()
    {
        // Si el enemigo está dentro del rango de una torre,
        // se queda quieto para que EnemyAttack pueda atacar.
        /*if (enemyAttack != null && enemyAttack.HasTarget())
        {
            return;
        }*/

        if (isReversing)
            return;

        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentWaypoint];

        Vector3 direction = target.position - transform.position;

        transform.position +=
            direction.normalized *
            movementSpeed *
            Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                Debug.Log("El enemigo llegó al final del Path");

                enabled = false;

                if (Finished != null)
                    Finished(this);
            }
        }
    }

    // EMP Ability

    public void ApplySlow(float multiplier, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        slowCoroutine =
            StartCoroutine(
                SlowCoroutine(multiplier, duration)
            );
    }

    private IEnumerator SlowCoroutine(
        float multiplier,
        float duration)
    {
        movementSpeed = originalSpeed * multiplier;

        yield return new WaitForSeconds(duration);

        movementSpeed = originalSpeed;

        slowCoroutine = null;
    }

    // Repulsion Ability

    public void ReversePath(float duration)
    {
        if (reverseCoroutine != null)
        {
            StopCoroutine(reverseCoroutine);
        }

        reverseCoroutine =
            StartCoroutine(
                ReversePathCoroutine(duration)
            );
    }

    private IEnumerator ReversePathCoroutine(
        float duration)
    {
        isReversing = true;

        float timer = 0f;

        while (timer < duration)
        {
            if (currentWaypoint <= 0)
                break;

            Transform previousWaypoint =
                waypoints[currentWaypoint - 1];

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    previousWaypoint.position,
                    movementSpeed * Time.deltaTime
                );

            if (Vector3.Distance(
                transform.position,
                previousWaypoint.position) <= 0.1f)
            {
                currentWaypoint--;
            }

            timer += Time.deltaTime;

            yield return null;
        }

        isReversing = false;

        reverseCoroutine = null;
    }
}
