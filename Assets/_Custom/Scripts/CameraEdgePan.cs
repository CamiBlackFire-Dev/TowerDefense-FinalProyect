using UnityEngine;
using UnityEngine.InputSystem;

// Mueve la camara cuando el puntero se acerca a un borde de la pantalla,
// como en los juegos de estrategia. Cada nivel marca sus propios limites
// para que no se pueda perder de vista el tablero.
// Se configura desde Tower Defense > Game Debug (seccion "Camara").
public class CameraEdgePan : MonoBehaviour
{
    [Header("Movimiento")]
    public bool panEnabled = true;
    // Franja en pixeles desde cada borde que activa el movimiento.
    public float edgeThickness = 40f;
    // Unidades por segundo. No depende de la velocidad del juego (x1/x2/x3).
    public float panSpeed = 18f;

    [Header("Zoom")]
    public bool zoomEnabled = true;
    public float zoomStep = 1.25f;
    public float zoomInDistance = 2f;
    public float zoomOutDistance = 6f;

    [Header("Limites del nivel (plano XZ)")]
    // Rectangulo donde puede estar la camara. Se marca por nivel: fuera de
    // esto no se puede mover, asi no se pierde de vista el tablero.
    public Vector2 limitMin = new Vector2(-10f, -20f);
    public Vector2 limitMax = new Vector2(10f, 5f);
    // Dibuja el rectangulo en la vista de escena aunque no este seleccionada.
    public bool drawLimitsGizmo = true;

    private float _initialHeight;
    private bool _zoomHeightInitialized;

    public float MinZoomHeight
    {
        get
        {
            EnsureZoomHeight();
            return _initialHeight - Mathf.Max(0f, zoomInDistance);
        }
    }

    public float MaxZoomHeight
    {
        get
        {
            EnsureZoomHeight();
            return _initialHeight + Mathf.Max(0f, zoomOutDistance);
        }
    }

    private void Awake()
    {
        EnsureZoomHeight();
    }

    private void Update()
    {
        if (!panEnabled || !Application.isPlaying)
            return;

        // Con el juego congelado (pausa, victoria o derrota) hay un menu
        // encima: mover la camara por detras se siente raro. Los tres
        // ponen timeScale en 0, asi que sirve como senal comun sin tener
        // que preguntarle al HUD por cada overlay.
        if (Time.timeScale == 0f)
            return;

        HandleZoomInput();

        Vector2 direction;
        if (!TryGetEdgeDirection(out direction))
            return;

        // unscaledDeltaTime a proposito: con el juego en x2 o x3, deltaTime
        // viene multiplicado y la camara saldria disparada. La velocidad de
        // la camara es cosa del jugador, no del ritmo de la partida.
        Move(direction, Time.unscaledDeltaTime);
    }

    private void HandleZoomInput()
    {
        if (!zoomEnabled || Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        Zoom(-Mathf.Sign(scroll) * zoomStep);
    }

    public void Zoom(float heightDelta)
    {
        if (heightDelta == 0f)
            return;

        EnsureZoomHeight();
        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y + heightDelta, MinZoomHeight, MaxZoomHeight);
        transform.position = position;
    }

    private void EnsureZoomHeight()
    {
        if (_zoomHeightInitialized)
            return;

        _initialHeight = transform.position.y;
        _zoomHeightInitialized = true;
    }

    // Que direccion pide el puntero segun el borde que este tocando.
    // Devuelve false si no toca ningun borde o si el puntero se fue de la
    // ventana (en WebGL la ultima posicion se queda congelada en el borde:
    // sin este corte la camara seguiria moviendose sola).
    private bool TryGetEdgeDirection(out Vector2 direction)
    {
        direction = Vector2.zero;

        Pointer pointer = Pointer.current;
        if (pointer == null)
            return false;

        Vector2 position = pointer.position.ReadValue();

        bool insideScreen = position.x >= 0f && position.x <= Screen.width
            && position.y >= 0f && position.y <= Screen.height;
        if (!insideScreen)
            return false;

        if (position.x <= edgeThickness)
            direction.x = -1f;
        else if (position.x >= Screen.width - edgeThickness)
            direction.x = 1f;

        if (position.y <= edgeThickness)
            direction.y = -1f;
        else if (position.y >= Screen.height - edgeThickness)
            direction.y = 1f;

        return direction != Vector2.zero;
    }

    // Mueve la camara sobre el plano del suelo y la deja dentro de los
    // limites. Publico para poder probarlo sin depender del puntero.
    public void Move(Vector2 direction, float deltaTime)
    {
        if (direction == Vector2.zero)
            return;

        // La camara mira inclinada hacia abajo, asi que su forward apunta
        // al suelo: se aplana para que "arriba" en pantalla sea "adelante"
        // en el tablero y no un acercamiento.
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            // Camara mirando recto hacia abajo: su forward no da direccion
            // util, se usa la del mundo.
            forward = Vector3.forward;
        }
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 movement = (right * direction.x + forward * direction.y).normalized;
        transform.position += movement * panSpeed * deltaTime;

        ApplyLimits();
    }

    // Deja la camara dentro del rectangulo. La altura (Y) no se toca.
    public void ApplyLimits()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, Mathf.Min(limitMin.x, limitMax.x), Mathf.Max(limitMin.x, limitMax.x));
        position.z = Mathf.Clamp(position.z, Mathf.Min(limitMin.y, limitMax.y), Mathf.Max(limitMin.y, limitMax.y));
        transform.position = position;
    }

    // True si la camara ya esta dentro de los limites marcados.
    public bool IsInsideLimits()
    {
        Vector3 position = transform.position;
        return position.x >= Mathf.Min(limitMin.x, limitMax.x)
            && position.x <= Mathf.Max(limitMin.x, limitMax.x)
            && position.z >= Mathf.Min(limitMin.y, limitMax.y)
            && position.z <= Mathf.Max(limitMin.y, limitMax.y);
    }

    private void OnDrawGizmos()
    {
        if (!drawLimitsGizmo)
            return;

        DrawLimitsGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        // Seleccionada se ve siempre, aunque el gizmo permanente este apagado.
        if (!drawLimitsGizmo)
            DrawLimitsGizmo();
    }

    private void DrawLimitsGizmo()
    {
        float minX = Mathf.Min(limitMin.x, limitMax.x);
        float maxX = Mathf.Max(limitMin.x, limitMax.x);
        float minZ = Mathf.Min(limitMin.y, limitMax.y);
        float maxZ = Mathf.Max(limitMin.y, limitMax.y);
        float y = transform.position.y;

        Vector3 a = new Vector3(minX, y, minZ);
        Vector3 b = new Vector3(maxX, y, minZ);
        Vector3 c = new Vector3(maxX, y, maxZ);
        Vector3 d = new Vector3(minX, y, maxZ);

        Gizmos.color = IsInsideLimits() ? Color.cyan : Color.red;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}
