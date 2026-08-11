using NUnit.Framework;
using UnityEngine;

namespace TowerDefense
{
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

        // Al iniciar el dinero es el configurado (150 por defecto).
        [Test]
        public void AlIniciar_ElDineroEsElInicial()
        {
            EconomyManager economy = CrearEconomy();

            Assert.AreEqual(150, economy.Money);
        }

        // Gastar dinero suficiente descuenta la cantidad correcta.
        [Test]
        public void TrySpend_ConSuficienteDinero_Descuenta()
        {
            EconomyManager economy = CrearEconomy();

            bool gastado = economy.TrySpend(50);

            Assert.IsTrue(gastado);
            Assert.AreEqual(100, economy.Money);
        }

        // Gastar mas dinero del disponible no cambia nada.
        [Test]
        public void TrySpend_SinDineroSuficiente_Rechaza()
        {
            EconomyManager economy = CrearEconomy();

            bool gastado = economy.TrySpend(200);

            Assert.IsFalse(gastado);
            Assert.AreEqual(150, economy.Money);
        }

        // Sumar dinero aumenta el total.
        [Test]
        public void AddCurrency_SumaAlDinero()
        {
            EconomyManager economy = CrearEconomy();

            economy.AddCurrency(30);

            Assert.AreEqual(180, economy.Money);
        }

        private EconomyManager CrearEconomy()
        {
            _object = new GameObject("Economy Test");
            return _object.AddComponent<EconomyManager>();
        }
    }
}
