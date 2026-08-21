using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Custom.UI;

// Ventana de debug del juego. Hoy junta:
//  - Velocidad del juego: probar el juego a x2/x3 sin tocar codigo (usa
//    GameSpeedController, el mismo punto que usara mas adelante un boton
//    real del HUD).
//  - Oleadas y enemigos: cantidad, vida, dano, velocidad de los enemigos
//    (EnemySpawner), incluidos los que disparan a las torres.
//  - Oro: ver y fijar el dinero del jugador (EconomyManager).
//  - Powerups: costo de cada ranura, resetear cooldowns y desbloquear todo.
//  - Balance de torres: ver y ajustar el alcance (y el resto de las
//    estadisticas) sin tener que buscar los assets a mano.
// Se abre desde el menu Tower Defense > Game Debug.
public class GameDebugWindow : EditorWindow
{
    private TowerCatalog _catalog;
    private float _maxSliderRange = 30f;   // tope del slider, subelo si te queda corto
    private bool _drawRanges = true;       // circulos de alcance en la vista de escena
    private bool _drawOnlySelected;        // solo la torre seleccionada
    private Vector2 _scroll;

    private EnemySpawner _spawner;
    private int _jumpToWave = 5;        // oleada a la que saltar para probar el escalado
    private bool _showWavePreview;      // tabla de proximas oleadas
    private int _previewWaves = 8;
    private bool _drawSpawnPoint = true; // marca el punto de salida en la escena
    private bool _showSpawnSources = true;
    private bool _showBoss = true;
    private BoardSelector _boardSelector;
    private CameraEdgePan _cameraPan;
    private bool _editCameraLimits = true;  // tiradores en la vista de escena
    // Margen al ajustar los limites al camino. En 0 la camara puede enfocar
    // justo hasta el borde del recorrido: subirlo deja ver mas alla del
    // nivel, y de ahi en adelante empieza a entrar vacio en pantalla.
    private float _fitMargin;
    private EconomyManager _economy;
    private GameHUDController _hud;
    private int _moneyToSet = 500;         // valor pendiente del campo "Fijar oro"

    // Velocidades de prueba. x3 es la que se piensa dejar como boton
    // "oficial" en el HUD mas adelante; las demas son solo para probar.
    private static readonly float[] SpeedPresets = { 1f, 2f, 3f };

    [MenuItem("Tower Defense/Game Debug")]
    public static void Open()
    {
        GameDebugWindow window = GetWindow<GameDebugWindow>("Game Debug");
        window.minSize = new Vector2(380f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        if (_catalog == null)
            _catalog = FindCatalog();
        if (_spawner == null)
            _spawner = FindFirstObjectByType<EnemySpawner>();
        if (_economy == null)
            _economy = FindFirstObjectByType<EconomyManager>();
        if (_hud == null)
            _hud = FindFirstObjectByType<GameHUDController>();
        if (_cameraPan == null)
            _cameraPan = FindFirstObjectByType<CameraEdgePan>();

        SceneView.duringSceneGui += DrawSceneRanges;
        SceneView.duringSceneGui += DrawCameraLimitHandles;
        SceneView.duringSceneGui += DrawSpawnPointGizmo;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawSceneRanges;
        SceneView.duringSceneGui -= DrawCameraLimitHandles;
        SceneView.duringSceneGui -= DrawSpawnPointGizmo;
    }

    private void Update()
    {
        // Mientras la ventana este abierta y el juego corriendo, se repinta
        // sola para que el boton de velocidad activa se mantenga al dia
        // aunque el cambio venga de otro lado (por ejemplo una pausa).
        if (Application.isPlaying)
            Repaint();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSpeedSection();
        EditorGUILayout.Space();

        DrawCameraSection();
        EditorGUILayout.Space();

        DrawWaveSection();
        EditorGUILayout.Space();

        DrawBoardsSection();
        EditorGUILayout.Space();

        DrawEconomySection();
        EditorGUILayout.Space();

        DrawPowerupsSection();
        EditorGUILayout.Space();

        DrawCatalogField();

        if (_catalog == null)
        {
            EditorGUILayout.HelpBox(
                "No hay ningun TowerCatalog asignado. Deberia estar en " +
                "Assets/_Custom/Data/Towers/TowerCatalog.asset.",
                MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (_catalog.levels == null || _catalog.levels.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "El catalogo no tiene niveles. Agregalos desde el propio asset " +
                "(cada nivel es un TowerData).",
                MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space();
        DrawLevelTable();

        EditorGUILayout.Space();
        DrawSceneOptions();

        EditorGUILayout.Space();
        DrawSceneTowers();

        EditorGUILayout.EndScrollView();
    }

    // Botones x1/x2/x3: cambian Time.timeScale via GameSpeedController, asi
    // que aceleran enemigos, torres y animaciones sin tocar cada sistema.
    // Solo tiene efecto en Play Mode (fuera de el no hay nada corriendo que
    // acelerar).
    private void DrawSpeedSection()
    {
        EditorGUILayout.LabelField("Velocidad del juego", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        EditorGUILayout.BeginHorizontal();
        foreach (float speed in SpeedPresets)
        {
            bool active = Application.isPlaying && Mathf.Approximately(GameSpeedController.CurrentSpeed, speed);
            GUI.backgroundColor = active ? Color.green : Color.white;
            if (GUILayout.Button("x" + speed.ToString("0.#"), GUILayout.Height(28f)))
                GameSpeedController.SetSpeed(speed);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Solo funciona en Play Mode.", MessageType.None);
        }
        else
        {
            EditorGUILayout.LabelField("Velocidad actual: x" + GameSpeedController.CurrentSpeed.ToString("0.##"));
        }

        EditorGUILayout.HelpBox(
            "x3 es la velocidad pensada para un boton oficial en el HUD mas " +
            "adelante (todavia no agregado). Usa GameSpeedController.SetSpeed(...) " +
            "desde cualquier boton nuevo para reusar esto mismo.",
            MessageType.Info);
    }

    // Movimiento de la camara al llevar el puntero a los bordes, y los
    // limites de hasta donde puede llegar en este nivel. Los limites son
    // por escena: cada nivel marca los suyos y se guardan ahi.
    private void DrawCameraSection()
    {
        EditorGUILayout.LabelField("Camara", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _cameraPan = (CameraEdgePan)EditorGUILayout.ObjectField(
            "Camera Edge Pan", _cameraPan, typeof(CameraEdgePan), true);
        if (GUILayout.Button("Buscar", GUILayout.Width(60f)))
            _cameraPan = FindFirstObjectByType<CameraEdgePan>();
        EditorGUILayout.EndHorizontal();

        if (_cameraPan == null)
        {
            EditorGUILayout.HelpBox(
                "Esta escena todavia no tiene el movimiento de camara por bordes.",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(Camera.main == null);
            if (GUILayout.Button("Agregar a la camara principal"))
                AddCameraPanToMainCamera();
            EditorGUI.EndDisabledGroup();

            if (Camera.main == null)
                EditorGUILayout.HelpBox("No hay ninguna camara con el tag MainCamera.", MessageType.Warning);
            return;
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        bool panEnabled = EditorGUILayout.Toggle("Activo", _cameraPan.panEnabled);
        float edgeThickness = EditorGUILayout.Slider("Franja del borde (px)", _cameraPan.edgeThickness, 5f, 200f);
        float panSpeed = EditorGUILayout.Slider("Velocidad", _cameraPan.panSpeed, 1f, 60f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("Limites del nivel (X y Z)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Vector2 limitMin = EditorGUILayout.Vector2Field("Minimo (X, Z)", _cameraPan.limitMin);
        Vector2 limitMax = EditorGUILayout.Vector2Field("Maximo (X, Z)", _cameraPan.limitMax);
        bool drawGizmo = EditorGUILayout.Toggle("Dibujar en la escena", _cameraPan.drawLimitsGizmo);
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_cameraPan, "Cambiar camara del nivel");
            _cameraPan.panEnabled = panEnabled;
            _cameraPan.edgeThickness = Mathf.Max(1f, edgeThickness);
            _cameraPan.panSpeed = Mathf.Max(0f, panSpeed);
            _cameraPan.limitMin = limitMin;
            _cameraPan.limitMax = limitMax;
            _cameraPan.drawLimitsGizmo = drawGizmo;

            if (!Application.isPlaying)
                EditorUtility.SetDirty(_cameraPan);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        _editCameraLimits = EditorGUILayout.Toggle("Tiradores en la escena", _editCameraLimits);

        EditorGUILayout.BeginHorizontal();
        _fitMargin = EditorGUILayout.FloatField("Margen", _fitMargin);
        if (GUILayout.Button("Ajustar al camino", GUILayout.Width(130f)))
            FitCameraLimitsToPath();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Centrar en la camara"))
            CenterCameraLimitsOnCamera();
        EditorGUI.BeginDisabledGroup(_cameraPan.IsInsideLimits());
        if (GUILayout.Button("Meter la camara dentro"))
        {
            Undo.RecordObject(_cameraPan.transform, "Meter la camara en los limites");
            _cameraPan.ApplyLimits();
            SceneView.RepaintAll();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (!_cameraPan.IsInsideLimits())
        {
            EditorGUILayout.HelpBox(
                "La camara esta fuera de sus propios limites: al empezar la partida " +
                "saltaria de golpe hacia adentro.",
                MessageType.Warning);
        }

        EditorGUILayout.HelpBox(
            "Los limites son de este nivel y se guardan en la escena. " +
            "Arrastra los tiradores de la vista de escena para ajustarlos, " +
            "o usa \"Ajustar al camino\" para encuadrar el recorrido.",
            MessageType.Info);
    }

    // Le pone el componente a la camara principal y arranca con unos
    // limites que ya encuadran el nivel, para no partir de cero.
    private void AddCameraPanToMainCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        _cameraPan = Undo.AddComponent<CameraEdgePan>(camera.gameObject);
        FitCameraLimitsToPath();
        EditorUtility.SetDirty(_cameraPan);
        SceneView.RepaintAll();
    }

    // Encuadra los limites sobre el recorrido de los enemigos, que es lo
    // que de verdad define cuanto del nivel hay que poder ver. Sin camino
    // se cae a una caja alrededor de la camara.
    private void FitCameraLimitsToPath()
    {
        if (_cameraPan == null)
            return;

        Undo.RecordObject(_cameraPan, "Ajustar limites de camara");

        Path path = FindFirstObjectByType<Path>();
        Transform[] waypoints = path != null ? path.GetWaypoints() : null;

        if (waypoints == null || waypoints.Length == 0)
        {
            CenterCameraLimitsOnCamera();
            return;
        }

        Vector3 first = waypoints[0].position;
        float minX = first.x, maxX = first.x, minZ = first.z, maxZ = first.z;
        for (int i = 1; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Vector3 p = waypoints[i].position;
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z);
            maxZ = Mathf.Max(maxZ, p.z);
        }

        // La camara mira en diagonal hacia abajo, asi que su posicion queda
        // corrida respecto de lo que enfoca. Se conserva ese desfase para
        // que los limites encuadren el camino y no la camara en si.
        Vector3 offset = OffsetFromCameraToFocus();

        _cameraPan.limitMin = new Vector2(minX - _fitMargin - offset.x, minZ - _fitMargin - offset.z);
        _cameraPan.limitMax = new Vector2(maxX + _fitMargin - offset.x, maxZ + _fitMargin - offset.z);

        if (!Application.isPlaying)
            EditorUtility.SetDirty(_cameraPan);
        SceneView.RepaintAll();
    }

    // Deja los limites como una caja centrada en la camara, del tamano que
    // ya tenian. Sirve de punto de partida cuando no hay camino.
    private void CenterCameraLimitsOnCamera()
    {
        if (_cameraPan == null)
            return;

        Undo.RecordObject(_cameraPan, "Centrar limites de camara");

        Vector3 position = _cameraPan.transform.position;
        Vector2 halfSize = new Vector2(
            Mathf.Abs(_cameraPan.limitMax.x - _cameraPan.limitMin.x) * 0.5f,
            Mathf.Abs(_cameraPan.limitMax.y - _cameraPan.limitMin.y) * 0.5f);

        // Sin tamano previo se usa una caja por defecto, para que los
        // tiradores tengan de donde agarrarse.
        if (halfSize.x < 0.01f) halfSize.x = 10f;
        if (halfSize.y < 0.01f) halfSize.y = 10f;

        _cameraPan.limitMin = new Vector2(position.x - halfSize.x, position.z - halfSize.y);
        _cameraPan.limitMax = new Vector2(position.x + halfSize.x, position.z + halfSize.y);

        if (!Application.isPlaying)
            EditorUtility.SetDirty(_cameraPan);
        SceneView.RepaintAll();
    }

    // Cuanto se corre la camara respecto del punto que enfoca, por estar
    // inclinada. Se calcula cruzando su mirada con el plano del suelo.
    private Vector3 OffsetFromCameraToFocus()
    {
        if (_cameraPan == null)
            return Vector3.zero;

        Transform t = _cameraPan.transform;
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        Ray ray = new Ray(t.position, t.forward);

        float distance;
        if (!ground.Raycast(ray, out distance))
            return Vector3.zero;

        Vector3 focus = ray.GetPoint(distance);
        return focus - t.position;
    }

    // Rectangulo de limites en la vista de escena, con tiradores en cada
    // lado para estirarlo con el raton.
    private void DrawCameraLimitHandles(SceneView sceneView)
    {
        if (_cameraPan == null || !_cameraPan.drawLimitsGizmo)
            return;

        float minX = Mathf.Min(_cameraPan.limitMin.x, _cameraPan.limitMax.x);
        float maxX = Mathf.Max(_cameraPan.limitMin.x, _cameraPan.limitMax.x);
        float minZ = Mathf.Min(_cameraPan.limitMin.y, _cameraPan.limitMax.y);
        float maxZ = Mathf.Max(_cameraPan.limitMin.y, _cameraPan.limitMax.y);
        float y = _cameraPan.transform.position.y;

        Handles.color = _cameraPan.IsInsideLimits() ? Color.cyan : Color.red;
        Vector3[] corners =
        {
            new Vector3(minX, y, minZ),
            new Vector3(maxX, y, minZ),
            new Vector3(maxX, y, maxZ),
            new Vector3(minX, y, maxZ),
        };
        Handles.DrawSolidRectangleWithOutline(corners, new Color(0f, 1f, 1f, 0.05f), Handles.color);

        if (!_editCameraLimits)
            return;

        EditorGUI.BeginChangeCheck();

        float newMinX = DrawAxisHandle(new Vector3(minX, y, (minZ + maxZ) * 0.5f), Vector3.right).x;
        float newMaxX = DrawAxisHandle(new Vector3(maxX, y, (minZ + maxZ) * 0.5f), Vector3.right).x;
        float newMinZ = DrawAxisHandle(new Vector3((minX + maxX) * 0.5f, y, minZ), Vector3.forward).z;
        float newMaxZ = DrawAxisHandle(new Vector3((minX + maxX) * 0.5f, y, maxZ), Vector3.forward).z;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_cameraPan, "Mover limites de camara");
            _cameraPan.limitMin = new Vector2(newMinX, newMinZ);
            _cameraPan.limitMax = new Vector2(newMaxX, newMaxZ);

            if (!Application.isPlaying)
                EditorUtility.SetDirty(_cameraPan);
            Repaint();
        }
    }

    // Tirador que solo corre sobre un eje.
    private static Vector3 DrawAxisHandle(Vector3 position, Vector3 axis)
    {
        float size = HandleUtility.GetHandleSize(position) * 0.12f;
        return Handles.Slider(position, axis, size, Handles.DotHandleCap, 0f);
    }

    // Cantidad, vida, dano y velocidad de los enemigos, incluidos los que
    // disparan a las torres. Todo son campos publicos de EnemySpawner:
    // editables tanto en Play Mode (para probar ya mismo) como fuera de el
    // (para dejar el balance guardado en la escena).
    private void DrawWaveSection()
    {
        EditorGUILayout.LabelField("Oleadas y enemigos", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _spawner = (EnemySpawner)EditorGUILayout.ObjectField(
            "Enemy Spawner", _spawner, typeof(EnemySpawner), true);
        if (GUILayout.Button("Buscar", GUILayout.Width(60f)))
            _spawner = FindFirstObjectByType<EnemySpawner>();
        EditorGUILayout.EndHorizontal();

        if (_spawner == null)
        {
            EditorGUILayout.HelpBox("No hay ningun EnemySpawner en la escena abierta.", MessageType.Warning);
            return;
        }

        DrawWaveControls();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        int enemiesPerWave = EditorGUILayout.IntField("Cantidad por oleada (0 = sin parar)", _spawner.enemiesPerWave);
        float spawnInterval = EditorGUILayout.FloatField("Intervalo entre enemigos (s)", _spawner.spawnInterval);
        float startDelay = EditorGUILayout.FloatField("Retraso inicial (s)", _spawner.startDelay);
        float enemyHealth = EditorGUILayout.FloatField("Vida", _spawner.enemyHealth);
        float enemySpeed = EditorGUILayout.FloatField("Velocidad", _spawner.enemySpeed);
        int enemyReward = EditorGUILayout.IntField("Recompensa al morir", _spawner.enemyReward);
        int damagePerEnemy = EditorGUILayout.IntField("Dano a la base al escapar", _spawner.damagePerEnemy);
        int waveSurvivalGold = EditorGUILayout.IntField("Oro por sobrevivir la oleada", _spawner.waveSurvivalGold);
        EditorGUILayout.EndVertical();

        int spawnWaypointIndex = DrawSpawnPointField();

        EditorGUILayout.LabelField("Aumento por oleada", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        float healthGrowth = EditorGUILayout.Slider("Vida (+% por oleada)", _spawner.healthGrowthPerWave, 0f, 2f);
        float speedGrowth = EditorGUILayout.Slider("Velocidad (+% por oleada)", _spawner.speedGrowthPerWave, 0f, 2f);
        float damageGrowth = EditorGUILayout.Slider("Dano de tiradores (+% por oleada)", _spawner.shooterDamageGrowthPerWave, 0f, 2f);
        float chanceGrowth = EditorGUILayout.Slider("Tiradores (+ por oleada)", _spawner.shooterChanceGrowthPerWave, 0f, 1f);
        int enemiesGrowth = EditorGUILayout.IntField("Enemigos de mas por oleada", _spawner.enemiesGrowthPerWave);
        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("Enemigos que disparan a torres", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        float shooterChance = EditorGUILayout.Slider("Probabilidad de salir armado", _spawner.shooterChance, 0f, 1f);
        float shooterRange = EditorGUILayout.FloatField("Alcance", _spawner.shooterRange);
        float shooterDamage = EditorGUILayout.FloatField("Dano", _spawner.shooterDamage);
        float shooterAttackRate = EditorGUILayout.FloatField("Disparos por segundo", _spawner.shooterAttackRate);
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_spawner, "Cambiar balance de oleadas");
            _spawner.enemiesPerWave = Mathf.Max(0, enemiesPerWave);
            _spawner.spawnInterval = Mathf.Max(0.01f, spawnInterval);
            _spawner.startDelay = Mathf.Max(0f, startDelay);
            _spawner.enemyHealth = Mathf.Max(0f, enemyHealth);
            _spawner.enemySpeed = Mathf.Max(0f, enemySpeed);
            _spawner.enemyReward = Mathf.Max(0, enemyReward);
            _spawner.damagePerEnemy = Mathf.Max(0, damagePerEnemy);
            _spawner.waveSurvivalGold = Mathf.Max(0, waveSurvivalGold);
            _spawner.spawnWaypointIndex = Mathf.Max(0, spawnWaypointIndex);

            _spawner.healthGrowthPerWave = Mathf.Max(0f, healthGrowth);
            _spawner.speedGrowthPerWave = Mathf.Max(0f, speedGrowth);
            _spawner.shooterDamageGrowthPerWave = Mathf.Max(0f, damageGrowth);
            _spawner.shooterChanceGrowthPerWave = Mathf.Clamp01(chanceGrowth);
            _spawner.enemiesGrowthPerWave = enemiesGrowth;

            _spawner.shooterChance = Mathf.Clamp01(shooterChance);
            _spawner.shooterRange = Mathf.Max(0f, shooterRange);
            _spawner.shooterDamage = Mathf.Max(0f, shooterDamage);
            _spawner.shooterAttackRate = Mathf.Max(0f, shooterAttackRate);

            if (!Application.isPlaying)
                EditorUtility.SetDirty(_spawner);
            SceneView.RepaintAll();
        }

        // Fuera del bloque de cambios a proposito: es una opcion de vista,
        // no un dato de la escena, y no deberia ensuciarla al alternarla.
        EditorGUI.BeginChangeCheck();
        _drawSpawnPoint = EditorGUILayout.Toggle("Marcar la salida en la escena", _drawSpawnPoint);
        if (EditorGUI.EndChangeCheck())
            SceneView.RepaintAll();

        DrawSpawnSourcesSection();
        DrawBossSection();
        DrawWavePreview();
        DrawPlayModePersistence();
    }

    // El jefe: en que oleada sale y con que numeros. Solo sale uno por
    // nivel, al empezar la oleada elegida.
    private void DrawBossSection()
    {
        _showBoss = EditorGUILayout.Foldout(_showBoss, "Jefe", true);
        if (!_showBoss)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();

        GameObject bossPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab del jefe", _spawner.bossPrefab, typeof(GameObject), false);
        int bossWave = EditorGUILayout.IntField("Sale en la oleada (0 = nunca)", _spawner.bossWave);
        float bossHealth = EditorGUILayout.FloatField("Vida", _spawner.bossHealth);
        float bossSpeed = EditorGUILayout.FloatField("Velocidad", _spawner.bossSpeed);
        int bossReward = EditorGUILayout.IntField("Oro al derrotarlo", _spawner.bossReward);

        int sourceCount = _spawner.spawnSources != null ? _spawner.spawnSources.Length : 0;
        int bossSourceIndex = _spawner.bossSourceIndex;
        if (sourceCount > 0)
        {
            string[] labels = new string[sourceCount];
            for (int i = 0; i < sourceCount; i++)
                labels[i] = _spawner.spawnSources[i] != null ? _spawner.spawnSources[i].label : "Salida " + i;

            bossSourceIndex = EditorGUILayout.Popup("Entra por", Mathf.Clamp(bossSourceIndex, 0, sourceCount - 1), labels);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_spawner, "Cambiar el jefe");
            _spawner.bossPrefab = bossPrefab;
            _spawner.bossWave = Mathf.Max(0, bossWave);
            _spawner.bossHealth = Mathf.Max(1f, bossHealth);
            _spawner.bossSpeed = Mathf.Max(0.1f, bossSpeed);
            _spawner.bossReward = Mathf.Max(0, bossReward);
            _spawner.bossSourceIndex = Mathf.Max(0, bossSourceIndex);

            if (!Application.isPlaying)
                EditorUtility.SetDirty(_spawner);
        }

        if (_spawner.bossPrefab == null)
        {
            EditorGUILayout.HelpBox(
                "Sin prefab no sale ningun jefe. El del proyecto es " +
                "Assets/_Custom/Prefabs/Enemies/Boss_Golem.prefab.",
                MessageType.Warning);
        }
        else if (_spawner.bossWave <= 0)
        {
            EditorGUILayout.HelpBox("Con la oleada en 0 el jefe nunca aparece.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Al llegar al castillo no desaparece: se queda golpeando y hay que " +
                "matarlo para terminar la oleada.",
                MessageType.None);
        }

        if (Application.isPlaying)
        {
            EditorGUI.BeginDisabledGroup(_spawner.bossPrefab == null);
            if (GUILayout.Button("Sacar el jefe ahora"))
                _spawner.SpawnBoss();
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndVertical();
    }

    // Salidas multiples: cada una con su camino, su ritmo y su dureza.
    // Vacio = el nivel usa la salida unica de siempre.
    private void DrawSpawnSourcesSection()
    {
        SpawnSource[] sources = _spawner.spawnSources;
        int count = sources != null ? sources.Length : 0;

        _showSpawnSources = EditorGUILayout.Foldout(
            _showSpawnSources, "Salidas multiples (" + count + ")", true);
        if (!_showSpawnSources)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (count == 0)
        {
            EditorGUILayout.HelpBox(
                "Sin salidas configuradas: los enemigos salen todos del punto de arriba.",
                MessageType.None);
        }

        for (int i = 0; i < count; i++)
        {
            if (sources[i] == null)
                sources[i] = new SpawnSource();

            DrawSpawnSource(sources[i], i);
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Agregar salida"))
            AddSpawnSource();

        EditorGUI.BeginDisabledGroup(count == 0);
        if (GUILayout.Button("Quitar la ultima"))
            RemoveLastSpawnSource();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (count > 0)
        {
            EditorGUILayout.HelpBox(
                "Cada salida manda sus enemigos por su cuenta. Los campos en 0 " +
                "toman el valor general de arriba, para no repetirlo en todas.",
                MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSpawnSource(SpawnSource source, int index)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        source.active = EditorGUILayout.Toggle(source.active, GUILayout.Width(18f));
        source.label = EditorGUILayout.TextField(source.label);
        if (Application.isPlaying && GUILayout.Button("Soltar 1", GUILayout.Width(70f)))
            _spawner.SpawnFromSource(source);
        EditorGUILayout.EndHorizontal();

        source.pathBuilder = (PathBuilder)EditorGUILayout.ObjectField(
            "Camino (vacio = el principal)", source.pathBuilder, typeof(PathBuilder), true);

        // El slider se ajusta al camino de esta salida, que puede no ser el
        // principal y tener otra cantidad de waypoints.
        int waypoints = CountWaypointsOf(source.pathBuilder);
        if (waypoints > 0)
            source.startWaypointIndex = EditorGUILayout.IntSlider("Sale del waypoint", source.startWaypointIndex, 0, waypoints - 1);
        else
            source.startWaypointIndex = EditorGUILayout.IntField("Sale del waypoint", source.startWaypointIndex);

        source.enemiesPerWave = EditorGUILayout.IntField("Enemigos (0 = el general)", source.enemiesPerWave);
        source.spawnInterval = EditorGUILayout.FloatField("Intervalo (0 = el general)", source.spawnInterval);
        source.startDelay = EditorGUILayout.FloatField("Espera extra (s)", source.startDelay);
        source.healthMultiplier = EditorGUILayout.FloatField("Vida x", source.healthMultiplier);
        source.speedMultiplier = EditorGUILayout.FloatField("Velocidad x", source.speedMultiplier);

        if (EditorGUI.EndChangeCheck())
        {
            source.enemiesPerWave = Mathf.Max(0, source.enemiesPerWave);
            source.spawnInterval = Mathf.Max(0f, source.spawnInterval);
            source.startDelay = Mathf.Max(0f, source.startDelay);
            source.healthMultiplier = Mathf.Max(0.01f, source.healthMultiplier);
            source.speedMultiplier = Mathf.Max(0.01f, source.speedMultiplier);
            source.startWaypointIndex = Mathf.Max(0, source.startWaypointIndex);

            if (!Application.isPlaying)
                EditorUtility.SetDirty(_spawner);
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndVertical();
    }

    private void AddSpawnSource()
    {
        Undo.RecordObject(_spawner, "Agregar salida de enemigos");

        SpawnSource[] old = _spawner.spawnSources;
        int count = old != null ? old.Length : 0;
        SpawnSource[] grown = new SpawnSource[count + 1];
        for (int i = 0; i < count; i++)
            grown[i] = old[i];

        grown[count] = new SpawnSource();
        grown[count].label = "Salida " + (count + 1);
        _spawner.spawnSources = grown;

        if (!Application.isPlaying)
            EditorUtility.SetDirty(_spawner);
        SceneView.RepaintAll();
    }

    private void RemoveLastSpawnSource()
    {
        SpawnSource[] old = _spawner.spawnSources;
        if (old == null || old.Length == 0)
            return;

        Undo.RecordObject(_spawner, "Quitar salida de enemigos");

        SpawnSource[] shrunk = new SpawnSource[old.Length - 1];
        for (int i = 0; i < shrunk.Length; i++)
            shrunk[i] = old[i];
        _spawner.spawnSources = shrunk;

        if (!Application.isPlaying)
            EditorUtility.SetDirty(_spawner);
        SceneView.RepaintAll();
    }

    private int CountWaypointsOf(PathBuilder builder)
    {
        if (builder == null)
            return CountPathWaypoints();

        Path path = builder.Path;
        if (path == null)
            return 0;

        Transform[] waypoints = path.GetWaypoints();
        return waypoints != null ? waypoints.Length : 0;
    }

    // Estado de la oleada y botones para manejarla a mano. Solo tienen
    // sentido con el juego corriendo.
    private void DrawWaveControls()
    {
        if (!Application.isPlaying)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Oleada " + _spawner.WaveNumber +
            " | Corriendo: " + _spawner.IsRunning +
            " | Enviados: " + _spawner.SpawnedCount +
            " | Vivos: " + _spawner.AliveCount);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(_spawner.IsRunning);
        if (GUILayout.Button("Iniciar oleada"))
            _spawner.StartWave();
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!_spawner.IsRunning);
        if (GUILayout.Button("Terminar oleada"))
            _spawner.StopWave();
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Soltar 1 enemigo"))
            _spawner.SpawnEnemy();

        if (GUILayout.Button("Matar a todos"))
        {
            int cleared = _spawner.DebugClearEnemies();
            Debug.Log("Game Debug: se quitaron " + cleared + " enemigos de la pista.");
        }
        EditorGUILayout.EndHorizontal();

        // Saltar de oleada: para ver como pega el escalado en una oleada
        // alta sin jugar todas las anteriores.
        EditorGUILayout.BeginHorizontal();
        _jumpToWave = Mathf.Max(1, EditorGUILayout.IntField("Saltar a la oleada", _jumpToWave));
        if (GUILayout.Button("Ir", GUILayout.Width(70f)))
        {
            // Se corta la oleada en curso antes de saltar: cambiar el numero
            // a mitad de oleada le movería el piso a la cuenta de enemigos
            // (que se calcula a partir de ese numero) y podria darla por
            // terminada de golpe.
            if (_spawner.IsRunning)
                _spawner.StopWave();

            _spawner.DebugClearEnemies();
            // Una menos porque StartWave suma uno al arrancar.
            _spawner.DebugSetWaveNumber(_jumpToWave - 1);
            _spawner.StartWave();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    // Desde que waypoint salen los enemigos, con el total del camino a la
    // vista para no pasarse.
    private int DrawSpawnPointField()
    {
        int waypointCount = CountPathWaypoints();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        int index;
        if (waypointCount > 0)
        {
            index = EditorGUILayout.IntSlider(
                "Salen del waypoint", _spawner.spawnWaypointIndex, 0, waypointCount - 1);

            EditorGUILayout.LabelField(
                "   " + (index == 0
                    ? "Principio del camino (lo normal)."
                    : "Se saltan " + index + " de " + (waypointCount - 1) + " tramos."),
                EditorStyles.miniLabel);
        }
        else
        {
            index = EditorGUILayout.IntField("Salen del waypoint", _spawner.spawnWaypointIndex);
            EditorGUILayout.HelpBox("No se encontro el camino, no se puede saber cuantos waypoints hay.", MessageType.None);
        }
        EditorGUILayout.EndVertical();

        return index;
    }

    // Tabla de como quedarian las proximas oleadas con el aumento actual.
    // Evita tener que jugarlas para saber si el escalado se dispara.
    private void DrawWavePreview()
    {
        _showWavePreview = EditorGUILayout.Foldout(_showWavePreview, "Ver como quedan las proximas oleadas", true);
        if (!_showWavePreview)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _previewWaves = Mathf.Clamp(EditorGUILayout.IntField("Cuantas oleadas", _previewWaves), 1, 30);

        // Se arranca desde la oleada en curso si el juego esta corriendo.
        int firstWave = Application.isPlaying ? Mathf.Max(1, _spawner.WaveNumber) : 1;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Oleada", EditorStyles.miniBoldLabel, GUILayout.Width(55f));
        EditorGUILayout.LabelField("Enem.", EditorStyles.miniBoldLabel, GUILayout.Width(45f));
        EditorGUILayout.LabelField("Vida", EditorStyles.miniBoldLabel, GUILayout.Width(50f));
        EditorGUILayout.LabelField("Vel.", EditorStyles.miniBoldLabel, GUILayout.Width(45f));
        EditorGUILayout.LabelField("Dano", EditorStyles.miniBoldLabel, GUILayout.Width(45f));
        EditorGUILayout.LabelField("Tirad.", EditorStyles.miniBoldLabel, GUILayout.Width(45f));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _previewWaves; i++)
        {
            int wave = firstWave + i;

            // Con enemiesPerWave en 0 la oleada no termina nunca.
            int waveEnemies = _spawner.GetEnemiesForWave(wave);
            string enemiesText = waveEnemies > 0 ? waveEnemies.ToString() : "sin fin";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(wave.ToString(), EditorStyles.miniLabel, GUILayout.Width(55f));
            EditorGUILayout.LabelField(enemiesText, EditorStyles.miniLabel, GUILayout.Width(45f));
            EditorGUILayout.LabelField(_spawner.GetEnemyHealthForWave(wave).ToString("0"), EditorStyles.miniLabel, GUILayout.Width(50f));
            EditorGUILayout.LabelField(_spawner.GetEnemySpeedForWave(wave).ToString("0.0"), EditorStyles.miniLabel, GUILayout.Width(45f));
            EditorGUILayout.LabelField(_spawner.GetShooterDamageForWave(wave).ToString("0.0"), EditorStyles.miniLabel, GUILayout.Width(45f));
            EditorGUILayout.LabelField((_spawner.GetShooterChanceForWave(wave) * 100f).ToString("0") + "%", EditorStyles.miniLabel, GUILayout.Width(45f));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    // Guardar lo tocado en Play Mode. Unity descarta esos cambios al salir,
    // asi que se apuntan y se vuelven a aplicar solos al volver a Edit Mode.
    private void DrawPlayModePersistence()
    {
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Fuera de Play Mode los cambios ya se guardan solos en la escena " +
                "(acuerdate de Ctrl+S).",
                MessageType.None);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (SpawnerTuningStore.HasPending)
        {
            EditorGUILayout.HelpBox(
                "Listo: al salir de Play Mode estos valores se van a copiar a la escena.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Volver a apuntar (con los de ahora)"))
                SpawnerTuningStore.SavePending(_spawner);
            if (GUILayout.Button("Cancelar", GUILayout.Width(90f)))
                SpawnerTuningStore.DiscardPending();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Estos cambios se pierden al salir de Play Mode, salvo que los guardes.",
                MessageType.Warning);

            if (GUILayout.Button("Guardar estos valores al salir de Play Mode"))
                SpawnerTuningStore.SavePending(_spawner);
        }

        EditorGUILayout.EndVertical();
    }

    private int CountPathWaypoints()
    {
        Path path = FindFirstObjectByType<Path>();
        if (path == null)
            return 0;

        Transform[] waypoints = path.GetWaypoints();
        return waypoints != null ? waypoints.Length : 0;
    }

    // Marca en la escena de donde salen los enemigos.
    private void DrawSpawnPointGizmo(SceneView sceneView)
    {
        if (_spawner == null || !_drawSpawnPoint)
            return;

        Path path = FindFirstObjectByType<Path>();
        if (path == null)
            return;

        Transform[] waypoints = path.GetWaypoints();
        if (waypoints == null || waypoints.Length == 0)
            return;

        int index = Mathf.Clamp(_spawner.spawnWaypointIndex, 0, waypoints.Length - 1);
        Transform waypoint = waypoints[index];
        if (waypoint == null)
            return;

        Handles.color = Color.green;
        float size = HandleUtility.GetHandleSize(waypoint.position) * 0.35f;
        Handles.DrawWireDisc(waypoint.position, Vector3.up, size);
        Handles.DrawLine(waypoint.position, waypoint.position + Vector3.up * size * 3f);
        Handles.Label(waypoint.position + Vector3.up * size * 3.2f, "Salen aqui (wp " + index + ")");
    }

    // Tableros del nivel. Con mas de uno hace falta un BoardSelector para
    // que las teclas muevan solo el elegido y no los dos a la vez.
    private void DrawBoardsSection()
    {
        EditorGUILayout.LabelField("Tableros", EditorStyles.boldLabel);

        BoardManager[] boards = FindObjectsByType<BoardManager>(FindObjectsSortMode.None);

        if (_boardSelector == null)
            _boardSelector = FindFirstObjectByType<BoardSelector>();

        EditorGUILayout.LabelField("Tableros en la escena: " + boards.Length);

        if (boards.Length <= 1)
        {
            EditorGUILayout.HelpBox(
                boards.Length == 0
                    ? "Esta escena no tiene ningun BoardManager."
                    : "Un solo tablero: no hace falta selector, las teclas lo mueven directo.",
                MessageType.None);

            if (boards.Length == 1 && _boardSelector != null)
            {
                EditorGUILayout.HelpBox(
                    "Hay un BoardSelector de mas para un solo tablero. No molesta, pero sobra.",
                    MessageType.None);
            }
            return;
        }

        if (_boardSelector == null)
        {
            EditorGUILayout.HelpBox(
                "Hay " + boards.Length + " tableros pero ningun BoardSelector: las teclas " +
                "van a mover todos a la vez.",
                MessageType.Warning);

            if (GUILayout.Button("Crear el selector y enlazar los tableros"))
                CreateBoardSelector(boards);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();
        bool showHighlight = EditorGUILayout.Toggle("Marcar el tablero activo", _boardSelector.showHighlight);
        Color highlightColor = EditorGUILayout.ColorField("Color de la marca", _boardSelector.highlightColor);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_boardSelector, "Cambiar marca del tablero");
            _boardSelector.showHighlight = showHighlight;
            _boardSelector.highlightColor = highlightColor;
            if (!Application.isPlaying)
                EditorUtility.SetDirty(_boardSelector);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cambiar de tablero en el juego: Tab", EditorStyles.miniLabel);

        BoardManager[] selectorBoards = _boardSelector.boards;
        int count = selectorBoards != null ? selectorBoards.Length : 0;

        for (int i = 0; i < count; i++)
        {
            BoardManager board = selectorBoards[i];
            if (board == null)
                continue;

            EditorGUILayout.BeginHorizontal();

            bool isSelected = Application.isPlaying
                ? _boardSelector.SelectedIndex == i
                : i == 0;

            GUI.backgroundColor = isSelected ? Color.green : Color.white;
            EditorGUILayout.LabelField(
                (isSelected ? "> " : "   ") + board.gameObject.name,
                GUILayout.MinWidth(120f));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField(
                board.acceptsInput ? "recibe teclas" : "quieto",
                EditorStyles.miniLabel, GUILayout.Width(90f));

            if (GUILayout.Button("Ir", GUILayout.Width(30f)))
            {
                Selection.activeGameObject = board.gameObject;
                SceneView.FrameLastActiveSceneView();
            }

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Usar", GUILayout.Width(50f)))
                _boardSelector.Select(i);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Fuera de Play Mode se marca el primero como elegido; el cambio real " +
                "se ve con el juego corriendo.",
                MessageType.None);
        }
    }

    // Crea el selector y le engancha los tableros que haya en la escena.
    private void CreateBoardSelector(BoardManager[] boards)
    {
        GameObject holder = new GameObject("BoardSelector");
        Undo.RegisterCreatedObjectUndo(holder, "Crear BoardSelector");

        _boardSelector = holder.AddComponent<BoardSelector>();
        _boardSelector.boards = boards;
        _boardSelector.inputController = FindFirstObjectByType<InputController>();

        // El primero manda; el resto arranca quieto.
        for (int i = 0; i < boards.Length; i++)
        {
            if (boards[i] == null)
                continue;

            Undo.RecordObject(boards[i], "Enlazar tablero al selector");
            boards[i].acceptsInput = i == 0;
            EditorUtility.SetDirty(boards[i]);
        }

        EditorUtility.SetDirty(_boardSelector);
        EditorSceneManager.MarkSceneDirty(holder.scene);
    }

    // Dinero del jugador. Fuera de Play Mode se edita el oro inicial
    // (startingMoney, privado); en Play Mode se puede fijar o sumar sobre
    // el dinero que ya tiene la partida en curso.
    private void DrawEconomySection()
    {
        EditorGUILayout.LabelField("Oro", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _economy = (EconomyManager)EditorGUILayout.ObjectField(
            "Economy Manager", _economy, typeof(EconomyManager), true);
        if (GUILayout.Button("Buscar", GUILayout.Width(60f)))
            _economy = FindFirstObjectByType<EconomyManager>();
        EditorGUILayout.EndHorizontal();

        if (_economy == null)
        {
            EditorGUILayout.HelpBox("No hay ningun EconomyManager en la escena abierta.", MessageType.Warning);
            return;
        }

        if (!Application.isPlaying)
        {
            SerializedObject so = new SerializedObject(_economy);
            SerializedProperty startingMoney = so.FindProperty("startingMoney");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(startingMoney, new GUIContent("Oro inicial"));
            if (EditorGUI.EndChangeCheck())
                so.ApplyModifiedProperties();

            EditorGUILayout.HelpBox("Solo se puede fijar o sumar oro en Play Mode.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField("Oro actual: " + _economy.Money);

        EditorGUILayout.BeginHorizontal();
        _moneyToSet = EditorGUILayout.IntField("Nuevo valor", _moneyToSet);
        if (GUILayout.Button("Fijar", GUILayout.Width(60f)))
            _economy.SetMoney(_moneyToSet);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+100")) _economy.AddCurrency(100);
        if (GUILayout.Button("+500")) _economy.AddCurrency(500);
        if (GUILayout.Button("+1000")) _economy.AddCurrency(1000);
        if (GUILayout.Button("Vaciar")) _economy.SetMoney(0);
        EditorGUILayout.EndHorizontal();
    }

    // Costo de cada ranura del HUD (editable siempre) y dos atajos que solo
    // tienen sentido en Play Mode: resetear cooldowns y desbloquear todo,
    // para no tener que jugar una partida entera cada vez que se prueba.
    private void DrawPowerupsSection()
    {
        EditorGUILayout.LabelField("Powerups", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _hud = (GameHUDController)EditorGUILayout.ObjectField(
            "Game HUD Controller", _hud, typeof(GameHUDController), true);
        if (GUILayout.Button("Buscar", GUILayout.Width(60f)))
            _hud = FindFirstObjectByType<GameHUDController>();
        EditorGUILayout.EndHorizontal();

        if (_hud == null)
        {
            EditorGUILayout.HelpBox("No hay ningun GameHUDController en la escena abierta.", MessageType.Warning);
            return;
        }

        string[] slotNames = { "1 - Bomba", "2 - EMP", "3 - Repulsion", "4 - Reparacion", "5 - Tablero" };
        PowerUpCooldownConfig[] configs =
        {
            _hud.ability1Cooldown, _hud.ability2Cooldown, _hud.ability3Cooldown,
            _hud.ability4Cooldown, _hud.ability5Cooldown
        };
        int[] costs = { _hud.ability1Cost, _hud.ability2Cost, _hud.ability3Cost, _hud.ability4Cost, _hud.ability5Cost };

        EditorGUI.BeginChangeCheck();

        for (int i = 0; i < slotNames.Length; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(slotNames[i], EditorStyles.boldLabel);

            costs[i] = EditorGUILayout.IntField("Costo de desbloqueo", costs[i]);
            if (configs[i] != null)
            {
                configs[i].cooldownSeconds = EditorGUILayout.FloatField("Cooldown (s)", configs[i].cooldownSeconds);
                configs[i].usageLimitMode = (PowerUpUsageLimitMode)EditorGUILayout.EnumPopup(
                    "Limite de usos", configs[i].usageLimitMode);
                if (configs[i].usageLimitMode != PowerUpUsageLimitMode.Unlimited)
                    configs[i].maxUses = EditorGUILayout.IntField("Usos maximos", configs[i].maxUses);
            }

            EditorGUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_hud, "Cambiar balance de powerups");
            _hud.ability1Cost = Mathf.Max(0, costs[0]);
            _hud.ability2Cost = Mathf.Max(0, costs[1]);
            _hud.ability3Cost = Mathf.Max(0, costs[2]);
            _hud.ability4Cost = Mathf.Max(0, costs[3]);
            _hud.ability5Cost = Mathf.Max(0, costs[4]);

            if (!Application.isPlaying)
                EditorUtility.SetDirty(_hud);
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "El costo de desbloqueo solo se lee al armar el HUD (OnEnable): " +
                "un cambio en Play Mode no se nota hasta la proxima vez que se " +
                "abra la escena.",
                MessageType.None);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Resetear cooldowns"))
            _hud.DebugResetAllCooldowns();
        if (GUILayout.Button("Desbloquear todas las ranuras"))
            _hud.DebugUnlockAllAbilities();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCatalogField()
    {
        EditorGUILayout.LabelField("Catalogo", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _catalog = (TowerCatalog)EditorGUILayout.ObjectField(
            "Tower Catalog", _catalog, typeof(TowerCatalog), false);
        if (GUILayout.Button("Buscar", GUILayout.Width(60f)))
            _catalog = FindCatalog();
        EditorGUILayout.EndHorizontal();
    }

    // Una fila por nivel: alcance, dano y cadencia, todo editable.
    // El alcance lleva slider porque es el valor que mas se toquetea.
    private void DrawLevelTable()
    {
        EditorGUILayout.LabelField("Balance por nivel", EditorStyles.boldLabel);
        _maxSliderRange = Mathf.Max(1f, EditorGUILayout.FloatField("Tope del slider", _maxSliderRange));
        EditorGUILayout.Space();

        for (int i = 0; i < _catalog.levels.Length; i++)
        {
            TowerData data = _catalog.levels[i];
            if (data == null)
            {
                EditorGUILayout.HelpBox("El nivel " + (i + 1) + " del catalogo esta vacio.", MessageType.Warning);
                continue;
            }

            DrawLevelRow(i + 1, data);
        }
    }

    private void DrawLevelRow(int level, TowerData data)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        // Cuadrito con el color que usa el tablero para ese nivel, para
        // reconocer de un vistazo que circulo de la escena es cual.
        Rect swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
        swatch.y += 2f;
        EditorGUI.DrawRect(swatch, ColorForLevel(level));
        EditorGUILayout.LabelField("Nivel " + level, EditorStyles.boldLabel);
        if (GUILayout.Button("Ver asset", GUILayout.Width(75f)))
            EditorGUIUtility.PingObject(data);
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();

        float range = EditorGUILayout.Slider("Alcance", data.range, 0f, _maxSliderRange);
        float damage = EditorGUILayout.FloatField("Dano", data.damage);
        float attackRate = EditorGUILayout.FloatField("Disparos por segundo", data.attackRate);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Cambiar balance de la torre");
            data.range = Mathf.Max(0f, range);
            data.damage = Mathf.Max(0f, damage);
            data.attackRate = Mathf.Max(0f, attackRate);
            EditorUtility.SetDirty(data);

            ApplyToSceneTowers();
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSceneOptions()
    {
        EditorGUILayout.LabelField("Vista de escena", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _drawRanges = EditorGUILayout.Toggle("Dibujar alcances", _drawRanges);
        EditorGUI.BeginDisabledGroup(!_drawRanges);
        _drawOnlySelected = EditorGUILayout.Toggle("Solo la seleccionada", _drawOnlySelected);
        EditorGUI.EndDisabledGroup();
        if (EditorGUI.EndChangeCheck())
            SceneView.RepaintAll();

        EditorGUILayout.HelpBox(
            "Para arrastrar el alcance con el raton, selecciona una torre en la " +
            "escena: aparece un circulo con tiradores.",
            MessageType.Info);
    }

    // Lista de las torres que hay ahora mismo en la escena con el alcance
    // que estan usando de verdad. En Play Mode esto es lo que manda: si un
    // numero no coincide con el catalogo, esa torre no se refresco.
    private void DrawSceneTowers()
    {
        List<TowerAttack> towers = FindSceneTowers();

        EditorGUILayout.LabelField("Torres en la escena (" + towers.Count + ")", EditorStyles.boldLabel);

        if (towers.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay torres con TowerAttack en la escena.", MessageType.Info);
            return;
        }

        foreach (TowerAttack attack in towers)
        {
            EditorGUILayout.BeginHorizontal();

            Tower tower = attack.GetComponent<Tower>();
            int level = tower != null ? tower.Level : 1;

            Rect swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
            swatch.y += 2f;
            EditorGUI.DrawRect(swatch, ColorForLevel(level));

            EditorGUILayout.LabelField(attack.gameObject.name, GUILayout.MinWidth(80f));
            EditorGUILayout.LabelField("Nv " + level, GUILayout.Width(40f));
            EditorGUILayout.LabelField("Alcance " + attack.range.ToString("0.##"), GUILayout.Width(95f));

            if (GUILayout.Button("Ir", GUILayout.Width(30f)))
            {
                Selection.activeGameObject = attack.gameObject;
                SceneView.FrameLastActiveSceneView();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Aplicar el catalogo a las torres de la escena"))
        {
            ApplyToSceneTowers();
            SceneView.RepaintAll();
        }
    }

    // Circulos de alcance de todas las torres, con el color de su nivel.
    private void DrawSceneRanges(SceneView sceneView)
    {
        if (!_drawRanges || _catalog == null)
            return;

        foreach (TowerAttack attack in FindSceneTowers())
        {
            if (_drawOnlySelected && !Selection.Contains(attack.gameObject))
                continue;

            Tower tower = attack.GetComponent<Tower>();
            int level = tower != null ? tower.Level : 1;

            Handles.color = ColorForLevel(level);
            Handles.DrawWireDisc(attack.transform.position, Vector3.up, attack.range);
        }
    }

    // Aplica los valores del catalogo a las torres que ya existen.
    // Fuera de Play Mode tambien sirve: deja el Inspector al dia.
    private void ApplyToSceneTowers()
    {
        foreach (TowerAttack attack in FindSceneTowers())
        {
            attack.ForceRefreshStats();
            if (!Application.isPlaying)
                EditorUtility.SetDirty(attack);
        }
    }

    private static List<TowerAttack> FindSceneTowers()
    {
        var towers = new List<TowerAttack>();
        TowerAttack[] found = Object.FindObjectsByType<TowerAttack>(FindObjectsSortMode.None);

        foreach (TowerAttack attack in found)
        {
            // Se saltan los prefabs abiertos en modo aislado: aqui interesan
            // las torres que estan de verdad en el nivel.
            if (attack.gameObject.scene.IsValid())
                towers.Add(attack);
        }

        return towers;
    }

    private static TowerCatalog FindCatalog()
    {
        string[] guids = AssetDatabase.FindAssets("t:TowerCatalog");
        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<TowerCatalog>(path);
    }

    // Se reutiliza la paleta del tablero para que el circulo de la escena
    // tenga el mismo color que la torre. Sin tablero se usa un blanco.
    public static Color ColorForLevel(int level)
    {
        BoardManager board = Object.FindFirstObjectByType<BoardManager>();
        Color color = board != null ? board.TowerColorForLevel(level) : Color.white;
        color.a = 1f;
        return color;
    }
}
