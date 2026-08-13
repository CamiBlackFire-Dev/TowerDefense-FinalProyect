using UnityEngine;
using DamageNumbersPro;

// Proyectil que sale de una torre y persigue a su objetivo.
// Al llegar le hace dano, muestra el numero de dano y se destruye.
public class TowerProjectile : MonoBehaviour
{
    [Header("Vuelo")]
    public float speed = 10f;        // velocidad en unidades por segundo
    public float hitDistance = 0.4f; // distancia a la que se considera impacto
    public float maxLifetime = 5f;   // si el objetivo huye, la bala se apaga

    [Header("Impacto")]
    public DamageNumber popupPrefab;  // numero de dano (Damage Numbers Pro)
    public GameObject impactEffect;   // efecto opcional donde pega

    private Transform _target;
    private float _damage;
    private float _life;

    // La torre le pasa a quien perseguir y cuanto dano hace.
    public void Setup(Transform target, float damage, DamageNumber popup, GameObject effect)
    {
        _target = target;
        _damage = damage;
        popupPrefab = popup;
        impactEffect = effect;
    }

    private void Update()
    {
        MoveStep(Time.deltaTime);
    }

    // Un paso del vuelo. Publico para poder probarlo sin Play Mode.
    public void MoveStep(float deltaTime)
    {
        _life += deltaTime;

        // Si el objetivo ya no existe (o murio), el proyectil se va sin pegar.
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            DestroyObject(gameObject);
            return;
        }

        // Si el enemigo huye mas rapido que la bala, esta se apaga sola.
        if (_life >= maxLifetime)
        {
            DestroyObject(gameObject);
            return;
        }

        Vector3 haciaObjetivo = _target.position - transform.position;
        float distancia = haciaObjetivo.magnitude;
        float paso = speed * deltaTime;

        // Si el paso llega mas lejos que el objetivo, se aterriza en el
        // (asi una bala rapida nunca pasa de largo sin pegar).
        if (paso >= distancia)
        {
            transform.position = _target.position;
            Hit();
            return;
        }

        transform.position += haciaObjetivo.normalized * paso;
    }

    // Pega en el objetivo: dano, numero flotante y efecto opcional.
    private void Hit()
    {
        EnemyHealth health = _target.GetComponent<EnemyHealth>();
        if (health != null)
            health.TakeDamage(_damage);

        if (popupPrefab != null)
            popupPrefab.Spawn(transform.position, _damage);

        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        DestroyObject(gameObject);
    }

    private void DestroyObject(GameObject target)
    {
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
