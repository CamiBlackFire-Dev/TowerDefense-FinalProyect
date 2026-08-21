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
    public event System.Action<TestEnemyMovement> Finished;

    [Header("Audio")]
    // En bucle mientras el enemigo esta activo (caminando, o retrocediendo
    // por Repulsion). Se detiene sola al morir o al llegar al final, porque
    // las dos cosas apagan este componente (ver EnemyHealth.Die y el
    // "enabled = false" de mas abajo) y eso dispara OnDisable.
    [SerializeField] private AudioClip footstepsSound;
    [SerializeField] [Range(0f, 1f)] private float footstepsVolume = 0.35f;
    private AudioSource footstepsSource;

    private void Start()
    {
        if (path != null)
            waypoints = path.GetWaypoints();

        originalSpeed = movementSpeed;
    }

    private void OnEnable()
    {
        if (footstepsSound == null)
            return;

        if (footstepsSource == null)
        {
            footstepsSource = gameObject.AddComponent<AudioSource>();
            footstepsSource.clip = footstepsSound;
            footstepsSource.loop = true;
            footstepsSource.playOnAwake = false;
            footstepsSource.spatialBlend = 1f;
            footstepsSource.volume = footstepsVolume;
            // Un pitch fijo por enemigo evita que sonoros identicos que
            // arrancan juntos se escuchen como un solo golpe de volumen.
            footstepsSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
        }

        footstepsSource.Play();
    }

    private void OnDisable()
    {
        if (footstepsSource != null)
            footstepsSource.Stop();
    }

    public void SetPath(Path newPath)
    {
        path = newPath;
        waypoints = path != null ? path.GetWaypoints() : null;
        currentWaypoint = 0;
    }

    // Desde que waypoint arranca. Lo usa el spawner cuando los enemigos
    // salen de un punto intermedio del camino: sin esto seguirian yendo
    // primero al waypoint 0, es decir caminando hacia atras.
    public void SetStartWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        currentWaypoint = Mathf.Clamp(index, 0, waypoints.Length - 1);
    }

    // El spawner deja aqui la velocidad de esta oleada. Se guarda tambien
    // como originalSpeed para que un EMP posterior recupere esta velocidad
    // (y no la que traiga el prefab por defecto) al terminar el slow.
    public void SetMovementSpeed(float speed)
    {
        movementSpeed = speed;
        originalSpeed = speed;
    }

    private void Update()
    {
        if (isReversing)
            return;

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

        slowCoroutine = StartCoroutine(SlowCoroutine(multiplier, duration));
    }

    private IEnumerator SlowCoroutine(float multiplier, float duration)
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

        reverseCoroutine = StartCoroutine(ReversePathCoroutine(duration));
    }

    private IEnumerator ReversePathCoroutine(float duration)
    {
        isReversing = true;

        float timer = 0f;

        while (timer < duration)
        {
            if (currentWaypoint <= 0)
                break;

            Transform previousWaypoint = waypoints[currentWaypoint - 1];

            transform.position = Vector3.MoveTowards(transform.position, previousWaypoint.position, movementSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, previousWaypoint.position) <= 0.1f)
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
