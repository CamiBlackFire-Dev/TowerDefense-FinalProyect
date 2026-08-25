using UnityEngine;

// Permite comprar torres de nivel 1 con el dinero del jugador.
// La torre aparece sola en una celda libre aleatoria (sin drag & drop).
public class TowerShop : MonoBehaviour
{
    // Tablero por defecto: el que se usa en los niveles de un solo tablero.
    [SerializeField] private BoardManager board;
    [SerializeField] private EconomyManager economy;
    [SerializeField] private InputController inputController;
    [SerializeField] private EnemySpawner spawner;
    // En los niveles con varios tableros la torre tiene que aparecer en el
    // que el jugador tiene elegido, no siempre en el mismo. Se busca solo
    // en la escena si se deja vacio; si no hay ninguno se usa "board".
    [SerializeField] private BoardSelector boardSelector;
    [SerializeField] private int towerCost = 50;
    // Config global de oro; si esta puesta, el costo sale de ahi.
    [SerializeField] private GameEconomyConfig economyConfig;

    [Header("Audio")]
    [SerializeField] private AudioClip purchaseSound;
    // Se usa tanto si no alcanza el oro como si no hay celda libre o hay
    // una oleada en curso: cualquier intento de comprar que no funciona.
    [SerializeField] private AudioClip failureSound;

    public int TowerCost => economyConfig != null ? economyConfig.towerCost : towerCost;

    // Donde va a aparecer la torre: el tablero elegido si el nivel tiene
    // selector, y si no el de siempre.
    public BoardManager TargetBoard
    {
        get
        {
            if (boardSelector != null && boardSelector.SelectedBoard != null)
                return boardSelector.SelectedBoard;

            return board;
        }
    }

    private void OnEnable()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();
        if (boardSelector == null)
            boardSelector = FindFirstObjectByType<BoardSelector>();
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
        // Se resuelve una sola vez: si el jugador cambiara de tablero a
        // mitad de la compra, el sitio libre que se comprobo y el sitio
        // donde se coloca tienen que ser el mismo, o se cobraria el oro sin
        // llegar a poner la torre.
        BoardManager target = TargetBoard;

        if (target == null || economy == null)
            return false;

        if (spawner != null && spawner.IsRunning)
        {
            PlayFailureSound();
            return false;
        }

        // Si no hay celdas libres no se gasta dinero.
        if (!target.Grid.HasFreeCell())
        {
            PlayFailureSound();
            return false;
        }

        // Si el dinero no alcanza tampoco se gasta nada.
        if (!economy.TrySpend(TowerCost))
        {
            PlayFailureSound();
            return false;
        }

        bool placed;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "1_Tutorial"
            && (target.PlaceTower(0, 0, 1) || target.PlaceTower(2, 1, 1) || target.PlaceTower(1, 3, 1)))
        {
            placed = true;
        }
        else
        {
            // La torre aparece en una celda libre aleatoria.
            placed = target.PlaceTowerRandom(1);
        }

        if (placed && AudioManager.Instance != null && purchaseSound != null)
            AudioManager.Instance.PlaySFX(purchaseSound);

        return placed;
    }

    private void PlayFailureSound()
    {
        if (AudioManager.Instance != null && failureSound != null)
            AudioManager.Instance.PlaySFX(failureSound);
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
