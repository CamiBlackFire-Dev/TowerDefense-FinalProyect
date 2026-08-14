using System.Collections;
using UnityEngine;

// Arrastre de la bomba desde la ranura de habilidades: mientras se sostiene
// presionada, un modelo de la bomba sigue al mouse/dedo en el mundo 3D y
// una zona roja parpadeante marca donde caeria si se suelta ahi.
// No sabe nada de UI Toolkit: GameHUDController le avisa cuando empieza y
// termina el arrastre, y le pasa la posicion del puntero en pantalla.
// (Nombre distinto de BombAbility a proposito: esa ya existe en
// Assets/JeanAssets/Scripts/Drone para la logica de explosion del dron.)
public class BombDragAbility : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject bombPrefab;
    public Camera worldCamera; // se busca Camera.main si se deja vacio

    [Header("Apuntado")]
    // Altura del plano de punteria (donde "vive" el tablero/camino).
    // El tablero de LevelPrueba esta a Y=0.39: por debajo de eso la zona
    // roja queda metida dentro de la malla y casi no se ve.
    public float groundHeight = 0.42f;
    // Cuanto flota la bomba por encima del suelo mientras se sostiene.
    public float holdHeight = 2f;

    [Header("Zona de caida")]
    public Material indicatorMaterial;
    public float indicatorRadius = 2.2f;
    public float indicatorHeightOffset = 0.1f; // separacion del suelo para no pisarse con el
    public float blinkInterval = 0.15f;

    [Header("Impacto")]
    // Cuanto tarda en bajar desde donde se solto hasta el suelo.
    public float fallDuration = 0.4f;
    // Curva de la caida: arranca lento y acelera, como si cayera por gravedad.
    public AnimationCurve fallCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 2f));
    // Cuanto se deja ver la bomba en el suelo despues de explotar antes de borrarla.
    public float destroyDelayAfterImpact = 0.25f;

    private GameObject _heldBomb;
    private GameObject _indicator;
    private Renderer _indicatorRenderer;
    private bool _dragging;
    private bool _hasValidTarget;
    private Vector3 _lastGroundPoint;
    private float _blinkTimer;

    public bool IsDragging
    {
        get { return _dragging; }
    }

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    // Arranca el arrastre: prepara (o reutiliza) el modelo flotante y la
    // zona de caida, los dos arrancan ocultos hasta el primer UpdateDrag.
    public void BeginDrag()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        EnsureVisuals();
        _dragging = true;
        _hasValidTarget = false;
        _blinkTimer = 0f;

        if (_heldBomb != null)
            _heldBomb.SetActive(true);
        if (_indicator != null)
        {
            _indicator.SetActive(true);
            _indicatorRenderer.enabled = true;
        }
    }

    // Se llama cada frame (o cada evento de puntero) mientras se sostiene,
    // con la posicion actual del mouse/dedo en pixeles de pantalla.
    public void UpdateDrag(Vector2 screenPosition)
    {
        if (!_dragging || worldCamera == null)
            return;

        if (TryGetGroundPoint(screenPosition, out Vector3 point))
        {
            _hasValidTarget = true;
            _lastGroundPoint = point;

            if (_heldBomb != null)
                _heldBomb.transform.position = point + Vector3.up * holdHeight;
            if (_indicator != null)
                _indicator.transform.position = point + Vector3.up * indicatorHeightOffset;
        }

        // Parpadeo: alterna la visibilidad de la zona a un ritmo fijo.
        _blinkTimer += Time.deltaTime;
        if (_blinkTimer >= blinkInterval)
        {
            _blinkTimer = 0f;
            if (_indicatorRenderer != null)
                _indicatorRenderer.enabled = !_indicatorRenderer.enabled;
        }
    }

    // Suelta la bomba: la deja caer suave desde donde estaba flotando hasta
    // la ultima posicion valida, y explota al tocar el suelo. Devuelve la
    // instancia creada (null si nunca hubo una posicion valida).
    public GameObject EndDrag()
    {
        if (!_dragging)
            return null;

        _dragging = false;
        Vector3 startPosition = _heldBomb != null ? _heldBomb.transform.position : _lastGroundPoint;
        if (_heldBomb != null)
            _heldBomb.SetActive(false);
        if (_indicator != null)
            _indicator.SetActive(false);

        if (!_hasValidTarget || bombPrefab == null)
            return null;

        GameObject dropped = Instantiate(bombPrefab, startPosition, Quaternion.identity);
        StartCoroutine(FallAndExplode(dropped, startPosition, _lastGroundPoint));
        return dropped;
    }

    // Baja la bomba hasta el suelo con la curva de caida y ahi la hace
    // explotar reutilizando BombAbility (el sistema de Jean para el dron).
    private IEnumerator FallAndExplode(GameObject bomb, Vector3 startPosition, Vector3 groundPosition)
    {
        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / fallDuration);
            float eased = fallCurve != null ? fallCurve.Evaluate(progress) : progress;
            if (bomb == null)
                yield break;
            bomb.transform.position = Vector3.LerpUnclamped(startPosition, groundPosition, eased);
            yield return null;
        }

        if (bomb == null)
            yield break;

        bomb.transform.position = groundPosition;

        BombAbility explosion = bomb.GetComponent<BombAbility>();
        if (explosion == null)
            explosion = bomb.AddComponent<BombAbility>();
        explosion.ExplodeAt(groundPosition);

        Destroy(bomb, destroyDelayAfterImpact);
    }

    // Cancela sin soltar nada (por ejemplo si el arrastre se interrumpe).
    public void CancelDrag()
    {
        _dragging = false;
        if (_heldBomb != null)
            _heldBomb.SetActive(false);
        if (_indicator != null)
            _indicator.SetActive(false);
    }

    // Interseca el rayo de la camara con un plano horizontal a groundHeight.
    private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
        if (ground.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    // Crea el modelo flotante y la zona de caida la primera vez que hacen
    // falta. Los dos quedan como hijos de este objeto, ocultos hasta usarse.
    private void EnsureVisuals()
    {
        if (_heldBomb == null && bombPrefab != null)
        {
            _heldBomb = Instantiate(bombPrefab, transform);
            _heldBomb.name = "HeldBombPreview";
            _heldBomb.SetActive(false);
        }

        if (_indicator == null)
        {
            _indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _indicator.name = "BombDropIndicator";
            _indicator.transform.SetParent(transform, false);

            Collider indicatorCollider = _indicator.GetComponent<Collider>();
            if (indicatorCollider != null)
                Destroy(indicatorCollider);

            _indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _indicator.transform.localScale = Vector3.one * indicatorRadius;

            _indicatorRenderer = _indicator.GetComponent<Renderer>();
            if (indicatorMaterial != null)
                _indicatorRenderer.sharedMaterial = indicatorMaterial;

            _indicator.SetActive(false);
        }
    }
}
