using System.Collections;
using UnityEngine;

// Feedback visual del castillo: cuando un enemigo llega al final del
// camino, el castillo pulsa un poco y se tine de rojo un momento.
// No conoce el combate ni las vidas: solo escucha EnemySpawner.EnemyReachedEnd.
public class CastleDamageFeedback : MonoBehaviour
{
    [Header("Referencias")]
    // Se busca solo en la escena si se deja vacio.
    public EnemySpawner spawner;

    [Header("Golpe")]
    public Color flashColor = new Color(1f, 0.15f, 0.15f);
    public float flashDuration = 0.35f;
    public float punchScale = 0.06f; // fraccion de crecimiento en el pico del golpe

    private Renderer[] _renderers;
    private Color[] _originalColors;
    private MaterialPropertyBlock _propertyBlock;
    private Vector3 _baseScale;
    private Coroutine _feedbackCoroutine;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _propertyBlock = new MaterialPropertyBlock();

        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material material = _renderers[i].sharedMaterial;
            _originalColors[i] = (material != null && material.HasProperty("_BaseColor"))
                ? material.GetColor("_BaseColor")
                : Color.white;
        }
    }

    private void OnEnable()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();

        if (spawner != null)
            spawner.EnemyReachedEnd += HandleEnemyReachedEnd;
    }

    private void OnDisable()
    {
        if (spawner != null)
            spawner.EnemyReachedEnd -= HandleEnemyReachedEnd;
    }

    private void HandleEnemyReachedEnd()
    {
        if (!Application.isPlaying || flashDuration <= 0f)
            return;

        if (_feedbackCoroutine != null)
            StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = StartCoroutine(FlashCoroutine());
    }

    // Pulso senoidal: sube y baja una sola vez, en escala y en color a la vez.
    private IEnumerator FlashCoroutine()
    {
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / flashDuration);
            float intensity = Mathf.Sin(progress * Mathf.PI);

            transform.localScale = _baseScale * (1f + punchScale * intensity);

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", Color.Lerp(_originalColors[i], flashColor, intensity));
                _renderers[i].SetPropertyBlock(_propertyBlock);
            }

            yield return null;
        }

        transform.localScale = _baseScale;
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].SetPropertyBlock(null);

        _feedbackCoroutine = null;
    }
}
