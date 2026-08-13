using UnityEngine;

// Vidas del jugador. Cada enemigo que llega al final del camino le quita vidas.
// Cuando se acaban avisa con el evento Defeated (lo usara el HUD y el game over).
public class PlayerBase : MonoBehaviour
{
    [Header("Vidas")]
    public int maxLives = 20;

    private static PlayerBase _instance;

    private int _lives;
    private bool _ready;

    // Avisos para la interfaz.
    public event System.Action<int> LivesChanged;
    public event System.Action Defeated;

    // Punto unico de acceso. Si cada script busca el suyo por separado
    // (FindFirstObjectByType) el orden de Awake/Start puede hacer que uno
    // cree su propio PlayerBase antes de que otro termine de buscar, y
    // cada uno le queda escuchando a una instancia distinta (el HUD se
    // desincroniza de las vidas reales). Con esto todos comparten la misma.
    public static PlayerBase Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<PlayerBase>();

            if (_instance == null && Application.isPlaying)
            {
                GameObject holder = new GameObject("PlayerBase");
                _instance = holder.AddComponent<PlayerBase>();
                Debug.Log("PlayerBase: no habia ninguno en la escena, se creo uno.", _instance);
            }

            return _instance;
        }
    }

    public int Lives
    {
        get
        {
            EnsureReady();
            return _lives;
        }
    }

    public bool IsAlive
    {
        get { return Lives > 0; }
    }

    private void Awake()
    {
        EnsureReady();
    }

    // Quita vidas cuando se escapa un enemigo.
    public void TakeDamage(int amount)
    {
        EnsureReady();
        if (amount <= 0 || _lives <= 0)
            return;

        _lives -= amount;
        if (_lives < 0)
            _lives = 0;

        if (LivesChanged != null)
            LivesChanged(_lives);

        Debug.Log("Un enemigo llego al final. Vidas restantes: " + _lives, this);

        if (_lives == 0 && Defeated != null)
            Defeated();
    }

    // Vuelve a llenar las vidas (para reiniciar la partida).
    public void ResetLives()
    {
        _ready = true;
        _lives = Mathf.Max(1, maxLives);

        if (LivesChanged != null)
            LivesChanged(_lives);
    }

    // Las vidas se llenan la primera vez que se usan.
    // Se hace asi porque en las pruebas de editor Awake no se ejecuta.
    private void EnsureReady()
    {
        if (_ready)
            return;

        _ready = true;
        _lives = Mathf.Max(1, maxLives);
    }
}
