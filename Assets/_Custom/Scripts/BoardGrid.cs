using System;
using System.Collections.Generic;

namespace TowerDefense
{
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
        // Procesamos el tablero fila por fila o columna por columna, siempre
        // desde el borde hacia el que se mueven las torres.
        public MoveResult Move(GridDirection direction)
        {
            bool changed = false;
            int totalMerges = 0;
            int[] line = new int[Size];

            for (int a = 0; a < Size; a++)
            {
                ExtractLine(direction, a, line);
                int merges = MergeLine(line);
                if (WriteBack(direction, a, line))
                    changed = true;
                totalMerges += merges;
            }

            return new MoveResult(changed, totalMerges);
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

        // Comprime y combina una linea en el lugar (indice 0 = borde del movimiento).
        // Trabaja en dos pasadas: primero se deslizan las torres sin huecos
        // y luego se fusionan parejas iguales de izquierda a derecha.
        // Regla clave de 2048: una torre creada por una fusion no puede volver
        // a fusionarse durante el mismo movimiento, por eso se salta el
        // elemento siguiente despues de cada fusion.
        private static int MergeLine(int[] line)
        {
            int n = line.Length;

            // Fase 1: deslizar las torres hacia el borde (sin huecos).
            int[] compact = new int[n];
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (line[i] != 0)
                    compact[count++] = line[i];
            }

            // Fase 2: fusionar parejas iguales.
            int merges = 0;
            int read = 0;
            int write = 0;
            while (read < count)
            {
                if (read + 1 < count && compact[read] == compact[read + 1])
                {
                    line[write] = compact[read] + 1;
                    read += 2;
                    merges++;
                }
                else
                {
                    line[write] = compact[read];
                    read++;
                }
                write++;
            }

            // Fase 3: rellenar el resto con vacios.
            while (write < n)
                line[write++] = 0;

            return merges;
        }

        private void ValidateCoords(int x, int y)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size)
                throw new ArgumentOutOfRangeException(
                    "(" + x + "," + y + ") esta fuera de la cuadricula " + Size + "x" + Size);
        }
    }

    // Resultado de un movimiento: si cambio el tablero y cuantas fusiones hubo.
    public readonly struct MoveResult
    {
        public bool Changed { get; }
        public int MergeCount { get; }

        public MoveResult(bool changed, int mergeCount)
        {
            Changed = changed;
            MergeCount = mergeCount;
        }
    }
}
