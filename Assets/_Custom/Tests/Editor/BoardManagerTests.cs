using NUnit.Framework;
using UnityEngine;

// Pruebas de la conexion entre el modelo BoardGrid y las torres visibles.
// Verifican que colocar torres y mover el tablero se refleja en la escena.
public class BoardManagerTests
{
    private GameObject _boardObject;
    private Mesh _testMesh;

    // Borra todo lo creado en cada prueba.
    [TearDown]
    public void Limpiar()
    {
        if (_boardObject != null)
            Object.DestroyImmediate(_boardObject);
        if (_testMesh != null)
            Object.DestroyImmediate(_testMesh);
    }

    // Al iniciar, el tablero crea las 16 celdas visibles.
    [Test]
    public void Awake_CreaLas16Celdas()
    {
        BoardManager board = CrearBoard();

        Assert.AreEqual(4, board.Grid.Size);
        Assert.AreEqual(16, board.transform.Find("Cells").childCount);
    }

    // Colocar una torre crea su visual en la casilla correcta.
    [Test]
    public void PlaceTower_CreaLaTorreVisualEnLaCasilla()
    {
        BoardManager board = CrearBoard();

        bool colocado = board.PlaceTower(0, 0, 1);

        Assert.IsTrue(colocado);
        Assert.AreEqual(1, board.Grid.GetLevel(0, 0));

        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        Assert.AreEqual(1, torre.Level);

        Vector3 posicion = torre.transform.position;
        Vector3 celda = board.CellToWorld(0, 0);
        Assert.AreEqual(celda.x, posicion.x, 0.01f);
        Assert.AreEqual(celda.z, posicion.z, 0.01f);
    }

    // No se puede colocar una torre sobre una casilla ocupada.
    [Test]
    public void PlaceTower_CeldaOcupada_RechazaLaColocacion()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(2, 2, 1);

        bool segundoIntento = board.PlaceTower(2, 2, 1);

        Assert.IsFalse(segundoIntento);
        Assert.AreEqual(1, board.Grid.GetLevel(2, 2));
    }

    // Un movimiento 2048 actualiza las torres visibles.
    [Test]
    public void Move_ActualizaLasTorresVisibles()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(0, 0, 1);
        board.PlaceTower(1, 0, 1);

        MoveResult result = board.Move(GridDirection.Left);

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(2, board.Grid.GetLevel(0, 0));

        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        Assert.AreEqual(2, torre.Level);
    }

    // Un movimiento sin cambios no toca las torres visibles.
    [Test]
    public void Move_SinCambios_NoModificaLasTorres()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(0, 0, 1);
        board.PlaceTower(1, 0, 2);

        MoveResult result = board.Move(GridDirection.Left);

        Assert.IsFalse(result.Changed);
        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        Assert.AreEqual(1, torre.Level);
    }

    // Cambiar una casilla desde el inspector actualiza el modelo y la vista.
    [Test]
    public void SetEditorLevel_ActualizaModeloYVista()
    {
        BoardManager board = CrearBoard();

        board.SetEditorLevel(3, 3, 4);

        Assert.AreEqual(4, board.GetEditorLevel(3, 3));
        Tower torre;
        Assert.IsTrue(board.TryGetTower(3, 3, out torre));
        Assert.AreEqual(4, torre.Level);
    }

    // Colocar al azar usa una celda libre.
    [Test]
    public void PlaceTowerRandom_ColocaEnUnaCeldaLibre()
    {
        BoardManager board = CrearBoard();

        bool colocado = board.PlaceTowerRandom(1);

        Assert.IsTrue(colocado);
        Assert.AreEqual(1, ContarOcupadas(board));
    }

    // Colocar al azar con el tablero lleno falla.
    [Test]
    public void PlaceTowerRandom_TableroLleno_Rechaza()
    {
        BoardManager board = CrearBoard();
        for (int x = 0; x < board.Grid.Size; x++)
            for (int y = 0; y < board.Grid.Size; y++)
                board.PlaceTower(x, y, 1);

        bool colocado = board.PlaceTowerRandom(1);

        Assert.IsFalse(colocado);
    }

    // Cuenta las casillas ocupadas del tablero.
    private int ContarOcupadas(BoardManager board)
    {
        int count = 0;
        for (int x = 0; x < board.Grid.Size; x++)
            for (int y = 0; y < board.Grid.Size; y++)
                if (board.Grid.GetLevel(x, y) > 0)
                    count++;

        return count;
    }

    // Con la animacion activa, los movimientos se encolan sin perderse.
    [Test]
    public void Move_DuranteAnimacion_EncolaElMovimiento()
    {
        BoardManager board = CrearBoard();

        // Simulamos una animacion en curso (en pruebas de editor no hay corutinas).
        SetPrivateField(board, "_isAnimating", true);

        board.Move(GridDirection.Left);
        board.Move(GridDirection.Up);

        Assert.AreEqual(2, board.PendingMoveCount);
    }

    // La cola de movimientos no supera el maximo configurado.
    [Test]
    public void Move_DuranteAnimacion_RespetaElMaximo()
    {
        BoardManager board = CrearBoard();
        SetPrivateField(board, "_isAnimating", true);

        for (int i = 0; i < 20; i++)
            board.Move(GridDirection.Left);

        Assert.AreEqual(8, board.PendingMoveCount);
    }

    // Sin animacion los movimientos se ejecutan al instante y no se encolan.
    [Test]
    public void Move_SinAnimacion_NoEncola()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(0, 0, 1);

        board.Move(GridDirection.Left);

        Assert.AreEqual(0, board.PendingMoveCount);
        Assert.IsFalse(board.IsAnimating);
    }

    // Cambiar el modelo de la cuadricula no cambia el tamano de las casillas.
    [Test]
    public void CellMesh_NoCambiaElTamanoDeLaCasilla()
    {
        BoardManager board = CrearBoard();
        Renderer conCubo = CasillaRenderer(board, 0, 0);
        Vector3 tamano = conCubo.bounds.size;
        Vector3 centro = conCubo.bounds.center;

        // Malla con otro tamano y con el pivote en una esquina.
        board.cellMesh = CrearMallaDePrueba();
        board.RebuildBoardView();

        Renderer conMalla = CasillaRenderer(board, 0, 0);
        Assert.AreEqual(tamano.x, conMalla.bounds.size.x, 0.01f);
        Assert.AreEqual(tamano.y, conMalla.bounds.size.y, 0.01f);
        Assert.AreEqual(tamano.z, conMalla.bounds.size.z, 0.01f);
        Assert.AreEqual(centro.x, conMalla.bounds.center.x, 0.01f);
        Assert.AreEqual(centro.y, conMalla.bounds.center.y, 0.01f);
        Assert.AreEqual(centro.z, conMalla.bounds.center.z, 0.01f);
    }

    // Cada nivel usa el modelo que le corresponde en la lista.
    [Test]
    public void TowerLevelModels_UsaElModeloDelNivel()
    {
        BoardManager board = CrearBoard();
        GameObject nivel1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject nivel2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        board.towerLevelModels = new GameObject[] { nivel1, nivel2 };

        board.PlaceTower(0, 0, 2);

        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        MeshFilter modelo = torre.GetComponentInChildren<MeshFilter>();
        Assert.AreEqual(nivel2.GetComponent<MeshFilter>().sharedMesh, modelo.sharedMesh);

        Object.DestroyImmediate(nivel1);
        Object.DestroyImmediate(nivel2);
    }

    // El color elegido en el tablero se aplica a la torre de ese nivel.
    [Test]
    public void TowerLevelColors_PintaLaTorreConElColorDelNivel()
    {
        BoardManager board = CrearBoard();
        board.towerLevelColors = new Color[] { Color.red, Color.blue };

        board.PlaceTower(0, 0, 2);

        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        torre.GetComponentInChildren<Renderer>().GetPropertyBlock(bloque);
        Color color = bloque.GetColor("_BaseColor");
        Assert.AreEqual(Color.blue.r, color.r, 0.01f);
        Assert.AreEqual(Color.blue.g, color.g, 0.01f);
        Assert.AreEqual(Color.blue.b, color.b, 0.01f);
    }

    // Renderer de una casilla concreta del tablero.
    private Renderer CasillaRenderer(BoardManager board, int x, int y)
    {
        Transform cell = board.transform.Find("Cells/Cell " + x + "," + y);
        return cell.GetComponent<Renderer>();
    }

    // Malla de prueba de 4 x 2 x 4 con el pivote en una esquina.
    private Mesh CrearMallaDePrueba()
    {
        _testMesh = new Mesh();
        _testMesh.vertices = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(4f, 0f, 0f),
            new Vector3(0f, 2f, 0f),
            new Vector3(0f, 0f, 4f),
        };
        _testMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, 0, 3, 1, 1, 3, 2 };
        _testMesh.RecalculateBounds();
        return _testMesh;
    }

    // Asigna un campo privado desde la prueba (los campos serializados son privados).
    private static void SetPrivateField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    // Crea un BoardManager de prueba con un tablero vacio.
    private BoardManager CrearBoard()
    {
        _boardObject = new GameObject("Board Test");
        return _boardObject.AddComponent<BoardManager>();
    }
}
