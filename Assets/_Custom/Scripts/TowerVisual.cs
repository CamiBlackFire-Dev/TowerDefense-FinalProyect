using System.Collections;
using UnityEngine;

namespace TowerDefense
{
    // Controla la apariencia de una torre: escala, color, aparicion y fusion.
    // Funciona con cualquier modelo 3D de forma plug & play:
    // - Si la torre tiene un hijo, ese hijo se usa como modelo.
    // - Si no hay ningun modelo, se crea un cubo simple.
    // - Detecta los Renderer de la raiz y de los hijos del modelo.
    // - Respeta la escala original del modelo importado.
    // El root de la torre se usa para moverse; el modelo visual se anima aparte.
    public class TowerVisual : MonoBehaviour
    {
        [Header("Modelo")]
        [SerializeField] private Transform visualRoot;        // objeto del modelo (auto si queda vacio)
        [SerializeField] private float baseOffsetY = 0f;      // altura extra manual del modelo

        [Header("Escala")]
        [SerializeField] private float baseScale = 0.75f;     // tamano del modelo (1 = tamano original)
        [SerializeField] private float scalePerLevel = 0.25f; // crecimiento por nivel (fraccion)
        [SerializeField] private Vector3 modelBaseScale = Vector3.zero; // escala original del modelo (se captura sola)

        [Header("Aparicion")]
        [SerializeField] private float spawnDuration = 0.18f; // duracion del crecimiento
        [SerializeField] private float spawnStartScale = 0.1f; // escala inicial al aparecer
        [SerializeField] private AnimationCurve spawnCurve;   // curva con pequeno rebote

        [Header("Fusion")]
        [SerializeField] private float[] mergePulsePerLevel;

        [Header("Colores")]
        [SerializeField] private Color[] levelColors;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _propertyBlock;
        private float _modelHalfHeight;
        private float _currentScaleFactor = 1f;
        private int _currentLevel = 1;
        private bool _setupDone;
        private Coroutine _spawnCoroutine;
        private Coroutine _pulseCoroutine;

        // Factor de escala actual (tamano del modelo en este nivel).
        public float CurrentScaleFactor
        {
            get { return _currentScaleFactor; }
        }

        private void Awake()
        {
            EnsureSetup();
        }

        private void OnValidate()
        {
            // En el editor solo se refresca si ya se inicializo antes.
            if (Application.isPlaying || !_setupDone)
                return;

            ApplyAppearance(_currentLevel);
        }

        // Aplica el nivel del modelo: escala y color.
        public void ApplyLevel(int level)
        {
            EnsureSetup();
            _currentLevel = Mathf.Max(1, level);
            _currentScaleFactor = baseScale * (1f + scalePerLevel * (_currentLevel - 1));
            ApplyScale(_currentScaleFactor);
            ApplyAppearance(_currentLevel);
        }

        // Altura que debe elevarse el root para apoyar el modelo en la celda.
        // Si el modelo es un hijo, el hijo se eleva solo y el root queda en el piso.
        public float GetStandingOffset()
        {
            EnsureSetup();
            if (visualRoot != transform)
                return 0f;

            return (_modelHalfHeight + baseOffsetY) * _currentScaleFactor;
        }

        // Anima la aparicion: la torre crece desde pequena con un rebote suave.
        public void PlaySpawnFeedback()
        {
            if (!Application.isPlaying || spawnDuration <= 0f)
                return;

            StopAllVisualFeedback();
            _spawnCoroutine = StartCoroutine(SpawnCoroutine());
        }

        // Si se reemplaza el modelo 3D por otro, se restablece la escala base
        // para volver a capturarla desde el nuevo modelo.
        [ContextMenu("Restablecer escala base del modelo")]
        private void ResetModelBaseScale()
        {
            modelBaseScale = Vector3.zero;
            _setupDone = false;
            if (!Application.isPlaying)
                ApplyLevel(_currentLevel);
        }

        // Anima la fusion: cambia de nivel y hace un pulso con destello.
        // El feedback es mas vistoso cuanto mayor es el nivel.
        // Si la torre se fusiona otra vez, se cancela el pulso anterior.
        public void PlayMergeFeedback(int level, float duration)
        {
            ApplyLevel(level);
            if (!Application.isPlaying || duration <= 0f)
                return;

            StopAllVisualFeedback();
            _pulseCoroutine = StartCoroutine(PulseCoroutine(duration));
        }

        // Detiene cualquier animacion visual previa y restaura la escala exacta.
        private void StopAllVisualFeedback()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            ApplyScale(_currentScaleFactor);
            ApplyAppearance(_currentLevel);
        }

        // Prepara las referencias del modelo. Se llama una sola vez.
        private void EnsureSetup()
        {
            if (_setupDone)
                return;

            _setupDone = true;
            EnsurePalette();

            // Si no hay ningun renderer, se crea un cubo simple como modelo.
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (_renderers.Length == 0)
                CreateFallbackCube();

            // Si no se asigno visualRoot, se busca un hijo; si no hay, la raiz.
            if (visualRoot == null)
            {
                if (transform.childCount > 0)
                    visualRoot = transform.GetChild(0);
                else
                    visualRoot = transform;
            }

            // La escala base del modelo se captura una sola vez y queda guardada.
            // Nunca se vuelve a capturar despues de escalar por nivel: si se
            // hiciera, cada carga de escena multiplicaria la escala otra vez
            // y las torres se encogerian cada vez que se entra en Play Mode.
            if (modelBaseScale == Vector3.zero)
                modelBaseScale = visualRoot.localScale;

            _modelHalfHeight = ComputeModelHalfHeight();

            if (spawnCurve == null)
                spawnCurve = DefaultSpawnCurve();

            EnsurePulseDefaults();
        }

        // Crea un cubo de relleno cuando la torre no trae modelo.
        private void CreateFallbackCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Visual";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = Vector3.one;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        // Media altura del modelo a escala base, para apoyarlo en la celda.
        private float ComputeModelHalfHeight()
        {
            if (_renderers == null || _renderers.Length == 0)
                return 0f;

            MeshFilter filter = _renderers[0].GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return 0f;

            float height = filter.sharedMesh.bounds.size.y;
            return height * modelBaseScale.y * 0.5f;
        }

        // Escala el modelo. Si es un hijo, tambien lo eleva para apoyarlo.
        private void ApplyScale(float scaleFactor)
        {
            visualRoot.localScale = modelBaseScale * scaleFactor;

            if (visualRoot != transform)
            {
                Vector3 local = visualRoot.localPosition;
                local.y = baseOffsetY + _modelHalfHeight * scaleFactor;
                visualRoot.localPosition = local;
            }
        }

        // Pinta todos los renderers del modelo con el color del nivel.
        private void ApplyAppearance(int level)
        {
            if (_renderers == null || _renderers.Length == 0)
                return;

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            Color color = LevelColor(level);
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", color);
                _renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }

        private IEnumerator SpawnCoroutine()
        {
            float target = _currentScaleFactor;
            float start = target * spawnStartScale;
            float t = 0f;
            while (t < spawnDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / spawnDuration);
                float eased = spawnCurve != null ? spawnCurve.Evaluate(progress) : progress;
                ApplyScale(Mathf.Lerp(start, target, eased));
                yield return null;
            }

            ApplyScale(target);
            _spawnCoroutine = null;
        }

        private IEnumerator PulseCoroutine(float duration)
        {
            float target = _currentScaleFactor;
            float pulseAmount = PulseScaleForLevel(_currentLevel);
            Color baseColor = LevelColor(_currentLevel);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / duration);

                // El pulso sube y baja con una curva senoidal.
                float pulse = Mathf.Sin(progress * Mathf.PI) * pulseAmount;
                ApplyScale(target * (1f + pulse));

                // Destello: el color se aclara hacia el blanco en la cima.
                float flash = Mathf.Sin(progress * Mathf.PI) * 0.5f;
                if (_propertyBlock != null && _renderers != null)
                {
                    for (int i = 0; i < _renderers.Length; i++)
                    {
                        _renderers[i].GetPropertyBlock(_propertyBlock);
                        _propertyBlock.SetColor("_BaseColor", Color.Lerp(baseColor, Color.white, flash));
                        _renderers[i].SetPropertyBlock(_propertyBlock);
                    }
                }

                yield return null;
            }

            ApplyScale(target);
            ApplyAppearance(_currentLevel);
            _pulseCoroutine = null;
        }

        // Intensidad del pulso segun el nivel alcanzado.
        private float PulseScaleForLevel(int towerLevel)
        {
            EnsurePulseDefaults();
            int index = Mathf.Clamp(towerLevel - 1, 0, mergePulsePerLevel.Length - 1);
            return mergePulsePerLevel[index];
        }

        private void EnsurePulseDefaults()
        {
            if (mergePulsePerLevel != null && mergePulsePerLevel.Length > 0)
                return;

            mergePulsePerLevel = new float[]
            {
                0.10f,  // nivel 1: pulso discreto
                0.16f,  // nivel 2: pulso claro
                0.24f,  // nivel 3: pulso marcado
                0.32f,  // nivel 4 o mas: pulso fuerte
            };
        }

        // Color del nivel pedido.
        private Color LevelColor(int level)
        {
            EnsurePalette();
            int colorIndex = Mathf.Clamp(level - 1, 0, levelColors.Length - 1);
            return levelColors[colorIndex];
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

        // Curva por defecto: crece rapido, rebota un poco y se asienta.
        private static AnimationCurve DefaultSpawnCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.6f, 1.15f),
                new Keyframe(1f, 1f));
        }
    }
}
