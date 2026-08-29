using NUnit.Framework;
using UnityEditor;
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

    // Con una oleada en curso el tablero no se puede mover.
    [Test]
    public void Move_ConOleadaEnCurso_NoMueveLasTorres()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(0, 0, 1);
        board.PlaceTower(1, 0, 1);

        EnemySpawner spawner = _boardObject.AddComponent<EnemySpawner>();
        SetPrivateField(spawner, "_running", true);
        board.spawner = spawner;

        MoveResult result = board.Move(GridDirection.Left);

        Assert.IsFalse(result.Changed);
        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        Assert.AreEqual(1, torre.Level);
        Assert.IsTrue(board.TryGetTower(1, 0, out torre));
    }

    // Entre oleadas (spawner asignado pero detenido) el movimiento funciona normal.
    [Test]
    public void Move_SinOleadaEnCurso_SiMueveLasTorres()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(0, 0, 1);
        board.PlaceTower(1, 0, 1);
        board.spawner = _boardObject.AddComponent<EnemySpawner>();

        MoveResult result = board.Move(GridDirection.Left);

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(2, board.Grid.GetLevel(0, 0));
    }

    // Sin oleada en curso el tablero no esta bloqueado.
    [Test]
    public void IsLocked_SinOleada_NoEstaBloqueado()
    {
        BoardManager board = CrearBoard();
        board.spawner = _boardObject.AddComponent<EnemySpawner>();

        Assert.IsFalse(board.IsLocked);
    }

    // Con una oleada en curso el tablero esta bloqueado.
    [Test]
    public void IsLocked_ConOleadaEnCurso_EstaBloqueado()
    {
        BoardManager board = CrearBoard();
        EnemySpawner spawner = _boardObject.AddComponent<EnemySpawner>();
        SetPrivateField(spawner, "_running", true);
        board.spawner = spawner;

        Assert.IsTrue(board.IsLocked);
    }

    // El powerup de desbloqueo temporal deja mover el tablero aunque la
    // oleada siga en curso.
    [Test]
    public void UnlockTemporarily_ConOleadaEnCurso_SiMueveLasTorres()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(0, 0, 1);
        board.PlaceTower(1, 0, 1);

        EnemySpawner spawner = _boardObject.AddComponent<EnemySpawner>();
        SetPrivateField(spawner, "_running", true);
        board.spawner = spawner;

        board.UnlockTemporarily();
        MoveResult result = board.Move(GridDirection.Left);

        Assert.IsFalse(board.IsLocked);
        Assert.IsTrue(result.Changed);
        Assert.AreEqual(2, board.Grid.GetLevel(0, 0));
    }

    // LockStateChanged avisa cuando el tablero pasa a bloqueado (lo dispara
    // Update en el juego real; aca se llama al metodo privado directo,
    // igual que el resto de pruebas de editor no esperan a Update).
    [Test]
    public void LockStateChanged_AvisaAlBloquearse()
    {
        BoardManager board = CrearBoard();
        EnemySpawner spawner = _boardObject.AddComponent<EnemySpawner>();
        SetPrivateField(spawner, "_running", true);
        board.spawner = spawner;

        bool? avisado = null;
        board.LockStateChanged += locked => avisado = locked;

        InvokePrivateMethod(board, "NotifyLockStateIfChanged");

        Assert.IsTrue(avisado.HasValue);
        Assert.IsTrue(avisado.Value);
    }

    // En modo Anchored el ancho y el alto salen de agrupar los marcadores
    // por X y por Z, sin importar en que orden se hayan asignado.
    [Test]
    public void ModoAnchored_DeduceElTamanoDesdeLosMarcadores()
    {
        BoardManager board = CrearBoard();
        board.mode = BoardMode.Anchored;
        board.anchoredCells = CrearMarcadores(4, 3, 2.5f);

        board.RebuildBoardView();

        Assert.AreEqual(4, board.GridWidth);
        Assert.AreEqual(3, board.GridHeight);
    }

    // En modo Anchored la torre se coloca exactamente sobre el marcador,
    // no sobre la formula procedural centrada en el origen.
    [Test]
    public void ModoAnchored_LaTorreQuedaSobreElMarcador()
    {
        BoardManager board = CrearBoard();
        board.mode = BoardMode.Anchored;
        board.anchoredCells = CrearMarcadores(4, 3, 2.5f);
        board.RebuildBoardView();

        board.PlaceTower(1, 2, 1);

        Tower torre;
        Assert.IsTrue(board.TryGetTower(1, 2, out torre));
        Vector3 esperado = board.CellToWorld(1, 2);
        Assert.AreEqual(esperado.x, torre.transform.localPosition.x, 0.01f);
        Assert.AreEqual(esperado.z, torre.transform.localPosition.z, 0.01f);
    }

    // Crea marcadores en una grilla width x height, espaciados uniformemente,
    // en el mismo GameObject del tablero (misma referencia de espacio local
    // que usa BuildAnchorGrid).
    private Transform[] CrearMarcadores(int width, int height, float spacing)
    {
        var markers = new Transform[width * height];
        int i = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject marker = new GameObject("Marker " + x + "," + y);
                marker.transform.SetParent(_boardObject.transform, false);
                marker.transform.localPosition = new Vector3(x * spacing, 0f, y * spacing);
                markers[i++] = marker.transform;
            }
        }
        return markers;
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

    // Toda torre que crea el tablero puede disparar.
    [Test]
    public void PlaceTower_LaTorreQuedaListaParaAtacar()
    {
        BoardManager board = CrearBoard();

        board.PlaceTower(0, 0, 1);

        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        Assert.IsNotNull(torre.GetComponent<TowerAttack>());
        Assert.IsNotNull(torre.GetComponent<TowerTargetDetector>());
    }

    // Con el ataque apagado las torres quedan solo decorativas.
    [Test]
    public void PlaceTower_SinCombate_NoAgregaElAtaque()
    {
        BoardManager board = CrearBoard();
        board.towersAttack = false;

        board.PlaceTower(0, 0, 1);

        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        Assert.IsNull(torre.GetComponent<TowerAttack>());
    }

    // Toda torre tiene vida, incluso con el ataque apagado: es un dato
    // propio de la torre, no depende de si dispara sola.
    [Test]
    public void PlaceTower_LaTorreQuedaConVida()
    {
        BoardManager board = CrearBoard();
        board.towersAttack = false;

        board.PlaceTower(0, 0, 1);

        Tower torre;
        Assert.IsTrue(board.TryGetTower(0, 0, out torre));
        TowerHealth vida = torre.GetComponent<TowerHealth>();
        Assert.IsNotNull(vida);
        Assert.IsTrue(vida.IsAlive);
    }

    // Al quedarse sin vida, una torre de nivel 2 baja a nivel 1 con la vida
    // llena en vez de desaparecer de una.
    [Test]
    public void TowerHealth_AlAgotarse_BajaUnNivel()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(1, 1, 2);
        Tower torre;
        board.TryGetTower(1, 1, out torre);
        TowerHealth vida = torre.GetComponent<TowerHealth>();

        vida.TakeDamage(9999f);

        Assert.AreEqual(1, board.Grid.GetLevel(1, 1));
        Assert.IsTrue(board.TryGetTower(1, 1, out torre));
        Assert.AreEqual(1, torre.Level);
        Assert.IsTrue(torre.GetComponent<TowerHealth>().IsAlive);
    }

    // Una torre de nivel 1 que se queda sin vida si desaparece del todo:
    // ya no hay a donde bajar, la casilla queda vacia.
    [Test]
    public void TowerHealth_AlAgotarse_EnNivel1_QuitaLaTorre()
    {
        BoardManager board = CrearBoard();
        board.PlaceTower(2, 2, 1);
        Tower torre;
        board.TryGetTower(2, 2, out torre);
        TowerHealth vida = torre.GetComponent<TowerHealth>();

        vida.TakeDamage(9999f);

        Assert.AreEqual(0, board.Grid.GetLevel(2, 2));
        Assert.IsFalse(board.TryGetTower(2, 2, out torre));
    }

    [Test]
    public void TowerDepletionVfx_DistingueDegradacionDeDestruccionFinal()
    {
        BoardManager board = CrearBoard();
        GameObject downgrade = new GameObject("Downgrade VFX");
        GameObject destroyed = new GameObject("Destroyed VFX");
        downgrade.transform.SetParent(_boardObject.transform);
        destroyed.transform.SetParent(_boardObject.transform);
        board.towerDowngradeVfx = downgrade;
        board.towerDestroyedVfx = destroyed;

        Assert.AreSame(downgrade, board.TowerDepletionVfxForLevel(2));
        Assert.AreSame(destroyed, board.TowerDepletionVfxForLevel(1));
    }

    [Test]
    public void EnemyDamageNumber_UsaColorRojoPersonalizado()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Custom/Prefabs/VFX/EnemyDamageNumber.prefab");
        var damageNumber = prefab.GetComponent<DamageNumbersPro.DamageNumber>();
        SerializedObject serialized = new SerializedObject(damageNumber);
        Color color = serialized.FindProperty("numberSettings.color").colorValue;

        Assert.IsTrue(serialized.FindProperty("numberSettings.customColor").boolValue);
        Assert.Greater(color.r, 0.9f);
        Assert.Less(color.g, 0.15f);
        Assert.Less(color.b, 0.15f);
    }

    [Test]
    public void AlliedDamageNumber_ConservaElTextoBlanco()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Custom/Prefabs/VFX/DamageNumber.prefab");
        var damageNumber = prefab.GetComponent<DamageNumbersPro.DamageNumber>();
        SerializedObject serialized = new SerializedObject(damageNumber);
        TMPro.TMP_Text text = prefab.GetComponentInChildren<TMPro.TMP_Text>(true);

        Assert.IsFalse(serialized.FindProperty("numberSettings.customColor").boolValue);
        Assert.AreEqual(Color.white, text.color);
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

    // Llama un metodo privado desde la prueba (por ejemplo el que normalmente
    // dispara Update, para no depender de que Update corra en modo editor).
    private static void InvokePrivateMethod(object target, string methodName)
    {
        System.Reflection.MethodInfo method = target.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method.Invoke(target, null);
    }

    // Crea un BoardManager de prueba con un tablero vacio.
    private BoardManager CrearBoard()
    {
        _boardObject = new GameObject("Board Test");
        return _boardObject.AddComponent<BoardManager>();
    }
}
