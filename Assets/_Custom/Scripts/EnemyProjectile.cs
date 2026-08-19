using UnityEngine;
using DamageNumbersPro;

// Disparo que sale de un enemigo y persigue a la torre a la que apunta.
// Al llegar le hace dano, muestra el numero y se destruye.
// Es el espejo de TowerProjectile (que va de torre a enemigo): se mantienen
// separados porque cada uno le pega a un tipo de vida distinto y asi
// ninguno de los dos tiene que preguntar "que soy" en cada impacto.
public class EnemyProjectile : MonoBehaviour
{
    [Header("Vuelo")]
    public float speed = 8f;         // velocidad en unidades por segundo
    public float maxLifetime = 5f;   // si nunca llega, la bala se apaga sola

    [Header("Impacto")]
    public DamageNumber popupPrefab;  // numero de dano (Damage Numbers Pro)
    public GameObject impactEffect;   // efecto opcional donde pega

    private Transform _target;
    private float _damage;
    private float _life;

    // El enemigo le pasa a quien perseguir y cuanto dano hace.
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

        // Si la torre ya no existe (la destruyeron o bajo de nivel), la
        // bala se va sin pegar.
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            DestroyObject(gameObject);
            return;
        }

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

    // Pega en la torre: dano, numero flotante y efecto opcional.
    private void Hit()
    {
        TowerHealth health = _target.GetComponentInParent<TowerHealth>();
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
