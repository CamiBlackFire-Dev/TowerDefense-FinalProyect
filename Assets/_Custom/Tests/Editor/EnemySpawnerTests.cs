using NUnit.Framework;
using UnityEngine;

// Pruebas de los avisos de oleada: el HUD se sincroniza con estos eventos.
public class EnemySpawnerTests
{
    private GameObject _root;
    private int _startedCount;
    private int _finishedCount;

    // Cada test arranca con contadores en cero, sin importar el anterior.
    [SetUp]
    public void Reiniciar()
    {
        _startedCount = 0;
        _finishedCount = 0;
    }

    [TearDown]
    public void Limpiar()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    // Arrancar una oleada avisa a la interfaz y activa el spawner.
    [Test]
    public void StartWave_AvisaYActiva()
    {
        EnemySpawner spawner = CrearSpawner();

        spawner.StartWave();

        Assert.AreEqual(1, _startedCount);
        Assert.IsTrue(spawner.IsRunning);
        Assert.AreEqual(1, spawner.WaveNumber);
    }

    // Detener la oleada avisa a la interfaz y la desactiva.
    [Test]
    public void StopWave_AvisaYDesactiva()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.StartWave();

        spawner.StopWave();

        Assert.AreEqual(1, _finishedCount);
        Assert.IsFalse(spawner.IsRunning);
    }

    // Cada oleada nueva sube el numero.
    [Test]
    public void StartWave_SubeElNumeroDeOleada()
    {
        EnemySpawner spawner = CrearSpawner();

        spawner.StartWave();
        spawner.StopWave();
        spawner.StartWave();

        Assert.AreEqual(2, spawner.WaveNumber);
        Assert.AreEqual(2, _startedCount);
    }

    // Detener sin haber arrancado no avisa a la interfaz.
    [Test]
    public void StopWave_SinOleadaActiva_NoAvisa()
    {
        EnemySpawner spawner = CrearSpawner();

        spawner.StopWave();

        Assert.AreEqual(0, _finishedCount);
        Assert.IsFalse(spawner.IsRunning);
    }

    // Arma un spawner de prueba con camino y un enemigo falso.
    private EnemySpawner CrearSpawner()
    {
        _root = new GameObject("Spawner Test");
        EnemySpawner spawner = _root.AddComponent<EnemySpawner>();

        GameObject fake = new GameObject("Enemigo Falso");
        fake.transform.SetParent(_root.transform, false);
        spawner.enemyPrefabs = new GameObject[] { fake };
        spawner.pathBuilder = _root.AddComponent<PathBuilder>();

        spawner.WaveStarted += () => _startedCount++;
        spawner.WaveFinished += () => _finishedCount++;
        return spawner;
    }
}
