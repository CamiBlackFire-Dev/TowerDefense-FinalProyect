using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Pruebas de la forma del camino de los enemigos.
// Solo se revisa la matematica: no hace falta ver los modelos.
public class PathBuilderTests
{
    private GameObject _object;

    [TearDown]
    public void Limpiar()
    {
        if (_object != null)
            Object.DestroyImmediate(_object);
    }

    // Un tablero de 4x4 se rodea con 16 losetas.
    [Test]
    public void BuildPathPoints_TableroDe4_Crea16Losetas()
    {
        PathBuilder builder = CrearBuilder();

        List<Vector3> points = builder.BuildPathPoints();

        Assert.AreEqual(16, points.Count);
    }

    // El camino empieza arriba a la derecha y termina abajo a la derecha.
    [Test]
    public void BuildPathPoints_EmpiezaYTerminaEnElLadoDerecho()
    {
        PathBuilder builder = CrearBuilder();

        List<Vector3> points = builder.BuildPathPoints();

        Assert.AreEqual(5f, points[0].x, 0.01f);
        Assert.AreEqual(5f, points[0].z, 0.01f);
        Assert.AreEqual(5f, points[points.Count - 1].x, 0.01f);
        Assert.AreEqual(-5f, points[points.Count - 1].z, 0.01f);
    }

    // Las losetas van pegadas una detras de otra.
    [Test]
    public void BuildPathPoints_LasLosetasVanPegadas()
    {
        PathBuilder builder = CrearBuilder();

        List<Vector3> points = builder.BuildPathPoints();

        for (int i = 1; i < points.Count; i++)
        {
            float distancia = Vector3.Distance(points[i - 1], points[i]);
            Assert.AreEqual(builder.tileSize, distancia, 0.01f,
                "Salto raro entre la loseta " + (i - 1) + " y la " + i);
        }
    }

    // Una "C" cuadrada tiene exactamente dos esquinas.
    [Test]
    public void BuildPathPoints_TieneDosEsquinas()
    {
        PathBuilder builder = CrearBuilder();
        List<Vector3> points = builder.BuildPathPoints();

        int esquinas = 0;
        for (int i = 0; i < points.Count; i++)
            if (builder.IsCorner(points, i))
                esquinas++;

        Assert.AreEqual(2, esquinas);
    }

    // El camino rodea al tablero: ninguna loseta cae encima de las casillas.
    [Test]
    public void BuildPathPoints_NoPisaElTablero()
    {
        PathBuilder builder = CrearBuilder();
        float bordeDelTablero = builder.boardCells * builder.tileSize * 0.5f;

        List<Vector3> points = builder.BuildPathPoints();

        foreach (Vector3 point in points)
        {
            float distancia = Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z));
            Assert.Greater(distancia, bordeDelTablero,
                "La loseta en " + point + " se mete en el tablero");
        }
    }

    // La "C" esta abierta: el lado derecho solo tiene la entrada y la salida.
    [Test]
    public void BuildPathPoints_ElLadoDerechoQuedaAbierto()
    {
        PathBuilder builder = CrearBuilder();

        List<Vector3> points = builder.BuildPathPoints();

        int enElLadoDerecho = 0;
        foreach (Vector3 point in points)
            if (point.x > 4.9f)
                enElLadoDerecho++;

        Assert.AreEqual(2, enElLadoDerecho);
    }

    // En modo Manual el camino usa tal cual los puntos que se le asignen,
    // en el mismo orden (para integrarlo con un mapa hecho a mano).
    [Test]
    public void ModoManual_UsaLosWaypointsAsignados()
    {
        PathBuilder builder = CrearBuilder();
        builder.mode = PathMode.Manual;
        Transform a = CrearPunto("A");
        Transform b = CrearPunto("B");
        builder.manualWaypoints = new Transform[] { a, b };

        builder.Rebuild();

        Transform[] waypoints = builder.Path.GetWaypoints();
        Assert.AreEqual(2, waypoints.Length);
        Assert.AreSame(a, waypoints[0]);
        Assert.AreSame(b, waypoints[1]);
    }

    // Un hueco en el camino manual no rompe nada (solo se avisa por consola).
    [Test]
    public void ModoManual_ConHuecoNoRompe()
    {
        PathBuilder builder = CrearBuilder();
        builder.mode = PathMode.Manual;
        builder.manualWaypoints = new Transform[] { CrearPunto("A"), null };

        Assert.DoesNotThrow(() => builder.Rebuild());
    }

    private Transform CrearPunto(string nombre)
    {
        GameObject punto = new GameObject(nombre);
        punto.transform.SetParent(_object.transform);
        return punto.transform;
    }

    private PathBuilder CrearBuilder()
    {
        _object = new GameObject("Path Test");
        return _object.AddComponent<PathBuilder>();
    }
}
