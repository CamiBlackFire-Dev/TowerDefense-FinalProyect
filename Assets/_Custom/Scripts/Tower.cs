using UnityEngine;

namespace TowerDefense
{
    // Representa una torre y su apariencia basica.
    public class Tower : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private Color[] levelColors;
        [SerializeField] private float baseScale = 0.6f;
        [SerializeField] private float scalePerLevel = 0.15f;

        private MaterialPropertyBlock _propertyBlock;

        public int Level
        {
            get { return level; }
        }

        private void Awake()
        {
            ApplyAppearance();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyAppearance();
        }

        // Cambia el nivel y actualiza tamano y color.
        public void SetLevel(int newLevel)
        {
            level = Mathf.Max(1, newLevel);
            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            EnsurePalette();

            float size = baseScale + level * scalePerLevel;
            transform.localScale = new Vector3(size, size, size);

            Renderer renderer = GetComponent<Renderer>();
            if (renderer == null || levelColors.Length == 0)
                return;

            int colorIndex = Mathf.Clamp(level - 1, 0, levelColors.Length - 1);
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", levelColors[colorIndex]);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private void EnsurePalette()
        {
            if (levelColors != null && levelColors.Length > 0)
                return;

            levelColors = DefaultPalette();
        }

        // Paleta de colores por defecto para distinguir niveles en el prototipo.
        private static Color[] DefaultPalette()
        {
            return new Color[]
            {
                new Color(0.3f, 0.6f, 1f),
                new Color(0.3f, 1f, 0.5f),
                new Color(1f, 0.7f, 0.2f),
                new Color(1f, 0.3f, 0.3f),
                new Color(0.8f, 0.4f, 1f),
                new Color(1f, 1f, 1f),
            };
        }
    }
}
