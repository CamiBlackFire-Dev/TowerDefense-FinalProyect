using System;
using UnityEngine;
using DamageNumbersPro;

// Una salida de enemigos: por donde aparecen y con que ritmo y dureza.
// Un nivel puede tener varias y cada una va por su cuenta (su propio
// camino, su propio reloj y su propia cuenta de enemigos).
[Serializable]
public class SpawnSource
{
    public string label = "Salida";
    public bool active = true;

    [Header("Por donde salen")]
    // Camino propio de esta salida. Vacio = el camino principal del spawner.
    public PathBuilder pathBuilder;
    // Desde que waypoint de ese camino arrancan.
    public int startWaypointIndex = 0;

    [Header("Ritmo")]
    // Cuantos manda esta salida por oleada y cada cuanto. En 0 usan lo que
    // diga el spawner, para no tener que repetir el mismo numero en todas.
    public int enemiesPerWave = 0;
    // Enemigos de mas por oleada, propio de esta salida (igual que
    // enemiesGrowthPerWave pero por salida en vez de general). Solo se
    // aplica si enemiesPerWave es > 0: una salida en 0 ya crece con el
    // general (GetEnemiesForWave), asi que esto seria redundante.
    public float enemiesGrowthPerWave = 0f;
    public float spawnInterval = 0f;
    // Espera propia antes del primer enemigo: sirve para escalonar las
    // salidas y que no lleguen todas a la vez.
    public float startDelay = 0f;

    [Header("Dureza")]
    // Multiplican lo que ya calculo el spawner para esa oleada, asi una
    // salida puede ser la "dificil" sin tener que reconfigurar todo.
    public float healthMultiplier = 1f;
    public float speedMultiplier = 1f;
}

// Un tipo de enemigo disponible para el spawner, con desde/hasta que oleada
// puede salir. Sirve para ir metiendo enemigos nuevos a medida que el juego
// avanza (ej: el esqueleto volador recien desde la oleada 6) sin tener que
// sacar los viejos de la lista ni tocar los niveles que ya estaban armados.
[Serializable]
public class EnemyTypeEntry
{
    // Solo para reconocerlo en el inspector, no lo usa el juego.
    public string label = "Enemigo";
    public GameObject prefab;
    // Que tan seguido sale frente a los demas tipos activos en esa oleada.
    // Con todos en el mismo numero, todos tienen la misma chance.
    [Min(0f)] public float weight = 1f;
    // Desde que oleada puede aparecer (1 = desde la primera).
    [Min(1)] public int minWave = 1;
    // Hasta que oleada aparece. 0 = sin limite, sigue saliendo siempre.
    [Min(0)] public int maxWave = 0;
}

// Saca enemigos por el inicio del camino cada cierto tiempo.
// A cada enemigo le pasa el camino, su vida y quien le paga al jugador,
// asi el prefab del enemigo no necesita saber nada de la escena.
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemigos")]
    // Prefabs de los enemigos. En cada spawn sale uno al azar de la lista.
    // Los crea el menu Tower Defense > Crear prefabs de enemigos.
    // Se sigue usando tal cual si enemyTypes (abajo) esta vacio.
    public GameObject[] enemyPrefabs;
    // Que enemigos pueden salir y desde/hasta que oleada. Vacio = se cae al
    // comportamiento de siempre (enemyPrefabs, mismo peso, cualquier
    // oleada). Con entradas aca cada tipo elige su propia ventana de
    // oleadas, para ir sumando enemigos nuevos sin tocar los que ya estaban.
    public EnemyTypeEntry[] enemyTypes;
    // Los cuatro de abajo se buscan solos si se dejan vacios.
    public PathBuilder pathBuilder; // camino que van a recorrer
    public EconomyManager economy;  // paga al jugador cuando mueren
    public PlayerBase playerBase;   // pierde vidas si un enemigo llega al final
    // Donde flota el numero del bono de oleada (ver waveSurvivalGold): se
    // busca el objeto con CastleDamageFeedback, que vive en el castillo.
    public Transform castleTransform;
    // Numero flotante de oro al morir (opcional). Ej: Assets/_Custom/Prefabs/VFX/GoldNumber.prefab
    // Se reutiliza tambien para el bono de oleada completa.
    public DamageNumber goldPopupPrefab;

    [Header("Dano al escaparse")]
    public int damagePerEnemy = 1;  // vidas que quita cada enemigo que llega al final

    [Header("Oleada")]
    public bool spawnOnStart = false; // false: la oleada la arranca el boton del HUD
    public float startDelay = 2f;    // espera antes del primer enemigo
    public float spawnInterval = 2f; // segundos entre un enemigo y el siguiente
    public int enemiesPerWave = 8;   // cuantos salen (0 = sin parar)
    // Desde que waypoint del camino salen. 0 = el principio de siempre.
    // Subirlo los hace aparecer mas cerca del castillo, que sirve para
    // probar el final del recorrido sin esperar todo el trayecto.
    // Solo se usa cuando no hay salidas multiples configuradas.
    public int spawnWaypointIndex = 0;

    [Header("Salidas multiples")]
    // Vacio = una sola salida, la de siempre (spawnWaypointIndex).
    // Con salidas aqui, cada una manda sus enemigos por su cuenta y los
    // campos de arriba pasan a ser los valores por defecto de todas.
    public SpawnSource[] spawnSources;
    // Oro garantizado al terminar una oleada, sin importar cuantos enemigos
    // mataron las torres. Sin esto, si te quedas sin torres y sin la bomba
    // (la unica forma de matar enemigos sin torres) el oro deja de entrar
    // por completo y la partida queda sin salida.
    public int waveSurvivalGold = 50;

    [Header("Economia")]
    // Config global de oro. Si esta puesta, de ahi salen enemyReward y
    // waveSurvivalGold, para que todos los niveles paguen igual.
    public GameEconomyConfig economyConfig;

    // Oro que paga cada enemigo y bono por terminar la oleada, ya
    // resueltos contra la config global.
    public int EnemyReward
    {
        get { return economyConfig != null ? economyConfig.enemyReward : enemyReward; }
    }

    public int WaveSurvivalGold
    {
        get { return economyConfig != null ? economyConfig.waveSurvivalGold : waveSurvivalGold; }
    }

    [Header("Vida de los enemigos")]
    public float enemyHealth = 20f;
    public int enemyReward = 10;
    public float enemySpeed = 2f;   // unidades por segundo (TestEnemyMovement.SetMovementSpeed)

    [Header("Jefe")]
    // Prefab del jefe (el golem). Sin prefab no sale ninguno.
    public GameObject bossPrefab;
    // En que oleada aparece. 0 = nunca. Sale uno solo, como remate de esa
    // oleada: recien cuando ya salieron todos los enemigos normales de
    // ella (no al empezar). En una oleada sin fin (enemiesPerWave/las
    // salidas en 0) nunca llega a "terminar de salir", asi que el jefe
    // tampoco saldria: bossWave necesita una oleada con cantidad definida.
    public int bossWave = 10;
    public float bossHealth = 400f;
    public int bossReward = 200;
    public float bossSpeed = 1.2f;
    // Por que salida entra. Si el nivel tiene salidas multiples es el
    // indice dentro de esa lista; si no, se usa el camino principal.
    public int bossSourceIndex = 0;

    [Header("Aumento por oleada")]
    // Cuanto se endurece cada oleada respecto de la primera. El crecimiento
    // es lineal y no compuesto (oleada 3 con 0.2 = +40%, no +44%): asi el
    // numero se puede calcular de cabeza y no se dispara en oleadas altas.
    // Todo en 0 deja el juego como estaba: todas las oleadas iguales.
    [Range(0f, 2f)] public float healthGrowthPerWave = 0.15f;
    [Range(0f, 2f)] public float speedGrowthPerWave = 0.05f;
    [Range(0f, 2f)] public float shooterDamageGrowthPerWave = 0.1f;
    // Este es a secas: enemigos extra por oleada, no un porcentaje.
    public int enemiesGrowthPerWave = 2;
    // Que parte de los enemigos sale armada, sumado por oleada (tope 1).
    [Range(0f, 1f)] public float shooterChanceGrowthPerWave = 0.05f;

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
    // Reloj y cuenta de cada salida, en el mismo orden que spawnSources.
    // Se rehacen al empezar cada oleada.
    private float[] _sourceTimers;
    private int[] _sourceSpawned;
    // Evita sacar el jefe mas de una vez por oleada. Sin este flag,
    // SpawnBossIfDue se llamaria en TODOS los Update() posteriores a que
    // terminen de salir los enemigos normales (esa condicion se queda en
    // true el resto de la oleada), y sacaria un jefe nuevo en cada frame.
    private bool _bossSpawnedThisWave;

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

        if (castleTransform == null)
        {
            CastleDamageFeedback castle = FindFirstObjectByType<CastleDamageFeedback>();
            if (castle != null)
                castleTransform = castle.transform;
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
        _aliveCount = 0;
        _timer = startDelay;
        _running = true;
        _waveNumber++;

        ResetSourceState();
        _bossSpawnedThisWave = false;

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

        bool doneSpawning;
        if (HasSpawnSources())
        {
            doneSpawning = UpdateSpawnSources();
        }
        else
        {
            int waveEnemies = GetEnemiesForWave(_waveNumber);
            doneSpawning = waveEnemies > 0 && _spawned >= waveEnemies;

            // Mientras falten enemigos por sacar, el reloj de spawn manda.
            if (!doneSpawning)
            {
                _timer -= Time.deltaTime;
                if (_timer > 0f)
                    return;

                _timer = spawnInterval;
                SpawnEnemy();
                doneSpawning = waveEnemies > 0 && _spawned >= waveEnemies;
            }
        }

        // El jefe sale como remate de su oleada: recien cuando ya no queda
        // ningun enemigo normal por salir, no al arrancarla. Al spawnearlo
        // aca (antes del chequeo de abajo) _aliveCount ya lo cuenta a el,
        // asi que la oleada no se cierra sola en el mismo frame.
        if (doneSpawning && !_bossSpawnedThisWave)
        {
            _bossSpawnedThisWave = true;
            SpawnBossIfDue();
        }

        // Oleada completa solo cuando ya salieron todos y ademas no queda
        // ninguno vivo (muerto o escapado). Hasta entonces no se avisa al
        // HUD, asi que el boton de la siguiente oleada sigue oculto.
        if (doneSpawning && _aliveCount <= 0)
        {
            _running = false;

            if (economy != null)
            {
                economy.AddCurrency(WaveSurvivalGold);

                if (goldPopupPrefab != null && WaveSurvivalGold > 0 && castleTransform != null)
                    goldPopupPrefab.Spawn(castleTransform.position, WaveSurvivalGold);
            }

            if (WaveFinished != null)
                WaveFinished();
        }
    }

    // --- Valores efectivos de una oleada ---
    // Se calculan al vuelo (no se congelan al empezar la oleada) para que
    // tocar un numero desde la ventana de debug se note en el siguiente
    // enemigo que salga, sin tener que reiniciar la oleada.
    // Son publicos y reciben el numero de oleada para poder pedirles "como
    // seria la oleada 7" desde el editor sin llegar a jugarla.

    // Multiplicador lineal: oleada 1 = x1, oleada 2 = x(1+g), etc.
    private static float GrowthFactor(float growthPerWave, int waveNumber)
    {
        int wavesPassed = Mathf.Max(0, waveNumber - 1);
        return 1f + growthPerWave * wavesPassed;
    }

    public float GetEnemyHealthForWave(int waveNumber)
    {
        return enemyHealth * GrowthFactor(healthGrowthPerWave, waveNumber);
    }

    public float GetEnemySpeedForWave(int waveNumber)
    {
        return enemySpeed * GrowthFactor(speedGrowthPerWave, waveNumber);
    }

    public float GetShooterDamageForWave(int waveNumber)
    {
        return shooterDamage * GrowthFactor(shooterDamageGrowthPerWave, waveNumber);
    }

    public int GetEnemiesForWave(int waveNumber)
    {
        // Con enemiesPerWave en 0 la oleada es infinita: el crecimiento no
        // aplica, seguiria siendo infinita igual.
        if (enemiesPerWave <= 0)
            return 0;

        int wavesPassed = Mathf.Max(0, waveNumber - 1);
        return Mathf.Max(1, enemiesPerWave + enemiesGrowthPerWave * wavesPassed);
    }

    public float GetShooterChanceForWave(int waveNumber)
    {
        int wavesPassed = Mathf.Max(0, waveNumber - 1);
        return Mathf.Clamp01(shooterChance + shooterChanceGrowthPerWave * wavesPassed);
    }

    // --- Jefe ---

    // Saca el jefe si a esta oleada le toca. Va aparte del reloj de las
    // salidas normales: sale uno solo y de una vez, al empezar la oleada.
    private void SpawnBossIfDue()
    {
        if (bossPrefab == null || bossWave <= 0 || _waveNumber != bossWave)
            return;

        GameObject boss = SpawnBoss();
        if (boss == null)
            Debug.LogWarning("EnemySpawner: no se pudo sacar el jefe (falta camino?).", this);
    }

    // Crea el jefe en su salida. Publico para poder probarlo desde la
    // ventana de debug sin esperar a que llegue su oleada.
    public GameObject SpawnBoss()
    {
        if (bossPrefab == null)
            return null;

        Path path = pathBuilder != null ? pathBuilder.Path : null;
        int waypointIndex = spawnWaypointIndex;

        // Con salidas multiples entra por la que se haya elegido.
        if (HasSpawnSources() && spawnSources != null && spawnSources.Length > 0)
        {
            int index = Mathf.Clamp(bossSourceIndex, 0, spawnSources.Length - 1);
            SpawnSource source = spawnSources[index];
            if (source != null)
            {
                PathBuilder builder = source.pathBuilder != null ? source.pathBuilder : pathBuilder;
                if (builder != null)
                    path = builder.Path;
                waypointIndex = source.startWaypointIndex;
            }
        }

        if (path == null)
            return null;

        GameObject boss = CreateEnemy(bossPrefab, path, waypointIndex, bossHealth, bossReward, bossSpeed);
        if (boss == null)
            return null;

        boss.name = "Jefe (oleada " + _waveNumber + ")";

        // Sin BossEnemy se comportaria como un enemigo normal y
        // desapareceria al llegar al castillo.
        if (boss.GetComponent<BossEnemy>() == null)
            boss.AddComponent<BossEnemy>();

        return boss;
    }

    // --- Salidas multiples ---

    // True si el nivel tiene salidas configuradas y alguna esta activa.
    public bool HasSpawnSources()
    {
        if (spawnSources == null)
            return false;

        for (int i = 0; i < spawnSources.Length; i++)
            if (spawnSources[i] != null && spawnSources[i].active)
                return true;

        return false;
    }

    // Cuantos manda esta salida en la oleada indicada. En 0 hereda el
    // total del spawner, para no repetir el mismo numero en cada salida.
    public int GetEnemiesForSource(SpawnSource source, int waveNumber)
    {
        if (source == null)
            return 0;

        if (source.enemiesPerWave > 0)
        {
            int wavesPassed = Mathf.Max(0, waveNumber - 1);
            return Mathf.Max(1, Mathf.RoundToInt(source.enemiesPerWave + source.enemiesGrowthPerWave * wavesPassed));
        }

        return GetEnemiesForWave(waveNumber);
    }

    private void ResetSourceState()
    {
        if (spawnSources == null || spawnSources.Length == 0)
        {
            _sourceTimers = null;
            _sourceSpawned = null;
            return;
        }

        _sourceTimers = new float[spawnSources.Length];
        _sourceSpawned = new int[spawnSources.Length];

        for (int i = 0; i < spawnSources.Length; i++)
        {
            SpawnSource source = spawnSources[i];
            // Cada salida arranca con su propia espera, sumada a la general.
            _sourceTimers[i] = startDelay + (source != null ? Mathf.Max(0f, source.startDelay) : 0f);
        }
    }

    // Adelanta el reloj de cada salida activa. Devuelve true cuando todas
    // terminaron de mandar lo suyo.
    private bool UpdateSpawnSources()
    {
        // La oleada puede haber empezado antes de configurar las salidas.
        if (_sourceTimers == null || _sourceTimers.Length != spawnSources.Length)
            ResetSourceState();

        bool allDone = true;

        for (int i = 0; i < spawnSources.Length; i++)
        {
            SpawnSource source = spawnSources[i];
            if (source == null || !source.active)
                continue;

            int target = GetEnemiesForSource(source, _waveNumber);

            // target 0 = esta salida no para nunca, asi que la oleada
            // tampoco puede darse por terminada.
            if (target <= 0)
            {
                allDone = false;
            }
            else if (_sourceSpawned[i] >= target)
            {
                continue;   // esta ya termino
            }
            else
            {
                allDone = false;
            }

            _sourceTimers[i] -= Time.deltaTime;
            if (_sourceTimers[i] > 0f)
                continue;

            float interval = source.spawnInterval > 0f ? source.spawnInterval : spawnInterval;
            _sourceTimers[i] = Mathf.Max(0.01f, interval);

            if (SpawnFromSource(source) != null)
                _sourceSpawned[i]++;
        }

        return allDone;
    }

    // Crea un enemigo en una salida concreta, con su camino y su dureza.
    public GameObject SpawnFromSource(SpawnSource source)
    {
        if (source == null)
            return null;

        PathBuilder builder = source.pathBuilder != null ? source.pathBuilder : pathBuilder;
        if (builder == null)
            return null;

        return SpawnEnemyInternal(
            builder.Path,
            source.startWaypointIndex,
            source.healthMultiplier,
            source.speedMultiplier);
    }

    // Indica si hay al menos un prefab de enemigo utilizable, en enemyTypes
    // o en la lista plana de siempre.
    public bool HasEnemies()
    {
        if (enemyTypes != null)
            for (int i = 0; i < enemyTypes.Length; i++)
                if (enemyTypes[i] != null && enemyTypes[i].prefab != null)
                    return true;

        if (enemyPrefabs != null)
            for (int i = 0; i < enemyPrefabs.Length; i++)
                if (enemyPrefabs[i] != null)
                    return true;

        return false;
    }

    // True si esta entrada puede salir en esa oleada: tiene prefab, algo de
    // peso, y la oleada cae dentro de su ventana [minWave, maxWave] (0 = sin
    // limite superior).
    private static bool IsEnemyTypeActive(EnemyTypeEntry entry, int waveNumber)
    {
        if (entry == null || entry.prefab == null || entry.weight <= 0f)
            return false;

        if (waveNumber < Mathf.Max(1, entry.minWave))
            return false;

        if (entry.maxWave > 0 && waveNumber > entry.maxWave)
            return false;

        return true;
    }

    // Elige un enemigo para esta oleada. Si enemyTypes tiene algo valido
    // para ella, sortea entre esos por peso; si no (nivel viejo sin tocar,
    // o ninguna entrada activa todavia para esta oleada), cae a la lista
    // plana de siempre con el mismo peso para todos.
    public GameObject PickEnemyPrefab(int waveNumber)
    {
        GameObject fromTypes = PickFromEnemyTypes(waveNumber);
        if (fromTypes != null)
            return fromTypes;

        return PickFromEnemyPrefabs();
    }

    private GameObject PickFromEnemyTypes(int waveNumber)
    {
        if (enemyTypes == null || enemyTypes.Length == 0)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < enemyTypes.Length; i++)
            if (IsEnemyTypeActive(enemyTypes[i], waveNumber))
                totalWeight += enemyTypes[i].weight;

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.value * totalWeight;
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (!IsEnemyTypeActive(enemyTypes[i], waveNumber))
                continue;

            roll -= enemyTypes[i].weight;
            if (roll <= 0f)
                return enemyTypes[i].prefab;
        }

        // Redondeo de punto flotante: si nadie lo agarro, se devuelve el
        // ultimo activo en vez de caer a enemyPrefabs sin necesidad.
        for (int i = enemyTypes.Length - 1; i >= 0; i--)
            if (IsEnemyTypeActive(enemyTypes[i], waveNumber))
                return enemyTypes[i].prefab;

        return null;
    }

    // El comportamiento de siempre: cualquiera de la lista plana, mismo
    // peso, cualquier oleada. Sigue en pie para los niveles que no usan
    // enemyTypes.
    private GameObject PickFromEnemyPrefabs()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
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

    // Etiquetas de los tipos que pueden salir en esa oleada, separadas por
    // coma. Solo para mostrarlo en Game Debug; el juego no lo usa.
    public string DescribeActiveEnemyTypes(int waveNumber)
    {
        if (enemyTypes == null || enemyTypes.Length == 0)
            return "(cualquiera de Enemigos)";

        string result = "";
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (!IsEnemyTypeActive(enemyTypes[i], waveNumber))
                continue;

            string label = string.IsNullOrEmpty(enemyTypes[i].label) ? "?" : enemyTypes[i].label;
            result += (result.Length > 0 ? ", " : "") + label;
        }

        return result.Length > 0 ? result : "(ninguno activo aun)";
    }

    // Crea un enemigo en el punto de salida configurado y lo deja listo.
    public GameObject SpawnEnemy()
    {
        if (pathBuilder == null)
            return null;

        return SpawnEnemyInternal(pathBuilder.Path, spawnWaypointIndex, 1f, 1f);
    }

    // Parte comun de crear un enemigo, la use la salida unica de siempre o
    // una de las salidas multiples. Los multiplicadores son de la salida:
    // en 1 el enemigo sale con los valores que ya toquen para esa oleada.
    private GameObject SpawnEnemyInternal(Path path, int waypointIndex,
        float healthMultiplier, float speedMultiplier)
    {
        GameObject prefab = PickEnemyPrefab(_waveNumber);
        if (prefab == null)
            return null;

        GameObject enemy = CreateEnemy(prefab, path, waypointIndex,
            GetEnemyHealthForWave(_waveNumber) * healthMultiplier,
            EnemyReward,
            GetEnemySpeedForWave(_waveNumber) * speedMultiplier);

        if (enemy == null)
            return null;

        enemy.name = "Enemy " + (_spawned - 1).ToString("00");
        ConfigureShooter(enemy);
        return enemy;
    }

    // Instancia un enemigo en un waypoint y lo deja listo para caminar y
    // recibir dano. Lo comparten los enemigos normales y el jefe; lo que
    // los diferencia (armas, comportamiento en el castillo) se agrega
    // despues, por fuera.
    private GameObject CreateEnemy(GameObject prefab, Path path, int waypointIndex,
        float health, int reward, float speed)
    {
        if (prefab == null || path == null)
            return null;

        Transform[] waypoints = path.GetWaypoints();
        if (waypoints == null || waypoints.Length == 0)
            return null;

        int startIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
        Transform startWaypoint = waypoints[startIndex];
        if (startWaypoint == null)
            return null;

        GameObject enemy = Instantiate(prefab, startWaypoint.position, startWaypoint.rotation);
        _spawned++;
        _aliveCount++;

        // Recorrido: el enemigo sigue los waypoints que armo PathBuilder.
        TestEnemyMovement movement = enemy.GetComponent<TestEnemyMovement>();
        if (movement != null)
        {
            movement.SetPath(path);
            // Sin esto el enemigo que sale de un waypoint intermedio se
            // volveria caminando hacia el principio del camino.
            movement.SetStartWaypoint(startIndex);
            movement.SetMovementSpeed(speed);
            movement.Finished += HandleEnemyFinished;
        }

        // Vida: si el prefab no la trae, se le agrega aqui.
        EnemyHealth enemyHealthComponent = enemy.GetComponent<EnemyHealth>();
        if (enemyHealthComponent == null)
            enemyHealthComponent = enemy.AddComponent<EnemyHealth>();

        enemyHealthComponent.Setup(health, reward, economy, goldPopupPrefab);
        enemyHealthComponent.Died += HandleEnemyDied;

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
        if (UnityEngine.Random.value >= GetShooterChanceForWave(_waveNumber))
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

        attack.Setup(GetShooterDamageForWave(_waveNumber), shooterAttackRate,
            shooterProjectilePrefab, shooterProjectileSpeed,
            shooterPopupPrefab);
    }

    // --- Debug (lo llama GameDebugWindow, no el juego) ---

    // Salta el contador de oleadas para probar el escalado de una oleada
    // alta sin tener que jugar las anteriores. Solo cambia el numero: no
    // arranca ni corta la oleada en curso.
    public void DebugSetWaveNumber(int waveNumber)
    {
        _waveNumber = Mathf.Max(0, waveNumber);
    }

    // Borra los enemigos que haya en pista ahora mismo. No cuentan como
    // muertos (no pagan oro ni quitan vidas): es para limpiar la escena.
    // Devuelve cuantos se llevo por delante.
    public int DebugClearEnemies()
    {
        TestEnemyMovement[] alive = FindObjectsByType<TestEnemyMovement>(FindObjectsSortMode.None);

        for (int i = 0; i < alive.Length; i++)
        {
            if (Application.isPlaying)
                Destroy(alive[i].gameObject);
            else
                DestroyImmediate(alive[i].gameObject);
        }

        _aliveCount = 0;
        return alive.Length;
    }

    // Cuando un enemigo llega al final le quita vidas al jugador y desaparece.
    private void HandleEnemyFinished(TestEnemyMovement movement)
    {
        movement.Finished -= HandleEnemyFinished;

        // El jefe no se va al llegar: se queda golpeando el castillo hasta
        // que lo maten. Sigue contando como vivo a proposito, asi la oleada
        // no puede terminar sin derrotarlo.
        BossEnemy boss = movement.GetComponent<BossEnemy>();
        if (boss != null)
        {
            boss.StartAttackingCastle(playerBase);

            if (EnemyReachedEnd != null)
                EnemyReachedEnd();

            return;
        }

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
