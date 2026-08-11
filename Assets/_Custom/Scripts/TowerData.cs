using UnityEngine;

namespace TowerDefense
{
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
        public GameObject visualPrefab;  // prefab opcional para este nivel
    }
}
