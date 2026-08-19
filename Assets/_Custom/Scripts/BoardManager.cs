using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DamageNumbersPro;

// Como se ubican las casillas del tablero. Procedural: BoardManager genera
// sus propios cubos centrados en el origen (comportamiento de siempre).
// Anchored: usa Transforms ya puestos en la escena (por ejemplo bases
// redondas de un mapa hecho a mano) como posicion de cada casilla.
public enum BoardMode
{
    Procedural,
    Anchored
}

// Conecta BoardGrid con los objetos visibles de la escena.
// ExecuteAlways permite ver y editar el tablero sin entrar en Play Mode.
[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [Header("Modo")]
    public BoardMode mode = BoardMode.Procedural;

    [Header("Tablero")]
    [TextArea(2, 5)]
    public string initialBoard = "";        // niveles iniciales, filas separadas por |
    public int boardWidth = BoardGrid.DefaultSize;
    public int boardHeight = BoardGrid.DefaultSize;

    [Header("Casillas ancladas")]
    // Solo en modo Anchored. El orden no importa: el ancho y el alto del
    // tablero se deducen solos agrupando estos Transforms por X y por Z
    // (posicion local respecto de este mismo GameObject).
    public Transform[] anchoredCells;

    [Header("Visuales")]
    public float cellSize = 2f;             // distancia entre los centros de dos casillas
    public float cellThickness = 0.1f;      // alto de la casilla
    public Mesh cellMesh;                   // modelo de la casilla (vacio = cubo)
    public Material cellMaterial;           // material de la casilla
    public GameObject towerPrefab;          // prefab base de la torre (opcional)
    public GameObject[] towerLevelModels;   // modelo por nivel: [0] nivel 1, [1] nivel 2, ...

    [Header("Colores de las torres")]
    public bool useLevelColors = true;      // apagar para dejar el color original del modelo
    // Color superpuesto de cada nivel: [0] nivel 1, [1] nivel 2, ...
    // Si un nivel se pasa de la lista se repite el ultimo color.
    public Color[] towerLevelColors =
    {
        new Color(0.3f, 0.6f, 1f),  // nivel 1: azul
        new Color(0.3f, 1f, 0.5f),  // nivel 2: verde
        new Color(1f, 0.7f, 0.2f),  // nivel 3: naranja
        new Color(1f, 0.3f, 0.3f),  // nivel 4: rojo
        new Color(0.8f, 0.4f, 1f),  // nivel 5: morado
        new Color(1f, 1f, 1f),      // nivel 6 o mas: blanco
    };

    [Header("Combate")]
    public bool towersAttack = true;    // las torres disparan solas a los enemigos
    // Tamano del collider que hace visible a la torre para los enemigos
    // que disparan. Solo sirve para que la detecten, no bloquea nada.
    public float towerTargetRadius = 0.5f;
    public TowerCatalog towerCatalog;   // dano, alcance y cadencia de cada nivel
    public GameObject projectilePrefab; // bala visible de las torres (bola de canon)
    public DamageNumber damagePopup;    // numero de dano al pegar (Damage Numbers Pro)

    [Header("Animacion")]
    public float moveDuration = 0.14f;          // duracion del deslizamiento
    public float mergeFeedbackDuration = 0.22f; // duracion del pulso de fusion
    public AnimationCurve moveCurve;            // suavizado del deslizamiento
    public int maxBufferedMoves = 8;            // movimientos encolados maximo

    [Header("Input")]
    public InputController inputController;

    [Header("Oleadas")]
    // Se busca solo en la escena si se deja vacio. Mientras spawner.IsRunning
    // sea true el tablero queda bloqueado: solo se puede reordenar entre oleadas.
    public EnemySpawner spawner;

    [Header("Desbloqueo temporal")]
    // Cuanto dura el desbloqueo del powerup que permite editar el tablero
    // aunque haya una oleada en curso (ver UnlockTemporarily).
    public float temporaryUnlockDuration = 30f;

    // El Ground actual tiene la cara superior aproximadamente en y = 0.5.
    private const float BoardBaseY = 0.5f;

    private BoardGrid _grid;
    private Transform _cellContainer;
    private Transform _towerContainer;
    private bool _isRebuilding;
    private bool _isAnimating;
    private Vector3[,] _anchorLocalPositions;
    private float _temporaryUnlockTimer;
    private bool _wasRunning;
    private bool _wasLocked;

    // Avisa cada vez que el tablero pasa de bloqueado a desbloqueado o al
    // reves (por una oleada empezando/terminando, o por el desbloqueo
    // temporal empezando/venciendo). El feedback visual del tablero
    // (BoardLockFeedback) escucha esto en vez de mirar spawner.IsRunning
    // por su cuenta, para no duplicar la logica de cuando esta bloqueado.
    public event Action<bool> LockStateChanged;

    // True si el tablero no se puede reordenar ahora mismo: hay una oleada
    // en curso y no esta activo el desbloqueo temporal del powerup.
    public bool IsLocked
    {
        get { return spawner != null && spawner.IsRunning && _temporaryUnlockTimer <= 0f; }
    }

    // Segundos que quedan del desbloqueo temporal (0 si no esta activo).
    public float TemporaryUnlockRemaining
    {
        get { return _temporaryUnlockTimer; }
    }

    private readonly Dictionary<(int x, int y), GameObject> _cells =
        new Dictionary<(int x, int y), GameObject>();
    private readonly Dictionary<(int x, int y), GameObject> _towers =
        new Dictionary<(int x, int y), GameObject>();
    private readonly Queue<GridDirection> _pendingMoves = new Queue<GridDirection>();

    public BoardGrid Grid
    {
        get
        {
            EnsureInitialized();
            return _grid;
        }
    }

    public int GridWidth
    {
        get { return boardWidth; }
    }

    public int GridHeight
    {
        get { return boardHeight; }
    }

    // Cantidad de movimientos encolados esperando su turno.
    public int PendingMoveCount
    {
        get { return _pendingMoves.Count; }
    }

    // True mientras el tablero esta animando un movimiento.
    public bool IsAnimating
    {
        get { return _isAnimating; }
    }

    private void Awake()
    {
        if (Application.isPlaying)
            EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        SubscribeToInput();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        // Una oleada nueva empezando limpia el desbloqueo temporal de la
        // anterior: no se arrastra de una oleada a la siguiente.
        bool running = spawner != null && spawner.IsRunning;
        if (running && !_wasRunning)
            _temporaryUnlockTimer = 0f;
        _wasRunning = running;

        if (_temporaryUnlockTimer > 0f)
        {
            _temporaryUnlockTimer -= Time.deltaTime;
            if (_temporaryUnlockTimer < 0f)
                _temporaryUnlockTimer = 0f;
        }

        NotifyLockStateIfChanged();
    }

    // El powerup del HUD llama esto para poder reordenar el tablero aunque
    // la oleada siga en curso, durante temporaryUnlockDuration segundos.
    public void UnlockTemporarily()
    {
        _temporaryUnlockTimer = Mathf.Max(_temporaryUnlockTimer, temporaryUnlockDuration);
        NotifyLockStateIfChanged();
    }

    private void NotifyLockStateIfChanged()
    {
        bool locked = IsLocked;
        if (locked == _wasLocked)
            return;

        _wasLocked = locked;
        if (LockStateChanged != null)
            LockStateChanged(locked);
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    // Inicializa el tablero y reutiliza los objetos ya guardados en la escena.
    private void EnsureInitialized()
    {
        if (_grid != null && _cellContainer != null && _towerContainer != null)
            return;

        if (mode == BoardMode.Anchored)
            BuildAnchorGrid();

        _grid = ParseBoard(initialBoard);
        EnsureContainers();
        CacheExistingVisuals();

        if (moveCurve == null)
            moveCurve = DefaultMoveCurve();

        if (spawner == null && Application.isPlaying)
            spawner = FindFirstObjectByType<EnemySpawner>();

        if (!HasCompleteVisualView())
            RebuildBoardView();
        else
            UpdateTowers();
    }

    // Agrupa anchoredCells por X y por Z (con tolerancia) para deducir el
    // ancho y el alto del tablero sin que el orden del array importe.
    private void BuildAnchorGrid()
    {
        var valid = new List<Transform>();
        if (anchoredCells != null)
        {
            foreach (Transform t in anchoredCells)
                if (t != null)
                    valid.Add(t);
        }

        if (valid.Count == 0)
        {
            Debug.LogWarning("BoardManager: modo Anchored sin celdas en anchoredCells.", this);
            boardWidth = 0;
            boardHeight = 0;
            _anchorLocalPositions = new Vector3[0, 0];
            return;
        }

        List<float> columnsX = DistinctSorted(valid, t => t.localPosition.x);
        List<float> rowsZ = DistinctSorted(valid, t => t.localPosition.z);

        boardWidth = columnsX.Count;
        boardHeight = rowsZ.Count;
        _anchorLocalPositions = new Vector3[boardWidth, boardHeight];

        foreach (Transform t in valid)
        {
            int x = ClosestIndex(columnsX, t.localPosition.x);
            int y = ClosestIndex(rowsZ, t.localPosition.z);
            _anchorLocalPositions[x, y] = t.localPosition;
        }
    }

    // Junta valores parecidos (misma columna o fila) y los deja de menor a mayor.
    private static List<float> DistinctSorted(List<Transform> items, Func<Transform, float> selector)
    {
        const float tolerance = 0.5f;
        var values = new List<float>();
        foreach (Transform t in items)
        {
            float value = selector(t);
            bool found = false;
            for (int i = 0; i < values.Count; i++)
            {
                if (Mathf.Abs(values[i] - value) < tolerance)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                values.Add(value);
        }
        values.Sort();
        return values;
    }

    // Indice del valor mas cercano dentro de una lista ya ordenada.
    private static int ClosestIndex(List<float> sortedValues, float value)
    {
        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < sortedValues.Count; i++)
        {
            float distance = Mathf.Abs(sortedValues[i] - value);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    // Convierte una casilla a una posicion local dentro de Board.
    // En modo Anchored devuelve la posicion del Transform que le toca.
    public Vector3 CellToWorld(int x, int y)
    {
        if (mode == BoardMode.Anchored)
        {
            if (_anchorLocalPositions == null || x < 0 || x >= boardWidth || y < 0 || y >= boardHeight)
                return Vector3.zero;
            return _anchorLocalPositions[x, y];
        }

        float worldX = (x - (boardWidth - 1) / 2f) * cellSize;
        float worldZ = (y - (boardHeight - 1) / 2f) * cellSize;
        return new Vector3(worldX, 0f, worldZ);
    }

    // Devuelve el nivel que se esta editando en una casilla.
    public int GetEditorLevel(int x, int y)
    {
        return Grid.GetLevel(x, y);
    }

    // Cambia el nivel desde el inspector y guarda el nuevo estado del tablero.
    public void SetEditorLevel(int x, int y, int level)
    {
        EnsureInitialized();
        _grid.SetLevel(x, y, Mathf.Max(0, level));
        initialBoard = SerializeBoard(_grid);
        RebuildBoardView();
    }

    // Reconstruye la vista persistente de la cuadricula en la escena.
    public void RebuildBoardView()
    {
        if (_isRebuilding)
            return;

        _isRebuilding = true;
        try
        {
            if (mode == BoardMode.Anchored)
                BuildAnchorGrid();

            _grid = ParseBoard(initialBoard);
            EnsureContainers();
            ClearContainer(_cellContainer);
            ClearContainer(_towerContainer);
            _cells.Clear();
            _towers.Clear();

            // En modo Anchored las casillas ya existen en la escena (las puso
            // el mapa a mano): no se generan cubos propios, solo las torres.
            if (mode == BoardMode.Procedural)
                CreateCells();
            CreateTowersFromGrid();
        }
        finally
        {
            _isRebuilding = false;
        }
    }

    // Coloca una torre durante el juego. Las compras usaran este metodo.
    // En Play Mode la torre aparece con su animacion de spawn.
    public bool PlaceTower(int x, int y, int level)
    {
        EnsureInitialized();
        if (!_grid.IsEmpty(x, y) || level <= 0)
            return false;

        _grid.SetLevel(x, y, level);
        CreateTowerVisual(x, y, level, Application.isPlaying);
        return true;
    }

    // Ejecuta un movimiento estilo 2048.
    // En Play Mode anima los desplazamientos y las fusiones; fuera de el
    // (por ejemplo en pruebas de editor) actualiza las torres al instante.
    // Si ya hay una animacion corriendo, el movimiento se encola para no
    // perder las pulsaciones rapidas del jugador.
    public MoveResult Move(GridDirection direction)
    {
        EnsureInitialized();

        // Con una oleada en curso (y sin el desbloqueo temporal activo) no
        // se puede reordenar el tablero: el movimiento se ignora por
        // completo (no se encola ni se recuerda).
        if (IsLocked)
            return new MoveResult(false, 0,
                new List<TowerMoveEvent>(), new List<TowerMergeEvent>());

        // Con la animacion activa se encola y se devuelve un resultado vacio.
        if (_isAnimating)
        {
            if (_pendingMoves.Count < maxBufferedMoves)
                _pendingMoves.Enqueue(direction);
            return new MoveResult(false, 0,
                new List<TowerMoveEvent>(), new List<TowerMergeEvent>());
        }

        MoveResult result = _grid.Move(direction);
        if (!result.Changed)
            return result;

        if (Application.isPlaying)
        {
            _isAnimating = true;
            StartCoroutine(ProcessMoveQueue(result));
        }
        else
        {
            UpdateTowers();
        }

        return result;
    }

    // Modelo que le toca a un nivel. Devuelve null si la lista esta vacia,
    // y en ese caso la torre se queda con el modelo que ya trae el prefab.
    public GameObject TowerModelForLevel(int level)
    {
        if (towerLevelModels == null || towerLevelModels.Length == 0)
            return null;

        int index = Mathf.Clamp(level - 1, 0, towerLevelModels.Length - 1);
        return towerLevelModels[index];
    }

    // Color superpuesto que le toca a un nivel.
    public Color TowerColorForLevel(int level)
    {
        if (towerLevelColors == null || towerLevelColors.Length == 0)
            return Color.white;

        int index = Mathf.Clamp(level - 1, 0, towerLevelColors.Length - 1);
        return towerLevelColors[index];
    }

    // Pasa a la torre los colores elegidos en el tablero.
    // La torre no guarda su propia paleta: siempre usa esta.
    private void ApplyTowerColors(Tower tower)
    {
        tower.Visual.SetLevelColors(towerLevelColors, useLevelColors);
    }

    // Le pone a la torre el script de disparo, para que ataque sola.
    // TowerAttack trae consigo el detector de enemigos de Jean.
    private void ApplyTowerAttack(Tower tower)
    {
        if (!towersAttack)
            return;

        TowerAttack attack = tower.GetComponent<TowerAttack>();
        if (attack == null)
            attack = tower.gameObject.AddComponent<TowerAttack>();

        attack.catalog = towerCatalog;
        attack.projectilePrefab = projectilePrefab;
        attack.popupPrefab = damagePopup;
    }

    // Le da vida a la torre (base para que en el futuro los enemigos puedan
    // atacarlas). A diferencia del ataque, esto no depende de towersAttack:
    // la vida es un dato propio de la torre, no de si dispara sola.
    private void ApplyTowerHealth(Tower tower)
    {
        TowerHealth health = tower.GetComponent<TowerHealth>();
        if (health == null)
            health = tower.gameObject.AddComponent<TowerHealth>();

        health.catalog = towerCatalog;

        // Se saca y se vuelve a poner para no quedar escuchando dos veces
        // si esta torre ya tenia el componente (por ejemplo al refrescar el
        // tablero en el editor).
        health.Depleted -= HandleTowerDepleted;
        health.Depleted += HandleTowerDepleted;
    }

    // Deja la torre en la capa "Tower" y con un collider, que es como la
    // encuentran los enemigos que disparan (EnemyTargetDetector usa un
    // OverlapSphere sobre esa capa). Sin esto las torres son invisibles
    // para ellos: el modelo por si solo no trae collider.
    // El collider es solo para que la detecten; nada empuja a la torre,
    // asi que no hace falta Rigidbody.
    private void ApplyTowerTargetable(Tower tower)
    {
        int towerLayer = LayerMask.NameToLayer("Tower");
        if (towerLayer >= 0)
            tower.gameObject.layer = towerLayer;

        SphereCollider collider = tower.GetComponent<SphereCollider>();
        if (collider == null)
            collider = tower.gameObject.AddComponent<SphereCollider>();

        collider.isTrigger = true;
        collider.radius = towerTargetRadius;
        collider.center = new Vector3(0f, towerTargetRadius, 0f);
    }

    // La vida de una torre llego a 0: si tiene mas de un nivel, baja uno y
    // recupera la vida llena de ese nivel; si ya estaba en el nivel 1, la
    // casilla queda vacia como si nunca hubiera habido una torre ahi.
    private void HandleTowerDepleted(TowerHealth health)
    {
        Tower tower = health.GetComponent<Tower>();
        if (tower == null)
            return;

        (int x, int y)? key = FindTowerCoordinates(tower);
        if (!key.HasValue)
            return;

        int newLevel = tower.Level - 1;
        if (newLevel <= 0)
        {
            _grid.SetLevel(key.Value.x, key.Value.y, 0);
            DestroyTowerVisual(tower);
            return;
        }

        _grid.SetLevel(key.Value.x, key.Value.y, newLevel);
        tower.SetModel(TowerModelForLevel(newLevel));
        ApplyTowerColors(tower);
        tower.SetLevel(newLevel);
        tower.PositionOnCell(TowerPosition(key.Value.x, key.Value.y));
        health.ForceRefreshStats();
    }

    // Procesa en orden el movimiento actual y todos los encolados.
    // Los movimientos que no cambian el tablero se consumen sin animar.
    private IEnumerator ProcessMoveQueue(MoveResult firstResult)
    {
        yield return StartCoroutine(AnimateMove(firstResult));

        while (_pendingMoves.Count > 0)
        {
            GridDirection direction = _pendingMoves.Dequeue();
            MoveResult result = _grid.Move(direction);
            if (!result.Changed)
                continue;

            yield return StartCoroutine(AnimateMove(result));
        }

        _isAnimating = false;
    }

    // Anima un movimiento: primero deslizan todas las torres y luego
    // se aplican las fusiones con su feedback.
    // Solo espera el deslizamiento: el pulso de fusion sigue en paralelo
    // y no bloquea al jugador para mover de nuevo.
    private IEnumerator AnimateMove(MoveResult result)
    {
        var sliding = new List<(Tower tower, (int x, int y) to, Vector3 target)>();
        var merging = new List<(Tower survivor, Tower consumed, (int x, int y) to, Vector3 target)>();

        foreach (TowerMergeEvent merge in result.Merges)
        {
            Tower survivor = _towers[merge.FirstSource].GetComponent<Tower>();
            Tower consumed = _towers[merge.SecondSource].GetComponent<Tower>();
            Vector3 target = survivor.GetPositionOnCell(TowerPosition(merge.To.x, merge.To.y));
            merging.Add((survivor, consumed, merge.To, target));
        }

        foreach (TowerMoveEvent move in result.Moves)
        {
            Tower tower = _towers[move.From].GetComponent<Tower>();
            Vector3 target = tower.GetPositionOnCell(TowerPosition(move.To.x, move.To.y));
            sliding.Add((tower, move.To, target));
        }

        // Fase 1: todas las torres se deslizan hacia su destino.
        foreach (var item in sliding)
            item.tower.AnimateToLocalPosition(item.target, moveDuration, moveCurve, null);
        foreach (var item in merging)
        {
            item.survivor.AnimateToLocalPosition(item.target, moveDuration, moveCurve, null);
            item.consumed.AnimateToLocalPosition(item.target, moveDuration, moveCurve, null);
        }

        yield return new WaitForSeconds(moveDuration);

        // Fase 2: las fusiones suben de nivel, cambian de modelo, pulsan
        // y destruyen la torre consumida.
        foreach (var item in merging)
        {
            int newLevel = item.survivor.Level + 1;
            item.survivor.SetModel(TowerModelForLevel(newLevel));
            item.survivor.PlayMergeFeedback(newLevel, mergeFeedbackDuration);
            item.survivor.PositionOnCell(TowerPosition(item.to.x, item.to.y));
            DestroyTowerVisual(item.consumed);
        }

        // Fase 3: el diccionario pasa a las posiciones finales.
        foreach (TowerMoveEvent move in result.Moves)
            _towers.Remove(move.From);
        foreach (TowerMergeEvent merge in result.Merges)
        {
            _towers.Remove(merge.FirstSource);
            _towers.Remove(merge.SecondSource);
        }
        foreach (var item in sliding)
            _towers[item.to] = item.tower.gameObject;
        foreach (var item in merging)
            _towers[item.to] = item.survivor.gameObject;

        // No se espera el pulso de fusion: el siguiente movimiento puede
        // comenzar ya mientras el pulso termina por su cuenta.
    }

    // Curva por defecto: arranque rapido y frenado suave (ease-out).
    private static AnimationCurve DefaultMoveCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 0.85f),
            new Keyframe(1f, 1f));
    }

    // Coloca una torre en una celda libre aleatoria. Devuelve false si no hay sitio.
    public bool PlaceTowerRandom(int level)
    {
        EnsureInitialized();
        List<(int x, int y)> freeCells = _grid.GetFreeCells();
        if (freeCells.Count == 0)
            return false;

        int index = UnityEngine.Random.Range(0, freeCells.Count);
        (int x, int y) cell = freeCells[index];
        return PlaceTower(cell.x, cell.y, level);
    }

    // Devuelve la torre visual que esta en una casilla.
    public bool TryGetTower(int x, int y, out Tower tower)
    {
        EnsureInitialized();
        if (_towers.TryGetValue((x, y), out GameObject towerObject) && towerObject != null)
        {
            tower = towerObject.GetComponent<Tower>();
            return tower != null;
        }

        tower = null;
        return false;
    }

    private void SubscribeToInput()
    {
        if (!Application.isPlaying || inputController == null)
            return;

        inputController.MoveRequested -= HandleMoveRequested;
        inputController.MoveRequested += HandleMoveRequested;
    }

    private void UnsubscribeFromInput()
    {
        if (inputController != null)
            inputController.MoveRequested -= HandleMoveRequested;
    }

    private void HandleMoveRequested(GridDirection direction)
    {
        Move(direction);
    }

    private void EnsureContainers()
    {
        _cellContainer = transform.Find("Cells");
        if (_cellContainer == null)
        {
            GameObject cells = new GameObject("Cells");
            cells.transform.SetParent(transform, false);
            _cellContainer = cells.transform;
        }

        _towerContainer = transform.Find("Towers");
        if (_towerContainer == null)
        {
            GameObject towers = new GameObject("Towers");
            towers.transform.SetParent(transform, false);
            _towerContainer = towers.transform;
        }
    }

    private void CacheExistingVisuals()
    {
        _cells.Clear();
        _towers.Clear();

        for (int i = 0; i < _cellContainer.childCount; i++)
        {
            Transform child = _cellContainer.GetChild(i);
            if (TryParseCoordinate(child.name, "Cell ", out int x, out int y))
                _cells[(x, y)] = child.gameObject;
        }

        for (int i = 0; i < _towerContainer.childCount; i++)
        {
            Transform child = _towerContainer.GetChild(i);
            if (TryParseCoordinate(child.name, "Tower ", out int x, out int y) &&
                child.GetComponent<Tower>() != null)
            {
                _towers[(x, y)] = child.gameObject;
            }
        }
    }

    private bool HasCompleteVisualView()
    {
        // En modo Anchored no hay cubos propios que contar: las casillas
        // las puso el mapa a mano, asi que _cells se queda vacio a proposito.
        if (mode == BoardMode.Procedural && _cells.Count != GridWidth * GridHeight)
            return false;

        int occupiedCells = 0;
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (_grid.GetLevel(x, y) <= 0)
                    continue;

                occupiedCells++;
                if (!_towers.ContainsKey((x, y)))
                    return false;
            }
        }

        return _towers.Count == occupiedCells;
    }

    private void CreateCells()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
                _cells[(x, y)] = CreateCell(x, y);
        }
    }

    // Crea una casilla visible. Cambiar cellMesh no cambia el tamano del
    // tablero: el modelo siempre se ajusta al hueco de la casilla.
    private GameObject CreateCell(int x, int y)
    {
        GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cell.name = "Cell " + x + "," + y;
        cell.transform.SetParent(_cellContainer, false);

        // Sin modelo propio se queda el cubo que trae el primitivo.
        MeshFilter filter = cell.GetComponent<MeshFilter>();
        if (cellMesh != null)
            filter.sharedMesh = cellMesh;

        // Hueco que ocupa la casilla: algo mas chica que cellSize para que
        // se vea la separacion entre casillas.
        Vector3 cellBox = new Vector3(cellSize * 0.9f, cellThickness, cellSize * 0.9f);
        Bounds meshBounds = filter.sharedMesh.bounds;
        Vector3 scale = FitScale(meshBounds.size, cellBox);
        cell.transform.localScale = scale;

        // La casilla se centra en su sitio aunque el pivote del modelo no lo este.
        Vector3 position = CellToWorld(x, y);
        position.y = BoardBaseY + cellThickness * 0.5f;
        cell.transform.localPosition = position - Vector3.Scale(meshBounds.center, scale);

        // El collider copia los limites del modelo para que siga cubriendo la casilla.
        BoxCollider collider = cell.GetComponent<BoxCollider>();
        collider.center = meshBounds.center;
        collider.size = meshBounds.size;

        if (cellMaterial != null)
            cell.GetComponent<Renderer>().sharedMaterial = cellMaterial;

        return cell;
    }

    // Escala que necesita un modelo para ocupar exactamente el tamano pedido.
    private static Vector3 FitScale(Vector3 meshSize, Vector3 target)
    {
        return new Vector3(
            meshSize.x > 0f ? target.x / meshSize.x : 1f,
            meshSize.y > 0f ? target.y / meshSize.y : 1f,
            meshSize.z > 0f ? target.z / meshSize.z : 1f);
    }

    private void CreateTowersFromGrid()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                int level = _grid.GetLevel(x, y);
                if (level > 0)
                    CreateTowerVisual(x, y, level, false);
            }
        }
    }

    private void UpdateTowers()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                int level = _grid.GetLevel(x, y);
                if (level <= 0)
                    DestroyTowerVisual(x, y);
                else
                    SetOrCreateTowerVisual(x, y, level);
            }
        }
    }

    private void SetOrCreateTowerVisual(int x, int y, int level)
    {
        if (_towers.TryGetValue((x, y), out GameObject towerObject) && towerObject != null)
        {
            Tower tower = towerObject.GetComponent<Tower>();
            if (tower != null)
            {
                tower.SetModel(TowerModelForLevel(level));
                ApplyTowerColors(tower);
                ApplyTowerAttack(tower);
                ApplyTowerHealth(tower);
                ApplyTowerTargetable(tower);
                tower.SetLevel(level);
                tower.PositionOnCell(TowerPosition(x, y));
                return;
            }
        }

        CreateTowerVisual(x, y, level, false);
    }

    // Crea la torre visual de una casilla. Si animateSpawn esta activo,
    // la torre aparece en Play Mode con su animacion de crecimiento.
    // El prefab solo necesita el componente Tower: el modelo sale de
    // towerLevelModels y, si esa lista esta vacia, del propio prefab.
    private void CreateTowerVisual(int x, int y, int level, bool animateSpawn)
    {
        GameObject tower;
        if (towerPrefab != null)
            tower = Instantiate(towerPrefab, _towerContainer, false);
        else
        {
            tower = new GameObject("Tower");
            tower.transform.SetParent(_towerContainer, false);
        }

        tower.name = "Tower " + x + "," + y;

        Tower towerScript = tower.GetComponent<Tower>();
        if (towerScript == null)
            towerScript = tower.AddComponent<Tower>();

        towerScript.SetModel(TowerModelForLevel(level));
        ApplyTowerColors(towerScript);
        ApplyTowerAttack(towerScript);
        ApplyTowerHealth(towerScript);
        ApplyTowerTargetable(towerScript);
        towerScript.SetLevel(level);
        towerScript.PositionOnCell(TowerPosition(x, y));

        if (Application.isPlaying && animateSpawn)
            towerScript.PlaySpawnFeedback();

        _towers[(x, y)] = tower;
    }

    private void DestroyTowerVisual(int x, int y)
    {
        if (!_towers.TryGetValue((x, y), out GameObject tower))
            return;

        _towers.Remove((x, y));
        DestroyObject(tower);
    }

    // Destruye una torre concreta y la quita del diccionario.
    private void DestroyTowerVisual(Tower tower)
    {
        if (tower == null)
            return;

        (int x, int y)? key = FindTowerCoordinates(tower);
        if (key.HasValue)
            _towers.Remove(key.Value);
        DestroyObject(tower.gameObject);
    }

    // Busca en que casilla esta una torre recorriendo el diccionario
    // (no al reves porque _towers esta indexado por casilla, no por torre).
    private (int x, int y)? FindTowerCoordinates(Tower tower)
    {
        foreach (var pair in _towers)
        {
            if (pair.Value == tower.gameObject)
                return pair.Key;
        }

        return null;
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
            DestroyObject(container.GetChild(i).gameObject);
    }

    private void DestroyObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    // Posicion ancla de una casilla: el piso donde descansan los modelos.
    // La altura del modelo la aporta TowerVisual a traves de Tower.
    // En modo Anchored se usa la altura tal cual del marcador (ya puesto a
    // mano sobre el mapa), sin sumarle el grosor de un cubo que no existe.
    private Vector3 TowerPosition(int x, int y)
    {
        Vector3 position = CellToWorld(x, y);
        if (mode == BoardMode.Procedural)
            position.y = BoardBaseY + cellThickness;
        return position;
    }

    private BoardGrid ParseBoard(string description)
    {
        BoardGrid grid = new BoardGrid(Mathf.Max(1, boardWidth), Mathf.Max(1, boardHeight));
        if (string.IsNullOrWhiteSpace(description))
            return grid;

        string[] rows = description.Split('|');
        for (int y = 0; y < rows.Length && y < GridHeight; y++)
        {
            string[] cells = rows[y].Split(',');
            for (int x = 0; x < cells.Length && x < GridWidth; x++)
            {
                int level;
                if (int.TryParse(cells[x], out level))
                    grid.SetLevel(x, y, Mathf.Max(0, level));
            }
        }

        return grid;
    }

    private string SerializeBoard(BoardGrid grid)
    {
        StringBuilder result = new StringBuilder();
        for (int y = 0; y < GridHeight; y++)
        {
            if (y > 0)
                result.Append('|');

            for (int x = 0; x < GridWidth; x++)
            {
                if (x > 0)
                    result.Append(',');
                result.Append(grid.GetLevel(x, y));
            }
        }

        return result.ToString();
    }

    private bool TryParseCoordinate(string objectName, string prefix, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (!objectName.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        string[] values = objectName.Substring(prefix.Length).Split(',');
        if (values.Length != 2)
            return false;

        return int.TryParse(values[0], out x) && int.TryParse(values[1], out y);
    }
}
