using System;
using UnityEngine;
using DamageNumbersPro;

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
    // Numero flotante de oro al morir (opcional). Ej: Assets/_Custom/Prefabs/VFX/GoldNumber.prefab
    public DamageNumber goldPopupPrefab;

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
    public float enemySpeed = 2f;   // unidades por segundo (TestEnemyMovement.SetMovementSpeed)

    [Header("Enemigos que disparan")]
    // Que parte de los enemigos sale armada: 0 = ninguno, 1 = todos.
    // Los que disparan NO se detienen, siguen caminando hacia el castillo
    // y van tirandole a las torres que les quedan a tiro.
    [Range(0f, 1f)] public float shooterChance = 0.35f;
    // Alcance del enemigo, en las mismas unidades que el de las torres
    // (las torres van de 4 a 7 segun el nivel).
    public float shooterRange = 5f;
    public float shooterDamage = 4f;
    public float shooterAttackRate = 0.5f;  // disparos por segundo
    // Bala visible (obligatoria: EnemyAttack no dispara sin un prefab asignado).
    public GameObject shooterProjectilePrefab;
    public float shooterProjectileSpeed = 8f;
    public DamageNumber shooterPopupPrefab;  // numero de dano sobre la torre
    // El efecto de impacto ahora es un campo propio de EnemyProjectile
    // (del prefab de la bala), no algo que el spawner pueda variar por
    // oleada: se configura una sola vez en shooterProjectilePrefab.

    private float _timer;
    private int _spawned;
    private int _aliveCount;
    private bool _running;
    private int _waveNumber;

    // Avisos para la interfaz: el HUD se sincroniza con estos.
    public event Action WaveStarted;
    // Se dispara solo cuando ya no queda nadie vivo de la oleada (todos
    // muertos o escapados), no apenas cuando terminan de salir.
    public event Action WaveFinished;
    // Un enemigo llego al final del camino (le pego a la base). Lo usa el
    // feedback visual del castillo, separado de PlayerBase.LivesChanged
    // para no depender de que "perder vidas" siempre signifique esto.
    public event Action EnemyReachedEnd;

    // Numero de la ultima oleada arrancada (1, 2, 3...).
    public int WaveNumber
    {
        get { return _waveNumber; }
    }

    // True mientras la oleada sigue en curso: quedan enemigos por salir
    // o enemigos vivos de esta tanda. La siguiente oleada no puede
    // arrancar (el boton del HUD sigue oculto) hasta que esto sea false.
    public bool IsRunning
    {
        get { return _running; }
    }

    // Enemigos que ya salieron en esta oleada.
    public int SpawnedCount
    {
        get { return _spawned; }
    }

    // Enemigos de esta oleada que siguen vivos (ni muertos ni escapados).
    public int AliveCount
    {
        get { return _aliveCount; }
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

        // PlayerBase.Instance resuelve o crea el mismo PlayerBase que usa
        // el HUD, para que ambos escuchen siempre la misma instancia.
        if (playerBase == null)
            playerBase = PlayerBase.Instance;
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
        _aliveCount = 0;
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

        bool doneSpawning = enemiesPerWave > 0 && _spawned >= enemiesPerWave;

        // Mientras falten enemigos por sacar, el reloj de spawn manda.
        if (!doneSpawning)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return;

            _timer = spawnInterval;
            SpawnEnemy();
            doneSpawning = enemiesPerWave > 0 && _spawned >= enemiesPerWave;
        }

        // Oleada completa solo cuando ya salieron todos y ademas no queda
        // ninguno vivo (muerto o escapado). Hasta entonces no se avisa al
        // HUD, asi que el boton de la siguiente oleada sigue oculto.
        if (doneSpawning && _aliveCount <= 0)
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
        _aliveCount++;

        // Recorrido: el enemigo sigue los waypoints que armo PathBuilder.
        TestEnemyMovement movement = enemy.GetComponent<TestEnemyMovement>();
        if (movement != null)
        {
            movement.SetPath(path);
            movement.SetMovementSpeed(enemySpeed);
            movement.Finished += HandleEnemyFinished;
        }

        // Vida: si el prefab no la trae, se le agrega aqui.
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
            health = enemy.AddComponent<EnemyHealth>();

        health.Setup(enemyHealth, enemyReward, economy, goldPopupPrefab);
        health.Died += HandleEnemyDied;

        ConfigureShooter(enemy);

        return enemy;
    }

    // Decide si este enemigo sale armado y, si le toca, le deja listo el
    // ataque a torres. El movimiento no se toca: los que disparan siguen
    // avanzando igual, solo que van tirandole a lo que tengan a tiro.
    private void ConfigureShooter(GameObject enemy)
    {
        EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
        EnemyTargetDetector detector = enemy.GetComponent<EnemyTargetDetector>();

        // A este no le toco disparar. Algunos prefabs ya traen el ataque
        // puesto, asi que se apaga en vez de borrarlo (EnemyAttack pide un
        // EnemyTargetDetector, y quitarlos en el orden equivocado da error).
        if (UnityEngine.Random.value >= shooterChance)
        {
            if (attack != null)
                attack.enabled = false;
            if (detector != null)
                detector.enabled = false;
            return;
        }

        // Al agregar EnemyAttack, Unity trae solo el EnemyTargetDetector
        // que pide con RequireComponent.
        if (attack == null)
            attack = enemy.AddComponent<EnemyAttack>();
        attack.enabled = true;

        if (detector == null)
            detector = enemy.GetComponent<EnemyTargetDetector>();

        if (detector != null)
        {
            detector.enabled = true;
            detector.SetDetectionRange(shooterRange);

            int towerLayer = LayerMask.NameToLayer("Tower");
            if (towerLayer >= 0)
                detector.SetTowerLayer(1 << towerLayer);
        }

        attack.Setup(shooterDamage, shooterAttackRate,
            shooterProjectilePrefab, shooterProjectileSpeed,
            shooterPopupPrefab);
    }

    // Cuando un enemigo llega al final le quita vidas al jugador y desaparece.
    private void HandleEnemyFinished(TestEnemyMovement movement)
    {
        movement.Finished -= HandleEnemyFinished;

        if (playerBase != null)
            playerBase.TakeDamage(damagePerEnemy);

        if (EnemyReachedEnd != null)
            EnemyReachedEnd();

        _aliveCount = Mathf.Max(0, _aliveCount - 1);
        Destroy(movement.gameObject);
    }

    // Una torre elimino al enemigo: ya no cuenta como vivo para la oleada.
    private void HandleEnemyDied(EnemyHealth health)
    {
        health.Died -= HandleEnemyDied;
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }
}
