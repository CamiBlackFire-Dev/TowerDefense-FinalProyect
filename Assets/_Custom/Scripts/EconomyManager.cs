using System;
using UnityEngine;

namespace TowerDefense
{
    // Maneja el dinero del jugador. No conoce torres ni combate:
    // las recompensas por eliminar enemigos entraran por AddCurrency.
    public class EconomyManager : MonoBehaviour
    {
        [SerializeField] private int startingMoney = 150;

        private int _money;
        private bool _initialized;

        // Se dispara cada vez que cambia el dinero (lo usara el HUD mas adelante).
        public event Action<int> MoneyChanged;

        public int Money
        {
            get
            {
                EnsureInitialized();
                return _money;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        // Suma dinero al jugador, por ejemplo al matar enemigos.
        public void AddCurrency(int amount)
        {
            EnsureInitialized();
            if (amount <= 0)
                return;

            _money += amount;
            NotifyMoneyChanged();
        }

        // Intenta gastar dinero. Devuelve false si no alcanza.
        public bool TrySpend(int amount)
        {
            EnsureInitialized();
            if (amount < 0 || _money < amount)
                return false;

            _money -= amount;
            NotifyMoneyChanged();
            return true;
        }

        // Inicializa el dinero la primera vez que se usa.
        // Se llama desde Awake y desde cada metodo publico porque, fuera de
        // Play Mode (por ejemplo en pruebas de editor), Awake no se ejecuta.
        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;
            _money = startingMoney;
        }

        private void NotifyMoneyChanged()
        {
            if (MoneyChanged != null)
                MoneyChanged(_money);
        }
    }
}
