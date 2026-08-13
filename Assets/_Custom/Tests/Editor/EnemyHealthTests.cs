using NUnit.Framework;
using UnityEngine;

// Pruebas de la vida de los enemigos y del pago al jugador.
public class EnemyHealthTests
{
    private GameObject _enemyObject;
    private GameObject _economyObject;

    [TearDown]
    public void Limpiar()
    {
        if (_enemyObject != null)
            Object.DestroyImmediate(_enemyObject);
        if (_economyObject != null)
            Object.DestroyImmediate(_economyObject);
    }

    // El dano baja la vida pero el enemigo sigue vivo.
    [Test]
    public void TakeDamage_DanoParcial_ElEnemigoSigueVivo()
    {
        EnemyHealth health = CrearEnemigo(20f, 10);

        health.TakeDamage(5f);

        Assert.AreEqual(15f, health.CurrentHealth, 0.01f);
        Assert.IsTrue(health.IsAlive);
    }

    // Al quedarse sin vida el enemigo paga al jugador.
    [Test]
    public void TakeDamage_AlMorir_PagaLaRecompensa()
    {
        EconomyManager economy = CrearEconomia();
        int dineroInicial = economy.Money;
        EnemyHealth health = CrearEnemigo(20f, 10, economy);

        health.TakeDamage(20f);

        Assert.AreEqual(dineroInicial + 10, economy.Money);
    }

    // Un golpe mas fuerte que la vida tampoco paga de mas.
    [Test]
    public void TakeDamage_GolpeEnorme_PagaUnaSolaVez()
    {
        EconomyManager economy = CrearEconomia();
        int dineroInicial = economy.Money;
        EnemyHealth health = CrearEnemigo(20f, 10, economy);

        health.TakeDamage(500f);

        Assert.AreEqual(dineroInicial + 10, economy.Money);
    }

    private EnemyHealth CrearEnemigo(float vida, int recompensa, EconomyManager economy = null)
    {
        _enemyObject = new GameObject("Enemy Test");
        EnemyHealth health = _enemyObject.AddComponent<EnemyHealth>();
        health.Setup(vida, recompensa, economy);
        return health;
    }

    private EconomyManager CrearEconomia()
    {
        _economyObject = new GameObject("Economy Test");
        return _economyObject.AddComponent<EconomyManager>();
    }
}
