using NUnit.Framework;
using UnityEngine;

// Pruebas del dinero del jugador.
public class EconomyManagerTests
{
    private GameObject _object;

    [TearDown]
    public void Limpiar()
    {
        if (_object != null)
            Object.DestroyImmediate(_object);
    }

    // Al iniciar, el dinero es el que diga StartingMoney. Se compara
    // contra la propiedad y no contra un numero escrito a mano: el oro
    // inicial es un valor de balance que se ajusta seguido (y ahora sale
    // del GameEconomyConfig global), asi que fijarlo aca haria fallar la
    // prueba cada vez que alguien lo retoca, sin que nada este roto.
    [Test]
    public void AlIniciar_ElDineroEsElInicial()
    {
        EconomyManager economy = CrearEconomy();

        Assert.AreEqual(economy.StartingMoney, economy.Money);
    }

    // Gastar dinero suficiente descuenta la cantidad correcta.
    [Test]
    public void TrySpend_ConSuficienteDinero_Descuenta()
    {
        EconomyManager economy = CrearEconomy(150);

        bool gastado = economy.TrySpend(50);

        Assert.IsTrue(gastado);
        Assert.AreEqual(100, economy.Money);
    }

    // Gastar mas dinero del disponible no cambia nada.
    [Test]
    public void TrySpend_SinDineroSuficiente_Rechaza()
    {
        EconomyManager economy = CrearEconomy(150);

        bool gastado = economy.TrySpend(200);

        Assert.IsFalse(gastado);
        Assert.AreEqual(150, economy.Money);
    }

    // Sumar dinero aumenta el total.
    [Test]
    public void AddCurrency_SumaAlDinero()
    {
        EconomyManager economy = CrearEconomy(150);

        economy.AddCurrency(30);

        Assert.AreEqual(180, economy.Money);
    }

    private EconomyManager CrearEconomy()
    {
        _object = new GameObject("Economy Test");
        return _object.AddComponent<EconomyManager>();
    }

    // Con un saldo de arranque concreto, para las pruebas que necesitan
    // numeros exactos sin depender del balance del momento.
    private EconomyManager CrearEconomy(int dineroInicial)
    {
        EconomyManager economy = CrearEconomy();
        economy.SetMoney(dineroInicial);
        return economy;
    }
}
