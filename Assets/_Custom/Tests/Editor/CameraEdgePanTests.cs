using NUnit.Framework;
using UnityEngine;

// Pruebas del movimiento de camara por los bordes. Se prueba Move(), que
// recibe la direccion y el delta ya resueltos: asi la logica de movimiento
// y de limites se puede comprobar sin puntero ni Play Mode.
public class CameraEdgePanTests
{
    private GameObject _go;
    private CameraEdgePan _pan;

    [SetUp]
    public void Preparar()
    {
        _go = new GameObject("CamaraDePrueba");
        // Inclinada hacia abajo, como la camara real del juego: su forward
        // apunta al suelo y hay que aplanarlo para moverse por el tablero.
        _go.transform.position = new Vector3(0f, 10f, 0f);
        _go.transform.rotation = Quaternion.Euler(54f, 0f, 0f);

        _pan = _go.AddComponent<CameraEdgePan>();
        _pan.panSpeed = 10f;
        _pan.limitMin = new Vector2(-100f, -100f);
        _pan.limitMax = new Vector2(100f, 100f);
    }

    [TearDown]
    public void Limpiar()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Move_HaciaArriba_AvanzaEnZ()
    {
        _pan.Move(Vector2.up, 1f);

        Assert.Greater(_go.transform.position.z, 0f);
    }

    [Test]
    public void Move_HaciaLaDerecha_AvanzaEnX()
    {
        _pan.Move(Vector2.right, 1f);

        Assert.Greater(_go.transform.position.x, 0f);
    }

    // La camara mira inclinada: si no se aplanara su forward, moverse
    // "hacia adelante" la haria bajar hacia el suelo.
    [Test]
    public void Move_NoCambiaLaAltura()
    {
        _pan.Move(Vector2.up, 1f);

        Assert.AreEqual(10f, _go.transform.position.y, 0.001f);
    }

    // En diagonal se recorre lo mismo que en recto: sin normalizar, moverse
    // en esquina seria mas rapido que de frente.
    [Test]
    public void Move_EnDiagonal_NoVaMasRapidoQueEnRecto()
    {
        _pan.Move(Vector2.up, 1f);
        float recto = DistanciaEnElPlano(Vector3.zero, _go.transform.position);

        _go.transform.position = new Vector3(0f, 10f, 0f);
        _pan.Move(new Vector2(1f, 1f), 1f);
        float diagonal = DistanciaEnElPlano(Vector3.zero, _go.transform.position);

        Assert.AreEqual(recto, diagonal, 0.01f);
    }

    [Test]
    public void Move_RecorreLaDistanciaQueMarcaLaVelocidad()
    {
        _pan.panSpeed = 8f;

        _pan.Move(Vector2.right, 0.5f);

        Assert.AreEqual(4f, DistanciaEnElPlano(Vector3.zero, _go.transform.position), 0.01f);
    }

    [Test]
    public void Move_NoDejaPasarDelLimite()
    {
        _pan.limitMax = new Vector2(3f, 100f);

        _pan.Move(Vector2.right, 10f);   // pediria moverse 100 unidades

        Assert.AreEqual(3f, _go.transform.position.x, 0.001f);
    }

    [Test]
    public void Move_SinDireccion_NoMueveNada()
    {
        _pan.Move(Vector2.zero, 1f);

        Assert.AreEqual(Vector3.zero.x, _go.transform.position.x, 0.001f);
        Assert.AreEqual(Vector3.zero.z, _go.transform.position.z, 0.001f);
    }

    // Los limites se guardan como dos esquinas sueltas: si quien las edita
    // las deja al reves, se siguen entendiendo como un rectangulo valido.
    [Test]
    public void ApplyLimits_FuncionaAunqueLasEsquinasEstenAlReves()
    {
        _pan.limitMin = new Vector2(5f, 5f);
        _pan.limitMax = new Vector2(-5f, -5f);
        _go.transform.position = new Vector3(50f, 10f, 50f);

        _pan.ApplyLimits();

        Assert.AreEqual(5f, _go.transform.position.x, 0.001f);
        Assert.AreEqual(5f, _go.transform.position.z, 0.001f);
    }

    [Test]
    public void IsInsideLimits_DistingueDentroDeFuera()
    {
        _pan.limitMin = new Vector2(-5f, -5f);
        _pan.limitMax = new Vector2(5f, 5f);

        _go.transform.position = new Vector3(0f, 10f, 0f);
        Assert.IsTrue(_pan.IsInsideLimits());

        _go.transform.position = new Vector3(20f, 10f, 0f);
        Assert.IsFalse(_pan.IsInsideLimits());
    }

    [Test]
    public void Zoom_PositivoElevaLaCamaraSinMoverlaEnXZ()
    {
        _pan.Zoom(2f);

        Assert.AreEqual(12f, _go.transform.position.y, 0.001f);
        Assert.AreEqual(0f, _go.transform.position.x, 0.001f);
        Assert.AreEqual(0f, _go.transform.position.z, 0.001f);
    }

    [Test]
    public void Zoom_RespetaLosLimitesRelativosAAlturaInicial()
    {
        _pan.zoomInDistance = 2f;
        _pan.zoomOutDistance = 6f;

        _pan.Zoom(100f);
        Assert.AreEqual(16f, _go.transform.position.y, 0.001f);

        _pan.Zoom(-100f);
        Assert.AreEqual(8f, _go.transform.position.y, 0.001f);
    }

    [Test]
    public void Zoom_CadaPasoUsaLaDistanciaConfigurada()
    {
        _pan.zoomStep = 1.25f;

        _pan.Zoom(_pan.zoomStep);

        Assert.AreEqual(11.25f, _go.transform.position.y, 0.001f);
    }

    private static float DistanciaEnElPlano(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
