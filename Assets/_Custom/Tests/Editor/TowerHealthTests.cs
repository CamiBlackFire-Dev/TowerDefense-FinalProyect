using NUnit.Framework;
using UnityEngine;

// Pruebas de la vida de las torres (independiente del tablero: la
// integracion de "que pasa cuando llega a 0" se prueba en BoardManagerTests,
// que es quien de verdad decide si baja de nivel o desaparece).
public class TowerHealthTests
{
    private GameObject _towerObject;
    private TowerCatalog _catalog;
    private TowerData _level1;
    private TowerData _level2;

    [TearDown]
    public void Limpiar()
    {
        if (_towerObject != null)
            Object.DestroyImmediate(_towerObject);
        if (_catalog != null)
            Object.DestroyImmediate(_catalog);
        if (_level1 != null)
            Object.DestroyImmediate(_level1);
        if (_level2 != null)
            Object.DestroyImmediate(_level2);
    }

    // El dano baja la vida pero la torre sigue en pie.
    [Test]
    public void TakeDamage_DanoParcial_LaTorreSigueViva()
    {
        TowerHealth health = CrearTorre(1, 20f);

        health.TakeDamage(5f);

        Assert.AreEqual(15f, health.CurrentHealth, 0.01f);
        Assert.IsTrue(health.IsAlive);
    }

    // Un golpe mas fuerte que la vida no la deja en negativo.
    [Test]
    public void TakeDamage_GolpeEnorme_LaVidaNoBajaDeCero()
    {
        TowerHealth health = CrearTorre(1, 20f);

        health.TakeDamage(500f);

        Assert.AreEqual(0f, health.CurrentHealth, 0.01f);
        Assert.IsFalse(health.IsAlive);
    }

    // Dano cero o negativo no hace nada.
    [Test]
    public void TakeDamage_DanoCero_NoCambiaLaVida()
    {
        TowerHealth health = CrearTorre(1, 20f);

        health.TakeDamage(0f);
        health.TakeDamage(-5f);

        Assert.AreEqual(20f, health.CurrentHealth, 0.01f);
    }

    // Llegar a 0 avisa una sola vez con Depleted, no en cada golpe posterior.
    [Test]
    public void TakeDamage_AlLlegarACero_AvisaConDepletedUnaSolaVez()
    {
        TowerHealth health = CrearTorre(1, 10f);
        int avisos = 0;
        health.Depleted += _ => avisos++;

        health.TakeDamage(10f);
        health.TakeDamage(5f); // ya esta en 0, no deberia avisar de nuevo

        Assert.AreEqual(1, avisos);
    }

    // Con catalogo asignado, la vida maxima sale del nivel de la torre.
    [Test]
    public void RefreshStats_UsaLaVidaDelCatalogoSegunElNivel()
    {
        TowerHealth health = CrearTorreConCatalogo(2);

        health.RefreshStats();

        Assert.AreEqual(35f, health.CurrentHealth, 0.01f);
    }

    // ForceRefreshStats vuelve a llenar la vida aunque el nivel no cambie
    // (lo usa BoardManager justo despues de bajar el nivel de una torre).
    [Test]
    public void ForceRefreshStats_RellenaLaVida()
    {
        TowerHealth health = CrearTorre(1, 20f);
        health.TakeDamage(15f);

        health.ForceRefreshStats();

        Assert.AreEqual(20f, health.CurrentHealth, 0.01f);
    }

    private TowerHealth CrearTorre(int nivel, float vida)
    {
        _towerObject = new GameObject("Tower Health Test");

        Tower tower = _towerObject.AddComponent<Tower>();
        tower.SetLevel(nivel);

        TowerHealth health = _towerObject.AddComponent<TowerHealth>();
        health.maxHealth = vida;
        return health;
    }

    private TowerHealth CrearTorreConCatalogo(int nivel)
    {
        _towerObject = new GameObject("Tower Health Test");

        Tower tower = _towerObject.AddComponent<Tower>();
        tower.SetLevel(nivel);

        _level1 = ScriptableObject.CreateInstance<TowerData>();
        _level1.level = 1;
        _level1.maxHealth = 20f;

        _level2 = ScriptableObject.CreateInstance<TowerData>();
        _level2.level = 2;
        _level2.maxHealth = 35f;

        _catalog = ScriptableObject.CreateInstance<TowerCatalog>();
        _catalog.levels = new TowerData[] { _level1, _level2 };

        TowerHealth health = _towerObject.AddComponent<TowerHealth>();
        health.catalog = _catalog;
        return health;
    }
}
