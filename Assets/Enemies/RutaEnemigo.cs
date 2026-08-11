using UnityEngine;
using UnityEngine.AI;

public class RutaEnemigo : MonoBehaviour
{
    public NavMeshAgent enemigo;
    public Transform destino;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        enemigo.SetDestination(destino.position);
    }
}
