using NUnit.Framework;
using UnityEngine.InputSystem;

namespace TowerDefense
{
    // Pruebas de la correspondencia entre teclas y comandos de tablero.
    public class InputControllerTests
    {
        [TestCase(Key.UpArrow, GridDirection.Up)]
        [TestCase(Key.W, GridDirection.Up)]
        [TestCase(Key.DownArrow, GridDirection.Down)]
        [TestCase(Key.S, GridDirection.Down)]
        [TestCase(Key.LeftArrow, GridDirection.Left)]
        [TestCase(Key.A, GridDirection.Left)]
        [TestCase(Key.RightArrow, GridDirection.Right)]
        [TestCase(Key.D, GridDirection.Right)]
        public void TryGetDirection_DevuelveLaDireccionCorrecta(Key key, GridDirection expected)
        {
            GridDirection direction;

            bool found = InputController.TryGetDirection(key, out direction);

            Assert.IsTrue(found);
            Assert.AreEqual(expected, direction);
        }

        [Test]
        public void TryGetDirection_RechazaTeclasSinMovimiento()
        {
            GridDirection direction;

            bool found = InputController.TryGetDirection(Key.Space, out direction);

            Assert.IsFalse(found);
        }
    }
}
