using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Feedback visual de que el tablero esta bloqueado (oleada en curso, sin el
// desbloqueo temporal activo): las casillas cambian a un color de aviso con
// un pulso de animacion, se quedan fijas en ese color mientras siga
// bloqueado, y vuelven al color original (con el mismo tipo de pulso) al
// desbloquearse. Misma tecnica que CastleDamageFeedback: tine el material
// de verdad via MaterialPropertyBlock, no agrega ningun objeto encima.
// No conoce oleadas ni powerups: solo escucha BoardManager.LockStateChanged.
public class BoardLockFeedback : MonoBehaviour
{
    [Header("Referencias")]
    // Se busca en este mismo objeto y si no en la escena.
    public BoardManager board;

    [Header("Bloqueo")]
    public Color lockedColor = new Color(1f, 0.35f, 0.1f); // color de aviso mientras esta bloqueado
    public float transitionDuration = 0.4f;                // cuanto tarda el pulso de cambio
    public float punchScale = 0.03f;                        // fraccion de crecimiento en el pico del pulso

    private Renderer[] _renderers;
    private Color[] _originalColors;
    private Color[] _currentColors;
    private MaterialPropertyBlock _propertyBlock;
    private Vector3[] _baseScales;
    private Coroutine _transitionCoroutine;
    private bool _colorsReady;

    private void Awake()
    {
        if (board == null)
            board = GetComponent<BoardManager>();
        if (board == null)
            board = FindFirstObjectByType<BoardManager>();
    }

    private void OnEnable()
    {
        if (board != null)
            board.LockStateChanged += HandleLockStateChanged;
    }

    private void OnDisable()
    {
        if (board != null)
            board.LockStateChanged -= HandleLockStateChanged;
    }

    private void HandleLockStateChanged(bool locked)
    {
        if (!Application.isPlaying)
            return;

        EnsureColorsCaptured();
        if (_renderers.Length == 0)
            return;

        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionCoroutine(locked));
    }

    // Junta las casillas segun el modo del tablero: en Procedural son los
    // cubos que crea BoardManager (Cells/*); en Anchored son los marcadores
    // que puso a mano el mapa del companero (anchoredCells).
    private void EnsureColorsCaptured()
    {
        if (_colorsReady)
            return;

        _propertyBlock = new MaterialPropertyBlock();

        List<Renderer> found = new List<Renderer>();
        if (board.mode == BoardMode.Anchored && board.anchoredCells != null)
        {
            foreach (Transform marker in board.anchoredCells)
            {
                if (marker != null)
                    found.AddRange(marker.GetComponentsInChildren<Renderer>(true));
            }
        }
        else
        {
            Transform cells = board.transform.Find("Cells");
            if (cells != null)
                found.AddRange(cells.GetComponentsInChildren<Renderer>(true));
        }

        _renderers = found.ToArray();
        _originalColors = new Color[_renderers.Length];
        _baseScales = new Vector3[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material material = _renderers[i].sharedMaterial;
            _originalColors[i] = (material != null && material.HasProperty("_BaseColor"))
                ? material.GetColor("_BaseColor")
                : Color.white;
            _baseScales[i] = _renderers[i].transform.localScale;
        }
        _currentColors = (Color[])_originalColors.Clone();

        _colorsReady = true;
    }

    // Pulso: cada casilla crece y vuelve a su tamano una sola vez mientras
    // su color se mueve del que tenga ahora mismo al de llegada (bloqueado
    // o el original, segun toque). Arranca desde el color actual y no desde
    // un extremo fijo para que un cambio de estado a mitad de pulso no salte.
    private IEnumerator TransitionCoroutine(bool locked)
    {
        Color[] startColors = (Color[])_currentColors.Clone();

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / transitionDuration);
            float intensity = Mathf.Sin(progress * Mathf.PI);

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                Color target = locked ? lockedColor : _originalColors[i];
                Color color = Color.Lerp(startColors[i], target, progress);
                _currentColors[i] = color;

                _renderers[i].transform.localScale = _baseScales[i] * (1f + punchScale * intensity);
                _renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", color);
                _renderers[i].SetPropertyBlock(_propertyBlock);
            }

            yield return null;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
                continue;

            Color target = locked ? lockedColor : _originalColors[i];
            _currentColors[i] = target;

            _renderers[i].transform.localScale = _baseScales[i];
            _renderers[i].GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", target);
            _renderers[i].SetPropertyBlock(_propertyBlock);
        }

        _transitionCoroutine = null;
    }
}
