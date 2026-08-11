using System.Collections;
using UnityEngine;

// Ancla logica de una torre en el tablero.
// No conoce la malla: toda la apariencia la maneja TowerVisual.
// Esto permite cambiar el cubo por cualquier modelo 3D sin tocar este codigo.
public class Tower : MonoBehaviour
{
    [SerializeField] private int level = 1;

    private TowerVisual _visual;

    public int Level
    {
        get { return level; }
    }

    // Componente visual de la torre (se crea solo si falta).
    public TowerVisual Visual
    {
        get
        {
            EnsureVisual();
            return _visual;
        }
    }

    private void Awake()
    {
        EnsureVisual();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (_visual == null)
            _visual = GetComponent<TowerVisual>();

        if (_visual != null)
            _visual.ApplyLevel(level);
    }

    // Cambia el nivel y actualiza la apariencia.
    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        Visual.ApplyLevel(level);
    }

    // Cambia el modelo 3D de la torre (el que toca segun el nivel).
    public void SetModel(GameObject modelPrefab)
    {
        Visual.SetModel(modelPrefab);
    }

    // Posicion final del root sobre la casilla, con el modelo apoyado.
    public Vector3 GetPositionOnCell(Vector3 anchor)
    {
        Vector3 position = anchor;
        position.y += Visual.GetStandingOffset();
        return position;
    }

    // Coloca el root sobre la casilla, con el modelo apoyado en la celda.
    public void PositionOnCell(Vector3 anchor)
    {
        transform.localPosition = GetPositionOnCell(anchor);
    }

    // Desliza la torre hasta una posicion local con suavizado.
    public void AnimateToLocalPosition(Vector3 target, float duration,
        AnimationCurve curve, System.Action onComplete)
    {
        StartCoroutine(SlideCoroutine(target, duration, curve, onComplete));
    }

    // Anima la aparicion de la torre (crece desde pequena).
    public void PlaySpawnFeedback()
    {
        Visual.PlaySpawnFeedback();
    }

    // Anima la fusion: nuevo nivel, pulso y destello.
    public void PlayMergeFeedback(int newLevel, float duration)
    {
        level = Mathf.Max(1, newLevel);
        Visual.PlayMergeFeedback(level, duration);
    }

    // Garantiza que la torre tenga un componente visual.
    private void EnsureVisual()
    {
        if (_visual != null)
            return;

        _visual = GetComponent<TowerVisual>();
        if (_visual == null)
            _visual = gameObject.AddComponent<TowerVisual>();

        _visual.ApplyLevel(level);
    }

    private IEnumerator SlideCoroutine(Vector3 target, float duration,
        AnimationCurve curve, System.Action onComplete)
    {
        Vector3 start = transform.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float eased = curve != null ? curve.Evaluate(progress) : progress;
            transform.localPosition = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }

        transform.localPosition = target;
        if (onComplete != null)
            onComplete();
    }
}
