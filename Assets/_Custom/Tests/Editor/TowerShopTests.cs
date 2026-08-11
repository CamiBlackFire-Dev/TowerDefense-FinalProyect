using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TowerDefense
{
    // Pruebas de la compra de torres.
    public class TowerShopTests
    {
        private GameObject _root;

        [TearDown]
        public void Limpiar()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        // Comprar con dinero coloca una torre nivel 1 y descuenta el costo.
        [Test]
        public void TryBuyTower_ConDinero_ColocaTorreYDescuenta()
        {
            TowerShop shop = CrearSetup(50);

            bool comprada = shop.TryBuyTower();

            Assert.IsTrue(comprada);
            Assert.AreEqual(1, ContarTorres(shop));
            Assert.AreEqual(100, shop.GetComponent<EconomyManager>().Money);
        }

        // Comprar con el tablero lleno no gasta dinero.
        [Test]
        public void TryBuyTower_TableroLleno_RechazaSinGastar()
        {
            TowerShop shop = CrearSetup(50);
            BoardManager board = shop.GetComponent<BoardManager>();

            // Llenamos las 16 casillas con torres.
            for (int x = 0; x < board.Grid.Size; x++)
                for (int y = 0; y < board.Grid.Size; y++)
                    board.PlaceTower(x, y, 1);

            bool comprada = shop.TryBuyTower();

            Assert.IsFalse(comprada);
            Assert.AreEqual(150, shop.GetComponent<EconomyManager>().Money);
        }

        // Comprar sin dinero suficiente no coloca torres ni descuenta.
        [Test]
        public void TryBuyTower_SinDinero_Rechaza()
        {
            TowerShop shop = CrearSetup(200);

            bool comprada = shop.TryBuyTower();

            Assert.IsFalse(comprada);
            Assert.AreEqual(0, ContarTorres(shop));
            Assert.AreEqual(150, shop.GetComponent<EconomyManager>().Money);
        }

        // Cuenta las torres que hay en el tablero.
        private int ContarTorres(TowerShop shop)
        {
            BoardManager board = shop.GetComponent<BoardManager>();
            int count = 0;
            for (int x = 0; x < board.Grid.Size; x++)
                for (int y = 0; y < board.Grid.Size; y++)
                    if (board.Grid.GetLevel(x, y) > 0)
                        count++;

            return count;
        }

        // Arma un TowerShop conectado a un tablero y una economia de prueba.
        private TowerShop CrearSetup(int cost)
        {
            _root = new GameObject("Shop Test");
            BoardManager board = _root.AddComponent<BoardManager>();
            EconomyManager economy = _root.AddComponent<EconomyManager>();
            TowerShop shop = _root.AddComponent<TowerShop>();

            SetPrivateField(shop, "board", board);
            SetPrivateField(shop, "economy", economy);
            SetPrivateField(shop, "towerCost", cost);
            return shop;
        }

        // Asigna un campo privado desde la prueba (los campos serializados son privados).
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
