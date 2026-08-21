using NUnit.Framework;
using UnityEngine;

// Pruebas del selector de tableros: en los niveles con mas de uno, las
// teclas tienen que mover solo el elegido.
public class BoardSelectorTests
{
    private GameObject _root;
    private BoardSelector _selector;
    private BoardManager _boardA;
    private BoardManager _boardB;

    [SetUp]
    public void Preparar()
    {
        _root = new GameObject("Selector Test");

        _boardA = new GameObject("Tablero A").AddComponent<BoardManager>();
        _boardB = new GameObject("Tablero B").AddComponent<BoardManager>();
        _boardA.transform.SetParent(_root.transform, false);
        _boardB.transform.SetParent(_root.transform, false);

        _selector = _root.AddComponent<BoardSelector>();
        _selector.boards = new BoardManager[] { _boardA, _boardB };
        _selector.showHighlight = false;   // sin marca visual en las pruebas
    }

    [TearDown]
    public void Limpiar()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void Select_SoloElElegidoRecibeTeclas()
    {
        _selector.Select(0);

        Assert.IsTrue(_boardA.acceptsInput);
        Assert.IsFalse(_boardB.acceptsInput);
    }

    [Test]
    public void Select_CambiarDeTableroPasaElTurno()
    {
        _selector.Select(0);
        _selector.Select(1);

        Assert.IsFalse(_boardA.acceptsInput);
        Assert.IsTrue(_boardB.acceptsInput);
    }

    [Test]
    public void SelectNext_DaLaVueltaAlLlegarAlUltimo()
    {
        _selector.Select(0);

        _selector.SelectNext();
        Assert.AreEqual(1, _selector.SelectedIndex);

        _selector.SelectNext();
        Assert.AreEqual(0, _selector.SelectedIndex);
    }

    // Un indice fuera de rango no debe dejar los dos tableros quietos ni
    // reventar: se recorta al que exista.
    [Test]
    public void Select_ConIndiceFueraDeRango_SeRecorta()
    {
        _selector.Select(99);

        Assert.AreEqual(1, _selector.SelectedIndex);
        Assert.IsTrue(_boardB.acceptsInput);
    }

    [Test]
    public void SelectionChanged_AvisaDelCambio()
    {
        int avisos = 0;
        int ultimo = -1;
        _selector.SelectionChanged += index => { avisos++; ultimo = index; };

        _selector.Select(1);

        Assert.AreEqual(1, avisos);
        Assert.AreEqual(1, ultimo);
    }
}
