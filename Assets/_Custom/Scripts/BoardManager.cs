using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DamageNumbersPro;

// Conecta BoardGrid con los objetos visibles de la escena.
// ExecuteAlways permite ver y editar el tablero sin entrar en Play Mode.
[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [Header("Tablero")]
    [TextArea(2, 5)]
    public string initialBoard = "";        // niveles iniciales, filas separadas por |

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

    // El Ground actual tiene la cara superior aproximadamente en y = 0.5.
    private const float BoardBaseY = 0.5f;

    private BoardGrid _grid;
    private Transform _cellContainer;
    private Transform _towerContainer;
    private bool _isRebuilding;
    private bool _isAnimating;

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

    public int GridSize
    {
        get { return BoardGrid.DefaultSize; }
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

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    // Inicializa el tablero y reutiliza los objetos ya guardados en la escena.
    private void EnsureInitialized()
    {
        if (_grid != null && _cellContainer != null && _towerContainer != null)
            return;

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

    // Convierte una casilla a una posicion local dentro de Board.
    public Vector3 CellToWorld(int x, int y)
    {
        float worldX = (x - 1.5f) * cellSize;
        float worldZ = (y - 1.5f) * cellSize;
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
            _grid = ParseBoard(initialBoard);
            EnsureContainers();
            ClearContainer(_cellContainer);
            ClearContainer(_towerContainer);
            _cells.Clear();
            _towers.Clear();

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

        // Con una oleada en curso no se puede reordenar el tablero: el
        // movimiento se ignora por completo (no se encola ni se recuerda).
        if (spawner != null && spawner.IsRunning)
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
        if (_cells.Count != GridSize * GridSize)
            return false;

        int occupiedCells = 0;
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
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
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
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
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                int level = _grid.GetLevel(x, y);
                if (level > 0)
                    CreateTowerVisual(x, y, level, false);
            }
        }
    }

    private void UpdateTowers()
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
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

        // Se busca la clave que apunta a esta torre.
        (int x, int y)? key = null;
        foreach (var pair in _towers)
        {
            if (pair.Value == tower.gameObject)
            {
                key = pair.Key;
                break;
            }
        }

        if (key.HasValue)
            _towers.Remove(key.Value);
        DestroyObject(tower.gameObject);
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
    private Vector3 TowerPosition(int x, int y)
    {
        Vector3 position = CellToWorld(x, y);
        position.y = BoardBaseY + cellThickness;
        return position;
    }

    private BoardGrid ParseBoard(string description)
    {
        BoardGrid grid = new BoardGrid();
        if (string.IsNullOrWhiteSpace(description))
            return grid;

        string[] rows = description.Split('|');
        for (int y = 0; y < rows.Length && y < GridSize; y++)
        {
            string[] cells = rows[y].Split(',');
            for (int x = 0; x < cells.Length && x < GridSize; x++)
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
        for (int y = 0; y < GridSize; y++)
        {
            if (y > 0)
                result.Append('|');

            for (int x = 0; x < GridSize; x++)
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
