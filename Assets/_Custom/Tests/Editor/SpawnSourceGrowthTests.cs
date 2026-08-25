using NUnit.Framework;
using UnityEngine;

// GetEnemiesForSource: cada salida puede crecer con las oleadas por su
// cuenta (enemiesGrowthPerWave), igual que el spawner de salida unica ya
// hacia con GetEnemiesForWave. Antes de esto, una salida con enemiesPerWave
// fijo (que es como estan armados Level2 y Level3) mandaba SIEMPRE la misma
// cantidad sin importar la oleada -- esto lo arregla sin tocar el
// comportamiento de las salidas en 0, que siguen heredando el general.
public class SpawnSourceGrowthTests
{
    [Test]
    public void EnemiesPerWaveEnCero_HeredaElGeneralConSuPropioCrecimiento()
    {
        var spawner = new GameObject("Spawner").AddComponent<EnemySpawner>();
        spawner.enemiesPerWave = 8;
        spawner.enemiesGrowthPerWave = 2;
        var source = new SpawnSource { enemiesPerWave = 0 };

        int enOlaUno = spawner.GetEnemiesForSource(source, 1);
        int enOlaCinco = spawner.GetEnemiesForSource(source, 5);

        Object.DestroyImmediate(spawner.gameObject);

        Assert.AreEqual(8, enOlaUno);
        Assert.AreEqual(16, enOlaCinco); // 8 + 2*(5-1), igual que GetEnemiesForWave
    }

    [Test]
    public void EnemiesPerWaveFijo_SinCrecimientoPropio_SeQuedaFijo()
    {
        var spawner = new GameObject("Spawner").AddComponent<EnemySpawner>();
        var source = new SpawnSource { enemiesPerWave = 2, enemiesGrowthPerWave = 0f };

        int enOlaUno = spawner.GetEnemiesForSource(source, 1);
        int enOlaDiez = spawner.GetEnemiesForSource(source, 10);

        Object.DestroyImmediate(spawner.gameObject);

        // Este es el bug que tenian Level2 y Level3: sin enemiesGrowthPerWave
        // la salida manda lo mismo para siempre, sin importar que tan
        // avanzada este la partida.
        Assert.AreEqual(2, enOlaUno);
        Assert.AreEqual(2, enOlaDiez);
    }

    [Test]
    public void EnemiesPerWaveFijo_ConCrecimientoPropio_EscalaLinealmente()
    {
        var spawner = new GameObject("Spawner").AddComponent<EnemySpawner>();
        var source = new SpawnSource { enemiesPerWave = 3, enemiesGrowthPerWave = 0.5f };

        int enOlaUno = spawner.GetEnemiesForSource(source, 1);
        int enOlaDiez = spawner.GetEnemiesForSource(source, 10); // 3 + 0.5*9 = 7.5 -> 8 (redondeo)

        Object.DestroyImmediate(spawner.gameObject);

        Assert.AreEqual(3, enOlaUno);
        Assert.AreEqual(8, enOlaDiez);
    }

    [Test]
    public void EnemiesPerWaveFijo_NuncaBajaDeUno()
    {
        var spawner = new GameObject("Spawner").AddComponent<EnemySpawner>();
        var source = new SpawnSource { enemiesPerWave = 1, enemiesGrowthPerWave = 0f };

        int resultado = spawner.GetEnemiesForSource(source, 50);

        Object.DestroyImmediate(spawner.gameObject);

        Assert.AreEqual(1, resultado);
    }
}
