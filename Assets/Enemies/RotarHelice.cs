using UnityEngine;
using UnityEngine.AI;

public class RotarHelice : MonoBehaviour
{
    public Transform rotacion;
    public float speed;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rotacion.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}
