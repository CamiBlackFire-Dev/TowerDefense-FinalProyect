using NUnit.Framework;
using UnityEngine;

// Que tipo de enemigo sale en cada oleada (enemyTypes) y cuando aparece el
// jefe. El jefe ahora sale como remate de su oleada, no al empezarla: eso
// solo se puede probar de verdad en Play Mode (depende de Update(), que en
// Edit Mode no corre), asi que aca solo se cubre lo que es logica pura:
// StartWave() ya no lo saca de una, y la eleccion de prefab por oleada.
public class EnemyTypesAndBossTimingTests
{
    private GameObject _root;

    [TearDown]
    public void Limpiar()
    {
        // CreateEnemy() instancia cada enemigo/jefe suelto en la raiz de la
        // escena, no como hijo del spawner: sin esto quedarian colgados
        // entre tests y un FindFirstObjectByType<BossEnemy>() de otro test
        // podria encontrar al jefe de este. EnemyHealth es el unico
        // componente que CreateEnemy garantiza en TODOS los casos (lo
        // agrega el mismo si el prefab no lo trae), asi que sirve para
        // encontrarlos sin importar que tan "falso" sea el prefab de prueba.
        foreach (EnemyHealth e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            Object.DestroyImmediate(e.gameObject);

        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void PickEnemyPrefab_SinEnemyTypes_UsaLaListaPlana()
    {
        EnemySpawner spawner = CrearSpawner();
        GameObject fallback = CrearFalso("Fallback");
        spawner.enemyPrefabs = new[] { fallback };

        Assert.AreSame(fallback, spawner.PickEnemyPrefab(1));
        Assert.AreSame(fallback, spawner.PickEnemyPrefab(20));
    }

    [Test]
    public void PickEnemyPrefab_RespetaLaVentanaDeOleadas()
    {
        EnemySpawner spawner = CrearSpawner();
        GameObject temprano = CrearFalso("Temprano");
        GameObject tardio = CrearFalso("Tardio");

        spawner.enemyTypes = new[]
        {
            new EnemyTypeEntry { label = "Temprano", prefab = temprano, weight = 1f, minWave = 1, maxWave = 3 },
            new EnemyTypeEntry { label = "Tardio", prefab = tardio, weight = 1f, minWave = 4, maxWave = 0 },
        };

        // Antes de la oleada 4 solo puede salir "Temprano".
        for (int i = 0; i < 20; i++)
            Assert.AreSame(temprano, spawner.PickEnemyPrefab(2));

        // De la 4 en adelante solo puede salir "Tardio" (Temprano ya cerro en la 3).
        for (int i = 0; i < 20; i++)
            Assert.AreSame(tardio, spawner.PickEnemyPrefab(5));
    }

    [Test]
    public void PickEnemyPrefab_SinNingunTipoActivoTodavia_CaeALaListaPlana()
    {
        EnemySpawner spawner = CrearSpawner();
        GameObject fallback = CrearFalso("Fallback");
        GameObject nuevo = CrearFalso("Nuevo");

        spawner.enemyPrefabs = new[] { fallback };
        spawner.enemyTypes = new[]
        {
            new EnemyTypeEntry { label = "Nuevo", prefab = nuevo, weight = 1f, minWave = 5, maxWave = 0 },
        };

        // En la oleada 1 "Nuevo" todavia no arranca: se cae a enemyPrefabs.
        Assert.AreSame(fallback, spawner.PickEnemyPrefab(1));

        // Desde la 5 ya puede salir "Nuevo".
        Assert.AreSame(nuevo, spawner.PickEnemyPrefab(5));
    }

    [Test]
    public void DescribeActiveEnemyTypes_ListaSoloLosActivosEnEsaOleada()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.enemyTypes = new[]
        {
            new EnemyTypeEntry { label = "Terrestre", prefab = CrearFalso("A"), weight = 1f, minWave = 1, maxWave = 0 },
            new EnemyTypeEntry { label = "Volador", prefab = CrearFalso("B"), weight = 1f, minWave = 6, maxWave = 0 },
        };

        Assert.AreEqual("Terrestre", spawner.DescribeActiveEnemyTypes(3));
        StringAssert.Contains("Volador", spawner.DescribeActiveEnemyTypes(6));
        StringAssert.Contains("Terrestre", spawner.DescribeActiveEnemyTypes(6));
    }

    // El cambio central del pedido: el jefe ya no sale apenas arranca la
    // oleada. Antes SpawnBossIfDue() se llamaba dentro de StartWave(); ahora
    // se llama desde Update() recien cuando ya no queda nada por spawnear,
    // asi que StartWave() por si sola no debe crear ningun BossEnemy.
    [Test]
    public void StartWave_YaNoSacaAlJefeDeInmediato()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.bossPrefab = CrearFalso("Jefe");
        spawner.bossWave = 1;

        spawner.StartWave();

        Assert.IsNull(Object.FindFirstObjectByType<BossEnemy>(),
            "el jefe no deberia existir todavia: recien sale al terminar los enemigos normales de la oleada");
    }

    // SpawnBoss() en si (el metodo que SI crea al jefe) sigue funcionando
    // igual que antes: lo sigue usando el boton "Sacar el jefe ahora".
    [Test]
    public void SpawnBoss_CreaUnBossEnemyQueNoDesaparece()
    {
        EnemySpawner spawner = CrearSpawner();
        spawner.bossPrefab = CrearFalso("Jefe");

        GameObject boss = spawner.SpawnBoss();

        Assert.IsNotNull(boss);
        Assert.IsNotNull(boss.GetComponent<BossEnemy>());
    }

    private EnemySpawner CrearSpawner()
    {
        _root = new GameObject("Spawner Test");
        EnemySpawner spawner = _root.AddComponent<EnemySpawner>();

        GameObject fake = CrearFalso("Enemigo Falso");
        spawner.enemyPrefabs = new[] { fake };
        spawner.pathBuilder = _root.AddComponent<PathBuilder>();
        return spawner;
    }

    private GameObject CrearFalso(string nombre)
    {
        GameObject fake = new GameObject(nombre);
        fake.transform.SetParent(_root.transform, false);
        return fake;
    }
}
