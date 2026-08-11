using UnityEngine;

// Vida de un enemigo. Las torres le quitan vida con TakeDamage.
// Al morir paga al jugador y se quita de la escena.
public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 20f;
    public int reward = 10;         // dinero que deja al morir
    public GameObject deathEffect;  // efecto opcional al morir

    [Header("Muerte")]
    public float deathDelay = 1.2f;      // tiempo para que se vea la animacion
    public string dieTrigger = "Die";    // trigger del EnemyAnimator

    private EconomyManager _economy;
    private float _currentHealth;
    private bool _ready;

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

    private void Awake()
    {
        EnsureReady();
    }

    // El spawner define la vida y el pago de la oleada.
    public void Setup(float health, int money, EconomyManager economy)
    {
        maxHealth = Mathf.Max(1f, health);
        reward = Mathf.Max(0, money);
        _economy = economy;
        _currentHealth = maxHealth;
        _ready = true;
    }

    // Quita vida. Si llega a cero el enemigo muere.
    public void TakeDamage(float amount)
    {
        EnsureReady();
        if (amount <= 0f || _currentHealth <= 0f)
            return;

        _currentHealth -= amount;
        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _currentHealth = 0f;

        if (_economy != null)
            _economy.AddCurrency(reward);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // Se apaga el enemigo: deja de caminar y las torres dejan de verlo,
        // asi no le siguen disparando mientras cae.
        TestEnemyMovement movement = GetComponent<TestEnemyMovement>();
        if (movement != null)
            movement.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        PlayDeathAnimation();

        // Fuera de Play Mode (pruebas de editor) hay que borrar al instante.
        if (Application.isPlaying)
            Destroy(gameObject, deathDelay);
        else
            DestroyImmediate(gameObject);
    }

    // Lanza la animacion de muerte si el enemigo tiene animator.
    private void PlayDeathAnimation()
    {
        if (string.IsNullOrEmpty(dieTrigger))
            return;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.SetTrigger(dieTrigger);
    }

    // La vida se llena la primera vez que se usa.
    // Se hace asi porque en las pruebas de editor Awake no se ejecuta.
    private void EnsureReady()
    {
        if (_ready)
            return;

        _ready = true;
        _currentHealth = maxHealth;
    }
}
