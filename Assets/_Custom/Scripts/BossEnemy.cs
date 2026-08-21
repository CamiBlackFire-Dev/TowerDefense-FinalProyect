using UnityEngine;

// Marca a un enemigo como jefe y cambia lo que pasa cuando llega al final
// del camino: en vez de desaparecer llevandose unas vidas de golpe (que es
// lo que hace un enemigo normal), se queda ahi golpeando el castillo cada
// pocos segundos hasta que lo maten.
//
// El efecto secundario importante es que la oleada no puede terminar
// mientras el jefe siga vivo: EnemySpawner solo la da por acabada cuando no
// queda nadie vivo, y el jefe no se descuenta al llegar. O sea que hay que
// derrotarlo para pasar de oleada.
[RequireComponent(typeof(EnemyHealth))]
public class BossEnemy : MonoBehaviour
{
    [Header("Golpes al castillo")]
    public int damagePerHit = 2;
    public float secondsBetweenHits = 5f;
    // El primer golpe no es inmediato: da un margen para reaccionar.
    public float firstHitDelay = 1f;

    [Header("Animacion")]
    public string attackTrigger = "Attack";

    private PlayerBase _playerBase;
    private Animator _animator;
    private float _timer;
    private bool _attacking;

    // True desde que llega al castillo y empieza a golpear.
    public bool IsAttackingCastle
    {
        get { return _attacking; }
    }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    // Lo llama EnemySpawner cuando el jefe termina el recorrido, en vez de
    // destruirlo como haria con un enemigo normal.
    public void StartAttackingCastle(PlayerBase playerBase)
    {
        if (_attacking)
            return;

        _playerBase = playerBase != null ? playerBase : PlayerBase.Instance;
        _attacking = true;
        _timer = Mathf.Max(0f, firstHitDelay);

        // Deja de caminar: ya llego.
        TestEnemyMovement movement = GetComponent<TestEnemyMovement>();
        if (movement != null)
            movement.enabled = false;
    }

    private void Update()
    {
        if (!_attacking || !Application.isPlaying)
            return;

        // Con el juego congelado (pausa o pantalla de fin) no debe seguir
        // pegando: Time.deltaTime ya viene en 0, pero se corta explicito
        // para que quede claro que es a proposito.
        if (Time.timeScale == 0f)
            return;

        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;

        _timer = Mathf.Max(0.1f, secondsBetweenHits);
        Hit();
    }

    private void Hit()
    {
        if (_playerBase == null)
            _playerBase = PlayerBase.Instance;

        if (_playerBase != null)
            _playerBase.TakeDamage(damagePerHit);

        if (_animator != null && !string.IsNullOrEmpty(attackTrigger))
            _animator.SetTrigger(attackTrigger);
    }
}
