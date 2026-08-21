using NUnit.Framework;
using UnityEngine;

// Pruebas del aumento de dificultad por oleada. Son cuentas puras (no
// necesitan Play Mode), asi que se comprueban directas sobre los Get*ForWave
// que usa el spawner al crear cada enemigo y que la ventana de debug usa
// para mostrar la tabla de proximas oleadas.
public class EnemyWaveScalingTests
{
    private GameObject _root;

    [TearDown]
    public void Limpiar()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    // La oleada 1 es la base: sin aumento todavia.
    [Test]
    public void PrimeraOleada_UsaLosValoresBase()
    {
        EnemySpawner spawner = CrearSpawner();

        Assert.AreEqual(100f, spawner.GetEnemyHealthForWave(1), 0.001f);
        Assert.AreEqual(10f, spawner.GetEnemySpeedForWave(1), 0.001f);
        Assert.AreEqual(20f, spawner.GetShooterDamageForWave(1), 0.001f);
        Assert.AreEqual(10, spawner.GetEnemiesForWave(1));
    }

    // El aumento es lineal, no compuesto: la oleada 3 con 0.5 es +100%
    // (dos oleadas pasadas), no +125% como daria multiplicar dos veces.
    [Test]
    public void ElAumentoEsLinealNoCompuesto()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.healthGrowthPerWave = 0.5f;

        Assert.AreEqual(150f, spawner.GetEnemyHealthForWave(2), 0.001f);
        Assert.AreEqual(200f, spawner.GetEnemyHealthForWave(3), 0.001f);
        Assert.AreEqual(250f, spawner.GetEnemyHealthForWave(4), 0.001f);
    }

    [Test]
    public void SinAumento_TodasLasOleadasSonIguales()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.healthGrowthPerWave = 0f;
        spawner.speedGrowthPerWave = 0f;
        spawner.enemiesGrowthPerWave = 0;

        Assert.AreEqual(100f, spawner.GetEnemyHealthForWave(10), 0.001f);
        Assert.AreEqual(10f, spawner.GetEnemySpeedForWave(10), 0.001f);
        Assert.AreEqual(10, spawner.GetEnemiesForWave(10));
    }

    // Los enemigos de mas por oleada se suman, no se multiplican.
    [Test]
    public void CantidadDeEnemigos_SeSumaPorOleada()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.enemiesGrowthPerWave = 3;

        Assert.AreEqual(10, spawner.GetEnemiesForWave(1));
        Assert.AreEqual(13, spawner.GetEnemiesForWave(2));
        Assert.AreEqual(22, spawner.GetEnemiesForWave(5));
    }

    // enemiesPerWave en 0 significa "oleada sin fin": el aumento no debe
    // convertirla en una oleada normal con enemigos contados.
    [Test]
    public void OleadaSinFin_SigueSinFinAunqueHayaAumento()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.enemiesPerWave = 0;
        spawner.enemiesGrowthPerWave = 5;

        Assert.AreEqual(0, spawner.GetEnemiesForWave(1));
        Assert.AreEqual(0, spawner.GetEnemiesForWave(9));
    }

    // La proporcion de tiradores es un porcentaje: no puede pasarse de 1
    // por mucho que se sumen oleadas.
    [Test]
    public void ProporcionDeTiradores_NoSePasaDeUno()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.shooterChance = 0.5f;
        spawner.shooterChanceGrowthPerWave = 0.3f;

        Assert.AreEqual(0.8f, spawner.GetShooterChanceForWave(2), 0.001f);
        Assert.AreEqual(1f, spawner.GetShooterChanceForWave(5), 0.001f);
        Assert.AreEqual(1f, spawner.GetShooterChanceForWave(50), 0.001f);
    }

    // Oleada 0 (antes de arrancar la primera) no debe dar valores por
    // debajo de la base por restar oleadas "negativas".
    [Test]
    public void OleadaCero_NoBajaDeLaBase()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.healthGrowthPerWave = 0.5f;

        Assert.AreEqual(100f, spawner.GetEnemyHealthForWave(0), 0.001f);
    }

    private EnemySpawner CrearSpawner()
    {
        _root = new GameObject("Spawner Escalado Test");
        EnemySpawner spawner = _root.AddComponent<EnemySpawner>();

        // Numeros redondos para que las cuentas se lean de un vistazo.
        spawner.enemyHealth = 100f;
        spawner.enemySpeed = 10f;
        spawner.shooterDamage = 20f;
        spawner.enemiesPerWave = 10;
        return spawner;
    }
}
