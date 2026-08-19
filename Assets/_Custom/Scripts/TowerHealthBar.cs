using UnityEngine;

// Barra de vida flotante sobre una torre.
// La crea TowerHealth al arrancar; se mantiene mirando a la camara y se
// achica segun la vida que le queda.
// Es el espejo de EnemyHealthBar (misma tecnica y mismos materiales), pero
// leyendo TowerHealth en vez de EnemyHealth.
public class TowerHealthBar : MonoBehaviour
{
    [Header("Tamanos")]
    public float barWidth = 1.1f;   // ancho de la barra
    public float barHeight = 0.14f; // alto de la barra

    [Header("Posicion")]
    public Vector3 barOffset = new Vector3(0f, 1.6f, 0f); // donde vive la barra, local a la torre

    [Header("Colores")]
    public Color fullColor = new Color(0.2f, 1f, 0.3f);  // vida llena
    public Color emptyColor = new Color(1f, 0.2f, 0.2f); // vida casi acabada

    [Header("Cuando se ve")]
    // Con el tablero lleno de torres, tener todas las barras encendidas es
    // mucho ruido visual. Asi solo aparece la de la torre que esta herida.
    // Apagalo si las quieres siempre visibles, como las de los enemigos.
    public bool hideWhenFull = true;

    private TowerHealth _health;
    private Transform _barRoot;
    private Transform _background;
    private Transform _fill;
    private Renderer _fillRenderer;
    private MaterialPropertyBlock _propertyBlock;

    // Materiales compartidos para no crear uno por torre.
    private static Material _backgroundMaterial;
    private static Material _fillMaterial;

    private void Start()
    {
        _health = GetComponent<TowerHealth>();
        CreateBarVisuals();
        PositionAboveTower();
    }

    private void LateUpdate()
    {
        if (_health == null || _barRoot == null)
            return;

        float pct = Mathf.Clamp01(_health.CurrentHealth / Mathf.Max(1f, _health.maxHealth));

        // Torre intacta (o ya sin vida): la barra se esconde.
        bool visible = _health.IsAlive && !(hideWhenFull && pct >= 1f);
        if (_barRoot.gameObject.activeSelf != visible)
            _barRoot.gameObject.SetActive(visible);

        if (!visible)
            return;

        // La barra siempre mira a la camara (la raiz de la torre no se gira).
        if (Camera.main != null)
            _barRoot.rotation = Camera.main.transform.rotation;

        UpdateFill(pct);
    }

    // Crea el fondo oscuro y la barra de color como hijos de un contenedor.
    private void CreateBarVisuals()
    {
        _barRoot = new GameObject("HealthBar").transform;
        _barRoot.SetParent(transform, false);

        _background = CreateQuad("Background", GetBackgroundMaterial());
        _background.SetParent(_barRoot.transform, false);
        _background.localScale = new Vector3(barWidth, barHeight, 1f);

        _fill = CreateQuad("Fill", GetFillMaterial());
        _fill.SetParent(_barRoot.transform, false);
        _fillRenderer = _fill.GetComponent<Renderer>();
    }

    // Coloca la barra en el offset elegido, local a la torre.
    // Con barOffset se edita la posicion completa (X, Y, Z) desde el Inspector.
    private void PositionAboveTower()
    {
        _barRoot.localPosition = barOffset;
    }

    // Actualiza el ancho y el color de la barra segun la vida.
    private void UpdateFill(float pct)
    {
        // El relleno crece desde la izquierda.
        _fill.localScale = new Vector3(barWidth * pct, barHeight, 1f);
        _fill.localPosition = new Vector3(-barWidth * (1f - pct) * 0.5f, 0f, 0f);

        Color color = Color.Lerp(emptyColor, fullColor, pct);
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        _fillRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", color);
        _fillRenderer.SetPropertyBlock(_propertyBlock);
    }

    // Crea un cuadrado 1x1 sin collider, listo para escalar.
    private Transform CreateQuad(string name, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;

        // El collider sobra: la barra es solo visual, y ademas la torre esta
        // en la capa Tower (donde los enemigos buscan a quien dispararle),
        // asi que un collider de mas aqui confundiria a su deteccion.
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        quad.GetComponent<Renderer>().sharedMaterial = material;
        return quad.transform;
    }

    // Los materiales salen de Resources, igual que los de la barra de los
    // enemigos: un material creado en tiempo de ejecucion sin que ningun
    // asset lo referencie no sobrevive el recorte de shaders de un build.
    private static Material GetBackgroundMaterial()
    {
        if (_backgroundMaterial == null)
            _backgroundMaterial = Resources.Load<Material>("HealthBarBackground");
        return _backgroundMaterial;
    }

    private static Material GetFillMaterial()
    {
        if (_fillMaterial == null)
            _fillMaterial = Resources.Load<Material>("HealthBarFill");
        return _fillMaterial;
    }
}
