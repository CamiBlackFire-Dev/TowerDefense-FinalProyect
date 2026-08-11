using UnityEngine;

namespace TowerDefense
{
    // Permite comprar torres de nivel 1 con el dinero del jugador.
    // La torre aparece sola en una celda libre aleatoria (sin drag & drop).
    public class TowerShop : MonoBehaviour
    {
        [SerializeField] private BoardManager board;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private InputController inputController;
        [SerializeField] private int towerCost = 50;

        private void OnEnable()
        {
            SubscribeToInput();
        }

        private void OnDisable()
        {
            if (inputController != null)
                inputController.BuyRequested -= HandleBuyRequested;
        }

        // Intenta comprar una torre. Devuelve false si falla.
        public bool TryBuyTower()
        {
            if (board == null || economy == null)
                return false;

            // Si no hay celdas libres no se gasta dinero.
            if (!board.Grid.HasFreeCell())
                return false;

            // Si el dinero no alcanza tampoco se gasta nada.
            if (!economy.TrySpend(towerCost))
                return false;

            // La torre aparece en una celda libre aleatoria.
            return board.PlaceTowerRandom(1);
        }

        private void SubscribeToInput()
        {
            if (!Application.isPlaying || inputController == null)
                return;

            inputController.BuyRequested -= HandleBuyRequested;
            inputController.BuyRequested += HandleBuyRequested;
        }

        private void HandleBuyRequested()
        {
            TryBuyTower();
        }
    }
}
