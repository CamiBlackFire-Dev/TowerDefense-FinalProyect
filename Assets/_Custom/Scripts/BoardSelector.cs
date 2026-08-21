using System;
using UnityEngine;

// Manda en los niveles con mas de un tablero: solo el elegido hace caso a
// las teclas de movimiento, asi cada tablero se ordena por separado.
// El cambio se hace con Tab (InputController.NextBoardRequested) y se marca
// con una placa de color debajo del tablero activo.
//
// El resaltado va en un objeto aparte y no tinendo las casillas a proposito:
// BoardLockFeedback ya les cambia el color para avisar del bloqueo por
// oleada, y los dos escribiendo sobre el mismo _BaseColor se pisarian.
public class BoardSelector : MonoBehaviour
{
    [Header("Tableros")]
    // En orden: el primero es el que arranca elegido. Si se deja vacio se
    // buscan solos todos los de la escena.
    public BoardManager[] boards;

    [Header("Input")]
    // Se busca solo en la escena si se deja vacio.
    public InputController inputController;

    [Header("Marca del tablero activo")]
    public bool showHighlight = true;
    public Color highlightColor = new Color(1f, 0.85f, 0.2f, 0.35f);
    // Material de la placa (Sprites/Default o similar, como los indicadores
    // de los powerups). Sin material se ve como un cuadro blanco solido.
    public Material highlightMaterial;
    // Cuanto sobresale la placa del borde del tablero.
    public float highlightPadding = 0.6f;
    // Altura respecto de la BASE de las casillas. Va en negativo a
    // proposito: la placa tiene que quedar por debajo de ellas y asomar
    // alrededor, no taparlas.
    public float highlightHeight = -0.01f;

    private int _selectedIndex;
    private GameObject _highlight;
    private Renderer _highlightRenderer;

    // Avisa cuando cambia el tablero elegido (lo usa la ventana de debug
    // para refrescarse, y sirve para un aviso en el HUD mas adelante).
    public event Action<int> SelectionChanged;

    public int SelectedIndex
    {
        get { return _selectedIndex; }
    }

    public BoardManager SelectedBoard
    {
        get
        {
            if (boards == null || boards.Length == 0)
                return null;

            int index = Mathf.Clamp(_selectedIndex, 0, boards.Length - 1);
            return boards[index];
        }
    }

    public int BoardCount
    {
        get { return boards != null ? boards.Length : 0; }
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (inputController != null)
        {
            inputController.NextBoardRequested -= SelectNext;
            inputController.NextBoardRequested += SelectNext;
        }

        // Se reaplica al encender para que el tablero elegido quede bien
        // aunque alguien haya tocado acceptsInput a mano en el Inspector.
        Select(_selectedIndex);
    }

    private void OnDisable()
    {
        if (inputController != null)
            inputController.NextBoardRequested -= SelectNext;
    }

    private void EnsureReferences()
    {
        if (boards == null || boards.Length == 0)
            boards = FindObjectsByType<BoardManager>(FindObjectsSortMode.None);

        if (inputController == null)
            inputController = FindFirstObjectByType<InputController>();
    }

    // Pasa al siguiente tablero, dando la vuelta al llegar al ultimo.
    public void SelectNext()
    {
        if (BoardCount == 0)
            return;

        Select((_selectedIndex + 1) % BoardCount);
    }

    public void Select(int index)
    {
        if (BoardCount == 0)
            return;

        _selectedIndex = Mathf.Clamp(index, 0, BoardCount - 1);

        for (int i = 0; i < boards.Length; i++)
        {
            if (boards[i] != null)
                boards[i].acceptsInput = i == _selectedIndex;
        }

        UpdateHighlight();

        if (SelectionChanged != null)
            SelectionChanged(_selectedIndex);
    }

    // Coloca la placa debajo del tablero elegido, del tamano que ocupen sus
    // casillas. Se calcula cada vez porque el tablero puede moverse.
    private void UpdateHighlight()
    {
        if (!Application.isPlaying)
            return;

        BoardManager board = SelectedBoard;
        if (!showHighlight || board == null)
        {
            if (_highlight != null)
                _highlight.SetActive(false);
            return;
        }

        Bounds bounds;
        if (!TryGetBoardBounds(board, out bounds))
        {
            if (_highlight != null)
                _highlight.SetActive(false);
            return;
        }

        EnsureHighlight();

        _highlight.SetActive(true);
        _highlight.transform.position = new Vector3(
            bounds.center.x,
            bounds.min.y + highlightHeight,
            bounds.center.z);
        _highlight.transform.localScale = new Vector3(
            bounds.size.x + highlightPadding * 2f,
            bounds.size.z + highlightPadding * 2f,
            1f);
    }

    // Caja que ocupan las casillas del tablero. Sirve para los dos modos:
    // en Anchored son los marcadores del mapa, en Procedural los cubos que
    // genera BoardManager bajo "Cells".
    private bool TryGetBoardBounds(BoardManager board, out Bounds bounds)
    {
        bounds = new Bounds();
        bool started = false;

        Renderer[] renderers;
        if (board.mode == BoardMode.Anchored && board.anchoredCells != null)
        {
            var found = new System.Collections.Generic.List<Renderer>();
            foreach (Transform marker in board.anchoredCells)
            {
                if (marker != null)
                    found.AddRange(marker.GetComponentsInChildren<Renderer>(true));
            }
            renderers = found.ToArray();
        }
        else
        {
            Transform cells = board.transform.Find("Cells");
            renderers = cells != null
                ? cells.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
        }

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (!started)
            {
                bounds = r.bounds;
                started = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return started;
    }

    private void EnsureHighlight()
    {
        if (_highlight != null)
            return;

        _highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _highlight.name = "BoardSelectionHighlight";
        _highlight.transform.SetParent(transform, false);
        // Tumbado sobre el suelo, como los indicadores de los powerups.
        _highlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Collider quadCollider = _highlight.GetComponent<Collider>();
        if (quadCollider != null)
            Destroy(quadCollider);

        _highlightRenderer = _highlight.GetComponent<Renderer>();
        if (highlightMaterial != null)
            _highlightRenderer.sharedMaterial = highlightMaterial;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _highlightRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", highlightColor);
        block.SetColor("_Color", highlightColor);
        _highlightRenderer.SetPropertyBlock(block);
    }
}
