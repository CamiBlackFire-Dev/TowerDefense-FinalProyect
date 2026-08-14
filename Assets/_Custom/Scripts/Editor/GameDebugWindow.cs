using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Ventana de debug del juego. Hoy junta dos cosas:
//  - Balance de torres: ver y ajustar el alcance (y el resto de las
//    estadisticas) sin tener que buscar los assets a mano.
//  - Velocidad del juego: probar el juego a x2/x3 sin tocar codigo (usa
//    GameSpeedController, el mismo punto que usara mas adelante un boton
//    real del HUD).
// Se abre desde el menu Tower Defense > Game Debug.
public class GameDebugWindow : EditorWindow
{
    private TowerCatalog _catalog;
    private float _maxSliderRange = 30f;   // tope del slider, subelo si te queda corto
    private bool _drawRanges = true;       // circulos de alcance en la vista de escena
    private bool _drawOnlySelected;        // solo la torre seleccionada
    private Vector2 _scroll;

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

        SceneView.duringSceneGui += DrawSceneRanges;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawSceneRanges;
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
