using System;
using System.Collections.Generic;

// Direcciones validas para mover el tablero. No existen diagonales.
public enum GridDirection
{
    Up,
    Down,
    Left,
    Right
}

// Modelo puro de la cuadricula y las reglas de combinacion estilo 2048.
// Cada casilla guarda un nivel de torre: 0 = vacia, N = torre nivel N.
// Dos torres del mismo nivel N se combinan en una torre nivel N + 1.
public sealed class BoardGrid
{
    public const int DefaultSize = 4;

    // Guardamos los niveles en un solo array para simplificar.
    // La casilla (x, y) vive en la posicion y * Size + x.
    private readonly int[] _levels;

    public int Size { get; }

    public BoardGrid(int size = DefaultSize)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        Size = size;
        _levels = new int[size * size];
    }

    // Devuelve el nivel de la casilla (x, y).
    public int GetLevel(int x, int y)
    {
        ValidateCoords(x, y);
        return _levels[y * Size + x];
    }

    // Fija el nivel de la casilla (x, y). Un nivel negativo se trata como vacio.
    public void SetLevel(int x, int y, int level)
    {
        ValidateCoords(x, y);
        if (level < 0)
            level = 0;
        _levels[y * Size + x] = level;
    }

    // Indica si la casilla (x, y) esta vacia.
    public bool IsEmpty(int x, int y)
    {
        return GetLevel(x, y) == 0;
    }

    // Devuelve true si queda al menos una casilla libre.
    public bool HasFreeCell()
    {
        for (int i = 0; i < _levels.Length; i++)
        {
            if (_levels[i] == 0)
                return true;
        }
        return false;
    }

    // Devuelve la lista de casillas libres. Sirve para colocar torres nuevas.
    public List<(int x, int y)> GetFreeCells()
    {
        var free = new List<(int x, int y)>();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (_levels[y * Size + x] == 0)
                    free.Add((x, y));
            }
        }
        return free;
    }

    // Ejecuta un movimiento completo estilo 2048 y devuelve si hubo cambios.
    // Ademas devuelve los eventos de movimiento y fusion para poder animar.
    // Procesamos el tablero fila por fila o columna por columna, siempre
    // desde el borde hacia el que se mueven las torres.
    public MoveResult Move(GridDirection direction)
    {
        bool changed = false;
        int totalMerges = 0;
        int[] line = new int[Size];
        var moves = new List<TowerMoveEvent>();
        var merges = new List<TowerMergeEvent>();

        for (int a = 0; a < Size; a++)
        {
            ExtractLine(direction, a, line);
            totalMerges += ProcessLine(direction, a, line, moves, merges);
            if (WriteBack(direction, a, line))
                changed = true;
        }

        return new MoveResult(changed, totalMerges, moves, merges);
    }

    // Copia una fila o columna al buffer, con el borde del movimiento al inicio.
    private void ExtractLine(GridDirection direction, int a, int[] buffer)
    {
        for (int b = 0; b < Size; b++)
        {
            int cell;
            switch (direction)
            {
                case GridDirection.Left:
                    cell = _levels[a * Size + b];
                    break;
                case GridDirection.Right:
                    cell = _levels[a * Size + (Size - 1 - b)];
                    break;
                case GridDirection.Up:
                    cell = _levels[(Size - 1 - b) * Size + a];
                    break;
                default:
                    cell = _levels[b * Size + a];
                    break;
            }
            buffer[b] = cell;
        }
    }

    // Vuelca el buffer procesado de vuelta al tablero.
    // Devuelve true si alguna casilla cambio.
    private bool WriteBack(GridDirection direction, int a, int[] buffer)
    {
        bool changed = false;
        for (int b = 0; b < Size; b++)
        {
            int x, y;
            switch (direction)
            {
                case GridDirection.Left:
                    x = b;
                    y = a;
                    break;
                case GridDirection.Right:
                    x = Size - 1 - b;
                    y = a;
                    break;
                case GridDirection.Up:
                    x = a;
                    y = Size - 1 - b;
                    break;
                default:
                    x = a;
                    y = b;
                    break;
            }
            changed |= SetQuiet(x, y, buffer[b]);
        }
        return changed;
    }

    // Escribe un valor en una casilla y devuelve true si realmente cambio.
    private bool SetQuiet(int x, int y, int value)
    {
        int index = y * Size + x;
        if (_levels[index] == value)
            return false;
        _levels[index] = value;
        return true;
    }

    // Comprime, fusiona y anota los eventos de la linea.
    // Devuelve cuantas fusiones ocurrieron.
    private int ProcessLine(GridDirection direction, int a, int[] line,
        List<TowerMoveEvent> moves, List<TowerMergeEvent> merges)
    {
        int n = line.Length;

        // Fase 1: deslizar las torres hacia el borde recordando su origen.
        int[] compact = new int[n];
        int[] compactFrom = new int[n];
        int count = 0;
        for (int b = 0; b < n; b++)
        {
            if (line[b] == 0)
                continue;
            compact[count] = line[b];
            compactFrom[count] = b;
            count++;
        }

        // Fase 2: fusionar parejas iguales y registrar cada evento.
        // Regla clave de 2048: una torre creada por una fusion no puede
        // volver a fusionarse durante el mismo movimiento, por eso se
        // salta el elemento siguiente despues de cada fusion.
        int mergesInLine = 0;
        int read = 0;
        int write = 0;
        while (read < count)
        {
            if (read + 1 < count && compact[read] == compact[read + 1])
            {
                int resultLevel = compact[read] + 1;
                LinePositionToCell(direction, a, write, out int toX, out int toY);
                LinePositionToCell(direction, a, compactFrom[read], out int firstX, out int firstY);
                LinePositionToCell(direction, a, compactFrom[read + 1], out int secondX, out int secondY);
                merges.Add(new TowerMergeEvent(
                    (toX, toY), (firstX, firstY), (secondX, secondY), resultLevel));

                line[write] = resultLevel;
                write++;
                read += 2;
                mergesInLine++;
            }
            else
            {
                LinePositionToCell(direction, a, compactFrom[read], out int fromX, out int fromY);
                LinePositionToCell(direction, a, write, out int toX, out int toY);
                if (fromX != toX || fromY != toY)
                    moves.Add(new TowerMoveEvent((fromX, fromY), (toX, toY), compact[read]));

                line[write] = compact[read];
                write++;
                read++;
            }
        }

        // Fase 3: rellenar el resto con vacios.
        while (write < n)
            line[write++] = 0;

        return mergesInLine;
    }

    // Convierte la posicion b de una linea a una casilla del tablero.
    // Vale tanto para el origen como para el destino porque la extraccion
    // recorre la linea desde el borde hacia el que se mueven las torres.
    private void LinePositionToCell(GridDirection direction, int a, int b, out int x, out int y)
    {
        switch (direction)
        {
            case GridDirection.Left:
                x = b;
                y = a;
                break;
            case GridDirection.Right:
                x = Size - 1 - b;
                y = a;
                break;
            case GridDirection.Up:
                x = a;
                y = Size - 1 - b;
                break;
            default:
                x = a;
                y = b;
                break;
        }
    }

    private void ValidateCoords(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            throw new ArgumentOutOfRangeException(
                "(" + x + "," + y + ") esta fuera de la cuadricula " + Size + "x" + Size);
    }
}

// Resultado de un movimiento: si cambio el tablero, cuantas fusiones hubo
// y los eventos necesarios para animar las torres.
public readonly struct MoveResult
{
    public bool Changed { get; }
    public int MergeCount { get; }
    public IReadOnlyList<TowerMoveEvent> Moves { get; }
    public IReadOnlyList<TowerMergeEvent> Merges { get; }

    public MoveResult(bool changed, int mergeCount,
        List<TowerMoveEvent> moves, List<TowerMergeEvent> merges)
    {
        Changed = changed;
        MergeCount = mergeCount;
        Moves = moves;
        Merges = merges;
    }
}

// Desplazamiento de una torre que no se fusiona.
public readonly struct TowerMoveEvent
{
    public (int x, int y) From { get; }
    public (int x, int y) To { get; }
    public int Level { get; }

    public TowerMoveEvent((int x, int y) from, (int x, int y) to, int level)
    {
        From = from;
        To = to;
        Level = level;
    }
}

// Fusion de dos torres iguales en una torre de nivel superior.
public readonly struct TowerMergeEvent
{
    public (int x, int y) To { get; }
    public (int x, int y) FirstSource { get; }
    public (int x, int y) SecondSource { get; }
    public int ResultLevel { get; }

    public TowerMergeEvent((int x, int y) to, (int x, int y) firstSource,
        (int x, int y) secondSource, int resultLevel)
    {
        To = to;
        FirstSource = firstSource;
        SecondSource = secondSource;
        ResultLevel = resultLevel;
    }
}
