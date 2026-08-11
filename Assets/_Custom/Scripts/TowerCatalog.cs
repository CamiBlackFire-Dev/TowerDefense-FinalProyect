using UnityEngine;

namespace TowerDefense
{
    // Catalogo con los datos de todos los niveles de torre.
    // El indice 0 guarda el nivel 1, el indice 1 el nivel 2, y asi sucesivamente.
    // Se crea desde Assets > Create > Tower Defense > Tower Catalog.
    [CreateAssetMenu(fileName = "TowerCatalog", menuName = "Tower Defense/Tower Catalog")]
    public class TowerCatalog : ScriptableObject
    {
        public TowerData[] levels;

        // Devuelve los datos del nivel pedido, o null si no existe.
        public TowerData GetLevelData(int level)
        {
            if (levels == null || level < 1 || level > levels.Length)
                return null;

            return levels[level - 1];
        }
    }
}
