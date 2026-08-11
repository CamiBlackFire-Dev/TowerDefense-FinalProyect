using NUnit.Framework;

// Pruebas de las reglas de combinacion estilo 2048.
// El algoritmo es la mecanica principal del juego, por eso se prueba
// antes de construir cualquier otro sistema encima.
public class BoardGridTests
{
    // El tablero por defecto es de 4x4.
    [Test]
    public void DefaultSize_IsFour()
    {
        Assert.AreEqual(4, new BoardGrid().Size);
    }

    // Guardar y leer un nivel funciona en cualquier casilla.
    [Test]
    public void SetAndGetLevel_RoundTrip()
    {
        var grid = new BoardGrid();
        grid.SetLevel(2, 3, 5);
        Assert.AreEqual(5, grid.GetLevel(2, 3));
        Assert.IsTrue(grid.IsEmpty(0, 0));
    }

    // Si no queda ninguna casilla libre, el tablero esta lleno.
    [Test]
    public void HasFreeCell_False_WhenGridFull()
    {
        var grid = new BoardGrid();
        for (int x = 0; x < grid.Size; x++)
            for (int y = 0; y < grid.Size; y++)
                grid.SetLevel(x, y, 1);

        Assert.IsFalse(grid.HasFreeCell());
        Assert.AreEqual(0, grid.GetFreeCells().Count);
    }

    // La lista de casillas libres solo incluye las vacias.
    [Test]
    public void GetFreeCells_ReturnsOnlyEmptyCells()
    {
        var grid = new BoardGrid();
        grid.SetLevel(0, 0, 1);
        grid.SetLevel(2, 2, 1);

        var free = grid.GetFreeCells();

        Assert.AreEqual(14, free.Count);
        Assert.IsFalse(free.Contains((0, 0)));
        Assert.IsFalse(free.Contains((2, 2)));
    }

    // Dos torres iguales seguidas se combinan en una de nivel superior.
    [Test]
    public void Left_TwoAdjacentEqual_MergeOnce()
    {
        var grid = FromString("1,1,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "2,0,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.IsTrue(result.Changed);
        Assert.AreEqual(1, result.MergeCount);
    }

    // Cuatro torres iguales se combinan por parejas, nunca en cadena.
    [Test]
    public void Left_FourOnes_MergeInPairs_NotChain()
    {
        var grid = FromString("1,1,1,1|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "2,2,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.AreEqual(2, result.MergeCount);
    }

    // Con tres torres iguales solo se combina una pareja y sobra una.
    [Test]
    public void Left_ThreeOnes_MergeOnlyOnePair()
    {
        var grid = FromString("1,1,1,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "2,1,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.AreEqual(1, result.MergeCount);
    }

    // La torre creada por una fusion no se fusiona otra vez en el mismo movimiento.
    [Test]
    public void Left_MergedTile_DoesNotMergeAgainInSameMove()
    {
        var grid = FromString("1,1,2,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "2,2,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.AreEqual(1, result.MergeCount);
    }

    // Grupos distintos se combinan de forma independiente en la misma linea.
    [Test]
    public void Left_AdjacentGroups_MergeIndependently()
    {
        var grid = FromString("1,1,2,2|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "2,3,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.AreEqual(2, result.MergeCount);
    }

    // Torres de niveles distintos nunca se combinan.
    [Test]
    public void Left_DifferentLevels_DoNotMerge()
    {
        var grid = FromString("1,2,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "1,2,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.IsFalse(result.Changed);
        Assert.AreEqual(0, result.MergeCount);
    }

    // Las torres se deslizan por los huecos y luego se combinan.
    [Test]
    public void Left_GapCollapses_ThenMerges()
    {
        var grid = FromString("1,0,1,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "2,0,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.IsTrue(result.Changed);
        Assert.AreEqual(1, result.MergeCount);
    }

    // Deslizarse sin combinar tambien cuenta como movimiento valido.
    [Test]
    public void Left_SlideWithoutMerge_IsAValidMove()
    {
        var grid = FromString("0,1,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        AssertBoard(grid, "1,0,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.IsTrue(result.Changed);
    }

    // Si nada se mueve ni se combina, el movimiento no cuenta como valido.
    [Test]
    public void Left_AlreadyPackedAgainstEdge_NotAValidMove()
    {
        var grid = FromString("2,3,4,5|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(0, result.MergeCount);
    }

    // Al mover a la derecha las torres se combinan contra el borde derecho.
    [Test]
    public void Right_FourOnes_MergeInPairsTowardRight()
    {
        var grid = FromString("1,1,1,1|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Right);

        AssertBoard(grid, "0,0,2,2|0,0,0,0|0,0,0,0|0,0,0,0");
        Assert.AreEqual(2, result.MergeCount);
    }

    // Al mover a la derecha la fusion del borde tampoco encadena.
    [Test]
    public void Right_LeadingEdgeMerge_DoesNotChain()
    {
        var grid = FromString("1,2,2,0|0,0,0,0|0,0,0,0|0,0,0,0");

        grid.Move(GridDirection.Right);

        AssertBoard(grid, "0,0,1,3|0,0,0,0|0,0,0,0|0,0,0,0");
    }

    // Al mover hacia arriba las columnas se combinan contra el borde
    // superior (y = 3), que es la parte alta vista desde la camara.
    [Test]
    public void Up_MergesColumnTowardTop()
    {
        var grid = FromString("1,0,0,0|1,0,0,0|1,0,0,0|1,0,0,0");

        var result = grid.Move(GridDirection.Up);

        AssertBoard(grid, "0,0,0,0|0,0,0,0|2,0,0,0|2,0,0,0");
        Assert.AreEqual(2, result.MergeCount);
    }

    // Al mover hacia abajo las columnas se combinan contra el borde
    // inferior (y = 0), que es la parte baja vista desde la camara.
    [Test]
    public void Down_MergesColumnTowardBottom()
    {
        var grid = FromString("1,0,0,0|1,0,0,0|1,0,0,0|1,0,0,0");

        var result = grid.Move(GridDirection.Down);

        AssertBoard(grid, "2,0,0,0|2,0,0,0|0,0,0,0|0,0,0,0");
        Assert.AreEqual(2, result.MergeCount);
    }

    // Una torre cerca de la camara (y = 0) sube a la parte superior al pulsar arriba.
    [Test]
    public void Up_DesplazaLasTorresHaciaLaParteSuperior()
    {
        var grid = FromString("1,0,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        grid.Move(GridDirection.Up);

        AssertBoard(grid, "0,0,0,0|0,0,0,0|0,0,0,0|1,0,0,0");
    }

    // Una torre en la parte superior (y = 3) baja hacia la camara al pulsar abajo.
    [Test]
    public void Down_DesplazaLasTorresHaciaLaParteInferior()
    {
        var grid = FromString("0,0,0,0|0,0,0,0|0,0,0,0|1,0,0,0");

        grid.Move(GridDirection.Down);

        AssertBoard(grid, "1,0,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
    }

    // Una fusion registra el destino, las dos fuentes y el nivel resultante.
    [Test]
    public void Left_Fusion_RegistraElEventoCompleto()
    {
        var grid = FromString("1,1,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        Assert.AreEqual(1, result.Merges.Count);
        Assert.AreEqual(0, result.Moves.Count);
        TowerMergeEvent merge = result.Merges[0];
        Assert.AreEqual((0, 0), merge.To);
        Assert.AreEqual((0, 0), merge.FirstSource);
        Assert.AreEqual((1, 0), merge.SecondSource);
        Assert.AreEqual(2, merge.ResultLevel);
    }

    // Una fusion con hueco registra fuentes no adyacentes.
    [Test]
    public void Left_FusionConHueco_RegistraFuentesLejanas()
    {
        var grid = FromString("1,0,1,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        Assert.AreEqual(1, result.Merges.Count);
        Assert.AreEqual((0, 0), result.Merges[0].FirstSource);
        Assert.AreEqual((2, 0), result.Merges[0].SecondSource);
    }

    // Cuatro torres iguales generan dos fusiones independientes.
    [Test]
    public void Left_CuatroUnos_RegistraDosFusiones()
    {
        var grid = FromString("1,1,1,1|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        Assert.AreEqual(2, result.Merges.Count);
        Assert.AreEqual(2, result.Merges[0].ResultLevel);
        Assert.AreEqual(2, result.Merges[1].ResultLevel);
        Assert.AreEqual((0, 0), result.Merges[0].To);
        Assert.AreEqual((1, 0), result.Merges[1].To);
    }

    // Un deslizamiento sin fusion registra el movimiento.
    [Test]
    public void Left_Deslizamiento_RegistraElMovimiento()
    {
        var grid = FromString("0,1,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        Assert.AreEqual(1, result.Moves.Count);
        Assert.AreEqual(0, result.Merges.Count);
        TowerMoveEvent move = result.Moves[0];
        Assert.AreEqual((1, 0), move.From);
        Assert.AreEqual((0, 0), move.To);
        Assert.AreEqual(1, move.Level);
    }

    // La torre creada por una fusion no vuelve a fusionarse: aparece como
    // movimiento de la siguiente torre, no como una segunda fusion.
    [Test]
    public void Left_FusionYDeslizamiento_RegistraAmbosEventos()
    {
        var grid = FromString("1,1,2,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        Assert.AreEqual(1, result.Merges.Count);
        Assert.AreEqual(1, result.Moves.Count);
        Assert.AreEqual(2, result.Merges[0].ResultLevel);
        Assert.AreEqual((2, 0), result.Moves[0].From);
        Assert.AreEqual((1, 0), result.Moves[0].To);
        Assert.AreEqual(2, result.Moves[0].Level);
    }

    // Mover hacia arriba registra la casilla destino en la parte alta.
    [Test]
    public void Up_Desplazamiento_RegistraMovimientoVertical()
    {
        var grid = FromString("1,0,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Up);

        Assert.AreEqual(1, result.Moves.Count);
        Assert.AreEqual((0, 0), result.Moves[0].From);
        Assert.AreEqual((0, 3), result.Moves[0].To);
    }

    // Un movimiento sin cambios no genera ningun evento.
    [Test]
    public void Left_SinCambios_NoGeneraEventos()
    {
        var grid = FromString("2,3,4,5|0,0,0,0|0,0,0,0|0,0,0,0");

        var result = grid.Move(GridDirection.Left);

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(0, result.Moves.Count);
        Assert.AreEqual(0, result.Merges.Count);
    }

    // Cada fila se procesa de forma independiente en el mismo movimiento.
    [Test]
    public void Left_MultipleRows_MoveIndependently()
    {
        var grid = FromString("1,1,0,0|0,1,1,0|0,0,0,0|1,0,1,0");

        grid.Move(GridDirection.Left);

        AssertBoard(grid, "2,0,0,0|2,0,0,0|0,0,0,0|2,0,0,0");
    }

    // La fusion siempre sube exactamente un nivel, sin importar el nivel actual.
    [Test]
    public void Merge_IncrementsLevelByExactlyOne()
    {
        var grid = FromString("11,11,0,0|0,0,0,0|0,0,0,0|0,0,0,0");

        grid.Move(GridDirection.Left);

        AssertBoard(grid, "12,0,0,0|0,0,0,0|0,0,0,0|0,0,0,0");
    }

    // Un tablero lleno sin parejas no permite ningun movimiento.
    [Test]
    public void NoPossibleMoves_AllDirections_NotAValidMove()
    {
        var grid = FromString("1,2,1,2|2,1,2,1|1,2,1,2|2,1,2,1");

        Assert.IsFalse(grid.Move(GridDirection.Left).Changed);
        Assert.IsFalse(grid.Move(GridDirection.Right).Changed);
        Assert.IsFalse(grid.Move(GridDirection.Up).Changed);
        Assert.IsFalse(grid.Move(GridDirection.Down).Changed);
    }

    // Un tablero vacio no permite ningun movimiento.
    [Test]
    public void EmptyBoard_AllDirections_NotAValidMove()
    {
        var grid = new BoardGrid();

        Assert.IsFalse(grid.Move(GridDirection.Left).Changed);
        Assert.IsFalse(grid.Move(GridDirection.Right).Changed);
        Assert.IsFalse(grid.Move(GridDirection.Up).Changed);
        Assert.IsFalse(grid.Move(GridDirection.Down).Changed);
    }

    // Arma un tablero desde un texto facil de leer.
    // Formato: "1,1,0,0|0,0,0,0|0,0,0,0|0,0,0,0" (una fila por linea).
    private static BoardGrid FromString(string descripcion)
    {
        string[] lineas = descripcion.Split('|');
        var grid = new BoardGrid(lineas.Length);
        for (int y = 0; y < lineas.Length; y++)
        {
            string[] celdas = lineas[y].Split(',');
            for (int x = 0; x < celdas.Length; x++)
                grid.SetLevel(x, y, int.Parse(celdas[x]));
        }
        return grid;
    }

    // Compara el tablero con el texto esperado, celda por celda.
    private static void AssertBoard(BoardGrid grid, string esperado)
    {
        string[] lineas = esperado.Split('|');
        for (int y = 0; y < lineas.Length; y++)
        {
            string[] celdas = lineas[y].Split(',');
            for (int x = 0; x < celdas.Length; x++)
                Assert.AreEqual(int.Parse(celdas[x]), grid.GetLevel(x, y),
                    "Casilla inesperada en (" + x + "," + y + ")");
        }
    }
}
