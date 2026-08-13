using System;
using UnityEngine;

// Saca enemigos por el inicio del camino cada cierto tiempo.
// A cada enemigo le pasa el camino, su vida y quien le paga al jugador,
// asi el prefab del enemigo no necesita saber nada de la escena.
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemigos")]
    // Prefabs de los enemigos. En cada spawn sale uno al azar de la lista.
    // Los crea el menu Tower Defense > Crear prefabs de enemigos.
    public GameObject[] enemyPrefabs;
    // Los tres de abajo se buscan solos si se dejan vacios.
    public PathBuilder pathBuilder; // camino que van a recorrer
    public EconomyManager economy;  // paga al jugador cuando mueren
    public PlayerBase playerBase;   // pierde vidas si un enemigo llega al final

    [Header("Dano al escaparse")]
    public int damagePerEnemy = 1;  // vidas que quita cada enemigo que llega al final

    [Header("Oleada")]
    public bool spawnOnStart = false; // false: la oleada la arranca el boton del HUD
    public float startDelay = 2f;    // espera antes del primer enemigo
    public float spawnInterval = 2f; // segundos entre un enemigo y el siguiente
    public int enemiesPerWave = 8;   // cuantos salen (0 = sin parar)

    [Header("Vida de los enemigos")]
    public float enemyHealth = 20f;
    public int enemyReward = 10;

    private float _timer;
    private int _spawned;
    private bool _running;
    private int _waveNumber;

    // Avisos para la interfaz: el HUD se sincroniza con estos.
    public event Action WaveStarted;
    public event Action WaveFinished;

    // Numero de la ultima oleada arrancada (1, 2, 3...).
    public int WaveNumber
    {
        get { return _waveNumber; }
    }

    // True mientras la oleada esta sacando enemigos.
    public bool IsRunning
    {
        get { return _running; }
    }

    // Enemigos que ya salieron en esta oleada.
    public int SpawnedCount
    {
        get { return _spawned; }
    }

    private void Start()
    {
        EnsureReferences();

        if (spawnOnStart)
            StartWave();
    }

    // Busca en la escena lo que no se haya arrastrado a mano.
    private void EnsureReferences()
    {
        if (pathBuilder == null)
            pathBuilder = GetComponent<PathBuilder>();
        if (pathBuilder == null)
            pathBuilder = FindFirstObjectByType<PathBuilder>();

        if (economy == null)
            economy = FindFirstObjectByType<EconomyManager>();

        if (playerBase == null)
            playerBase = FindFirstObjectByType<PlayerBase>();

        // Si nadie puso las vidas del jugador, se crean aqui mismo.
        if (playerBase == null)
        {
            playerBase = gameObject.AddComponent<PlayerBase>();
            Debug.Log("EnemySpawner: no habia PlayerBase en la escena, se creo uno.", this);
        }
    }

    // Empieza una oleada nueva desde cero.
    public void StartWave()
    {
        EnsureReferences();

        if (!HasEnemies() || pathBuilder == null)
        {
            Debug.LogWarning("EnemySpawner: faltan los prefabs de enemigos o el camino.", this);
            return;
        }

        _spawned = 0;
        _timer = startDelay;
        _running = true;
        _waveNumber++;

        if (WaveStarted != null)
            WaveStarted();
    }

    public void StopWave()
    {
        if (!_running)
            return;

        _running = false;
        if (WaveFinished != null)
            WaveFinished();
    }

    private void Update()
    {
        if (!_running || !Application.isPlaying)
            return;

        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;

        _timer = spawnInterval;
        SpawnEnemy();

        // Oleada completa: se avisa para que la interfaz muestre el boton.
        if (enemiesPerWave > 0 && _spawned >= enemiesPerWave)
        {
            _running = false;
            if (WaveFinished != null)
                WaveFinished();
        }
    }

    // Indica si hay al menos un prefab de enemigo en la lista.
    public bool HasEnemies()
    {
        if (enemyPrefabs == null)
            return false;

        for (int i = 0; i < enemyPrefabs.Length; i++)
            if (enemyPrefabs[i] != null)
                return true;

        return false;
    }

    // Elige al azar uno de los enemigos de la lista.
    public GameObject PickEnemyPrefab()
    {
        if (!HasEnemies())
            return null;

        // Se repite hasta encontrar uno que no este vacio.
        for (int intento = 0; intento < 10; intento++)
        {
            GameObject elegido = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
            if (elegido != null)
                return elegido;
        }

        return null;
    }

    // Crea un enemigo en la primera casilla del camino y lo deja listo.
    public GameObject SpawnEnemy()
    {
        GameObject prefab = PickEnemyPrefab();
        if (prefab == null || pathBuilder == null)
            return null;

        Path path = pathBuilder.Path;
        Transform[] waypoints = path.GetWaypoints();
        if (waypoints == null || waypoints.Length == 0)
            return null;

        GameObject enemy = Instantiate(prefab, waypoints[0].position, waypoints[0].rotation);
        enemy.name = "Enemy " + _spawned.ToString("00");
        _spawned++;

        // Recorrido: el enemigo sigue los waypoints que armo PathBuilder.
        TestEnemyMovement movement = enemy.GetComponent<TestEnemyMovement>();
        if (movement != null)
        {
            movement.SetPath(path);
            movement.Finished += HandleEnemyFinished;
        }

        // Vida: si el prefab no la trae, se le agrega aqui.
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
            health = enemy.AddComponent<EnemyHealth>();

        health.Setup(enemyHealth, enemyReward, economy);

        return enemy;
    }

    // Cuando un enemigo llega al final le quita vidas al jugador y desaparece.
    private void HandleEnemyFinished(TestEnemyMovement movement)
    {
        movement.Finished -= HandleEnemyFinished;

        if (playerBase != null)
            playerBase.TakeDamage(damagePerEnemy);

        Destroy(movement.gameObject);
    }
}
