using NUnit.Framework;
using UnityEngine;

// En modo Anchored el tablero reparte columnas y filas por la posicion en
// el MUNDO, no por la local. Importa porque el jugador pulsa "derecha"
// mirando la pantalla: si un tablero girado usara sus ejes locales, esa
// tecla movería las torres hacia abajo (paso de verdad en Level2 y Level3,
// que tienen el tablero a 90 grados).
public class BoardAnchoredOrientationTests
{
    private GameObject _root;

    [TearDown]
    public void Limpiar()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void TableroSinGirar_DerechaMueveHaciaMasX()
    {
        BoardManager board = CrearTablero(0f);

        Assert.Greater(MoverYMedir(board, GridDirection.Right).x, 0.5f);
    }

    [Test]
    public void TableroGirado90_DerechaSigueMoviendoHaciaMasX()
    {
        BoardManager board = CrearTablero(90f);

        Vector3 delta = MoverYMedir(board, GridDirection.Right);

        Assert.Greater(delta.x, 0.5f, "moverse a la derecha tiene que subir la X del mundo");
        Assert.Less(Mathf.Abs(delta.z), 0.5f, "moverse a la derecha no debe cambiar la Z");
    }

    [Test]
    public void TableroGirado90_ArribaSigueMoviendoHaciaMasZ()
    {
        BoardManager board = CrearTablero(90f);

        Vector3 delta = MoverYMedir(board, GridDirection.Up);

        Assert.Greater(delta.z, 0.5f, "moverse arriba tiene que subir la Z del mundo");
        Assert.Less(Mathf.Abs(delta.x), 0.5f, "moverse arriba no debe cambiar la X");
    }

    // Un giro cualquiera tampoco debe torcer los ejes.
    [Test]
    public void TableroGirado180_DerechaSigueMoviendoHaciaMasX()
    {
        BoardManager board = CrearTablero(180f);

        Assert.Greater(MoverYMedir(board, GridDirection.Right).x, 0.5f);
    }

    // Pone una torre en la esquina de abajo a la izquierda del mundo, mueve
    // en la direccion pedida y devuelve cuanto se desplazo en el mundo.
    private Vector3 MoverYMedir(BoardManager board, GridDirection direction)
    {
        for (int x = 0; x < board.boardWidth; x++)
            for (int y = 0; y < board.boardHeight; y++)
                board.Grid.SetLevel(x, y, 0);

        board.PlaceTower(0, 0, 1);
        Vector3 before = PosicionDeLaTorre(board);

        board.Move(direction);
        return PosicionDeLaTorre(board) - before;
    }

    private Vector3 PosicionDeLaTorre(BoardManager board)
    {
        for (int x = 0; x < board.boardWidth; x++)
            for (int y = 0; y < board.boardHeight; y++)
                if (board.Grid.GetLevel(x, y) > 0)
                    return PosicionMundo(board, x, y);

        return Vector3.zero;
    }

    private Vector3 PosicionMundo(BoardManager board, int x, int y)
    {
        var field = typeof(BoardManager).GetField("_anchorLocalPositions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Vector3[,] anchors = (Vector3[,])field.GetValue(board);
        return board.transform.TransformPoint(anchors[x, y]);
    }

    // Tablero 3x3 de marcadores separados 2 unidades, con el giro indicado.
    private BoardManager CrearTablero(float yaw)
    {
        _root = new GameObject("Tablero Test");
        _root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        var cells = new System.Collections.Generic.List<Transform>();
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                GameObject cell = new GameObject("celda " + x + "," + z);
                cell.transform.SetParent(_root.transform, false);
                cell.transform.localPosition = new Vector3(x * 2f, 0f, z * 2f);
                cells.Add(cell.transform);
            }
        }

        BoardManager board = _root.AddComponent<BoardManager>();
        board.mode = BoardMode.Anchored;
        board.anchoredCells = cells.ToArray();
        board.RebuildBoardView();
        return board;
    }
}
