using NUnit.Framework;
using UnityEngine;

namespace TowerDefense
{
    // Pruebas de la conexion entre el modelo BoardGrid y las torres visibles.
    // Verifican que colocar torres y mover el tablero se refleja en la escena.
    public class BoardManagerTests
    {
        private GameObject _boardObject;

        // Borra todo lo creado en cada prueba.
        [TearDown]
        public void Limpiar()
        {
            if (_boardObject != null)
                Object.DestroyImmediate(_boardObject);
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

        // Crea un BoardManager de prueba con un tablero vacio.
        private BoardManager CrearBoard()
        {
            _boardObject = new GameObject("Board Test");
            return _boardObject.AddComponent<BoardManager>();
        }
    }
}
