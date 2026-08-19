using UnityEngine;

// Vida de una torre. Hoy nada le hace dano todavia (es la base para que en
// el futuro los enemigos puedan atacar torres); TakeDamage ya esta lista
// para que ese sistema la use apenas exista.
// Al quedarse sin vida la torre no desaparece de una: baja un nivel y
// recupera la vida llena de ese nivel. Si ya estaba en nivel 1, ahi si se
// destruye y la casilla queda vacia. BoardManager es quien decide eso
// (escucha el evento Depleted), porque es el unico que conoce la cuadricula.
public class TowerHealth : MonoBehaviour
{
    [Header("Vida por nivel")]
    public TowerCatalog catalog;   // si esta vacio se usa maxHealth de abajo

    [Header("Valores por defecto")]
    public float maxHealth = 20f;

    private Tower _tower;
    private float _currentHealth;
    private bool _ready;
    private int _appliedLevel = -1;

    // Para una futura barra de vida sobre la torre (igual que EnemyHealthBar).
    public event System.Action<float> HealthChanged;
    // La vida llego a 0. BoardManager decide si baja de nivel o destruye la torre.
    public event System.Action<TowerHealth> Depleted;

    public float CurrentHealth
    {
        get
        {
            EnsureReady();
            return _currentHealth;
        }
    }

    public bool IsAlive
    {
        get { return CurrentHealth > 0f; }
    }

    private void OnEnable()
    {
        EnsureReady();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        RefreshStats();
    }

    // Le quita vida a la torre. Cuando llega a 0 avisa con Depleted; no se
    // destruye sola porque quien decide que pasa (bajar de nivel o
    // desaparecer) es BoardManager.
    public void TakeDamage(float amount)
    {
        EnsureReady();
        if (amount <= 0f || _currentHealth <= 0f)
            return;

        _currentHealth -= amount;
        if (_currentHealth < 0f)
            _currentHealth = 0f;

        if (HealthChanged != null)
            HealthChanged(_currentHealth);

        if (_currentHealth <= 0f && Depleted != null)
            Depleted(this);
    }

    // Copia la vida maxima que le toca al nivel actual y llena la vida.
    // Al igual que TowerAttack.RefreshStats, no hace nada si el nivel no
    // cambio desde la ultima vez (asi no se rellena la vida en cada frame).
    public void RefreshStats()
    {
        EnsureReady();

        if (_tower == null)
            _tower = GetComponent<Tower>();

        int level = _tower != null ? _tower.Level : 1;
        if (level == _appliedLevel)
            return;

        _appliedLevel = level;

        if (catalog != null)
        {
            TowerData data = catalog.GetLevelData(level);
            if (data != null)
                maxHealth = data.maxHealth;
        }

        _currentHealth = maxHealth;
        if (HealthChanged != null)
            HealthChanged(_currentHealth);
    }

    // Vuelve a leer el catalogo aunque el nivel no haya cambiado. La usa
    // BoardManager justo despues de bajar el nivel de la torre, para que
    // quede con la vida llena del nivel nuevo sin esperar al siguiente frame.
    public void ForceRefreshStats()
    {
        _appliedLevel = -1;
        RefreshStats();
    }

    // La vida se llena la primera vez que se usa. OnEnable ya llama esto,
    // pero se repite en TakeDamage/RefreshStats/CurrentHealth por si algo
    // los llama antes (por ejemplo en pruebas de editor, en un objeto que
    // arranca desactivado).
    private void EnsureReady()
    {
        if (_ready)
            return;

        _ready = true;
        _currentHealth = maxHealth;
    }
}
