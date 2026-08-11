using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TowerDefense
{
    // Conecta BoardGrid con los objetos visibles de la escena.
    // ExecuteAlways permite ver y editar el tablero sin entrar en Play Mode.
    [ExecuteAlways]
    public class BoardManager : MonoBehaviour
    {
        [Header("Tablero")]
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private float cellThickness = 0.1f;
        [TextArea(2, 5)]
        [SerializeField] private string initialBoard = "";

        [Header("Visuales")]
        [SerializeField] private Material cellMaterial;
        [SerializeField] private GameObject towerPrefab;

        [Header("Animacion")]
        [SerializeField] private float moveDuration = 0.14f;      // duracion del deslizamiento
        [SerializeField] private float mergeFeedbackDuration = 0.22f; // duracion del pulso de fusion
        [SerializeField] private AnimationCurve moveCurve;        // suavizado del deslizamiento

        [Header("Input")]
        [SerializeField] private InputController inputController;

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

        // Reconstruye la vista persistente de la cuadrícula en la escena.
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

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
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
        // Mientras la animacion corre se ignoran nuevos movimientos.
        public MoveResult Move(GridDirection direction)
        {
            EnsureInitialized();
            if (_isAnimating)
                return new MoveResult(false, 0,
                    new List<TowerMoveEvent>(), new List<TowerMergeEvent>());

            MoveResult result = _grid.Move(direction);
            if (!result.Changed)
                return result;

            if (Application.isPlaying)
                StartCoroutine(AnimateMove(result));
            else
                UpdateTowers();

            return result;
        }

        // Anima un movimiento: primero deslizan todas las torres y luego
        // se aplican las fusiones con su feedback.
        private IEnumerator AnimateMove(MoveResult result)
        {
            _isAnimating = true;

            var sliding = new List<(Tower tower, (int x, int y) to, Vector3 target)>();
            var merging = new List<(Tower survivor, Tower consumed, (int x, int y) to, Vector3 target)>();

            foreach (TowerMergeEvent merge in result.Merges)
            {
                Tower survivor = _towers[merge.FirstSource].GetComponent<Tower>();
                Tower consumed = _towers[merge.SecondSource].GetComponent<Tower>();
                Vector3 target = survivor.GetPositionOnCell(
                    merge.To.x, merge.To.y, TowerPosition(merge.To.x, merge.To.y));
                merging.Add((survivor, consumed, merge.To, target));
            }

            foreach (TowerMoveEvent move in result.Moves)
            {
                Tower tower = _towers[move.From].GetComponent<Tower>();
                Vector3 target = tower.GetPositionOnCell(
                    move.To.x, move.To.y, TowerPosition(move.To.x, move.To.y));
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

            // Fase 2: las fusiones cambian de nivel, pulsan y destruyen la consumida.
            foreach (var item in merging)
            {
                item.survivor.PlayMergeFeedback(item.survivor.Level + 1, mergeFeedbackDuration);
                item.survivor.PositionOnCell(item.to.x, item.to.y,
                    TowerPosition(item.to.x, item.to.y));
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

            yield return new WaitForSeconds(mergeFeedbackDuration);
            _isAnimating = false;
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
                {
                    Vector3 position = CellToWorld(x, y);
                    position.y = BoardBaseY + cellThickness * 0.5f;

                    GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cell.name = "Cell " + x + "," + y;
                    cell.transform.SetParent(_cellContainer, false);
                    cell.transform.localPosition = position;
                    cell.transform.localScale = new Vector3(
                        cellSize * 0.9f,
                        cellThickness,
                        cellSize * 0.9f);

                    Renderer renderer = cell.GetComponent<Renderer>();
                    if (renderer != null && cellMaterial != null)
                        renderer.sharedMaterial = cellMaterial;

                    _cells[(x, y)] = cell;
                }
            }
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
                    tower.SetLevel(level);
                    tower.PositionOnCell(x, y, TowerPosition(x, y));
                    return;
                }
            }

            CreateTowerVisual(x, y, level, false);
        }

        // Crea la torre visual de una casilla. Si animateSpawn esta activo,
        // la torre aparece en Play Mode con su animacion de crecimiento.
        // El prefab solo necesita el componente Tower: si no hay modelo,
        // TowerVisual crea un cubo; si lo hay, se usa tal cual.
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

            towerScript.SetLevel(level);
            towerScript.PositionOnCell(x, y, TowerPosition(x, y));

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
}
