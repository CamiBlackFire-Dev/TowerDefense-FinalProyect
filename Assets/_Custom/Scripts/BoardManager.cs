using System;
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

        [Header("Input")]
        [SerializeField] private InputController inputController;

        // El Ground actual tiene la cara superior aproximadamente en y = 0.5.
        private const float BoardBaseY = 0.5f;

        private BoardGrid _grid;
        private Transform _cellContainer;
        private Transform _towerContainer;
        private bool _isRebuilding;

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
        public bool PlaceTower(int x, int y, int level)
        {
            EnsureInitialized();
            if (!_grid.IsEmpty(x, y) || level <= 0)
                return false;

            _grid.SetLevel(x, y, level);
            SetOrCreateTowerVisual(x, y, level);
            return true;
        }

        // Ejecuta un movimiento 2048 y actualiza las torres visibles.
        public MoveResult Move(GridDirection direction)
        {
            EnsureInitialized();
            MoveResult result = _grid.Move(direction);
            if (result.Changed)
                UpdateTowers();
            return result;
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
                        CreateTowerVisual(x, y, level);
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
                    tower.transform.localPosition = TowerPosition(x, y, tower.transform.localScale.y);
                    return;
                }
            }

            CreateTowerVisual(x, y, level);
        }

        private void CreateTowerVisual(int x, int y, int level)
        {
            GameObject tower;
            if (towerPrefab != null)
                tower = Instantiate(towerPrefab, _towerContainer, false);
            else
                tower = GameObject.CreatePrimitive(PrimitiveType.Cube);

            tower.name = "Tower " + x + "," + y;
            tower.transform.SetParent(_towerContainer, false);

            Tower towerScript = tower.GetComponent<Tower>();
            if (towerScript == null)
                towerScript = tower.AddComponent<Tower>();

            towerScript.SetLevel(level);
            tower.transform.localPosition = TowerPosition(x, y, tower.transform.localScale.y);
            _towers[(x, y)] = tower;
        }

        private void DestroyTowerVisual(int x, int y)
        {
            if (!_towers.TryGetValue((x, y), out GameObject tower))
                return;

            _towers.Remove((x, y));
            DestroyObject(tower);
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

        private Vector3 TowerPosition(int x, int y, float towerHeight)
        {
            Vector3 position = CellToWorld(x, y);
            position.y = BoardBaseY + cellThickness + towerHeight * 0.5f;
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
