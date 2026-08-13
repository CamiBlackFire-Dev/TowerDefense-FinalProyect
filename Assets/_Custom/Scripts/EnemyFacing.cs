using UnityEngine;

// Gira al enemigo hacia donde esta caminando.
// No lo mueve: solo mira como cambio su posicion, asi funciona igual
// con TestEnemyMovement o con cualquier otro sistema de movimiento.
public class EnemyFacing : MonoBehaviour
{
    [Header("Giro")]
    public float turnSpeed = 10f;   // que tan rapido voltea en las esquinas
    public float modelYaw = 0f;     // si el modelo mira al reves: 180

    private Vector3 _lastPosition;

    private void OnEnable()
    {
        _lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 avance = transform.position - _lastPosition;
        _lastPosition = transform.position;

        // Solo importa el giro en el piso, no si sube o baja.
        avance.y = 0f;
        if (avance.sqrMagnitude < 0.000001f)
            return;

        Quaternion objetivo = Quaternion.LookRotation(avance.normalized) *
            Quaternion.Euler(0f, modelYaw, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, objetivo,
            turnSpeed * Time.deltaTime);
    }
}
