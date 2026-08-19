using UnityEngine;

// Datos configurables de un nivel de torre.
// El balance se edita desde Unity creando assets con el menu
// Assets > Create > Tower Defense > Tower Data.
[CreateAssetMenu(fileName = "TowerLevel", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    public int level = 1;            // nivel de la torre (1 = torre basica)
    public float damage = 5f;        // dano por disparo
    public float range = 4f;         // alcance del ataque
    public float attackRate = 1f;    // disparos por segundo
    public float maxHealth = 20f;    // vida de la torre en este nivel
    public GameObject visualPrefab;  // prefab opcional para este nivel
}
