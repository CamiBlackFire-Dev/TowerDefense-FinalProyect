using UnityEngine;

// Barra de vida flotante sobre un enemigo.
// La crea EnemyHealth al despertar; se mantiene mirando a la camara
// y se achica segun la vida que le queda.
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Tamanos")]
    public float barWidth = 1.6f;   // ancho de la barra
    public float barHeight = 0.18f; // alto de la barra
    public float offsetAbove = 0.4f; // altura extra sobre la cabeza del enemigo

    [Header("Colores")]
    public Color fullColor = new Color(0.2f, 1f, 0.3f); // vida llena
    public Color emptyColor = new Color(1f, 0.2f, 0.2f); // vida casi acabada

    private EnemyHealth _health;
    private Transform _barRoot;
    private Transform _background;
    private Transform _fill;
    private Renderer _fillRenderer;
    private MaterialPropertyBlock _propertyBlock;

    // Materiales compartidos para no crear uno por enemigo.
    private static Material _backgroundMaterial;
    private static Material _fillMaterial;

    private void Start()
    {
        _health = GetComponent<EnemyHealth>();
        CreateBarVisuals();
        PositionAboveEnemy();
    }

    private void LateUpdate()
    {
        if (_health == null)
            return;

        // Enemigo muerto: la barra desaparece.
        if (!_health.IsAlive)
        {
            if (_barRoot != null)
                _barRoot.gameObject.SetActive(false);
            return;
        }

        // La barra siempre mira a la camara (la raiz del enemigo no se gira).
        if (Camera.main != null)
            _barRoot.rotation = Camera.main.transform.rotation;

        UpdateFill();
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

    // Altura de la barra: encima del collider del enemigo.
    private void PositionAboveEnemy()
    {
        float halfHeight = 1f;
        Collider collider = GetComponentInChildren<Collider>();
        if (collider != null)
            halfHeight = collider.bounds.size.y * 0.5f;

        _barRoot.localPosition = new Vector3(0f, halfHeight + offsetAbove, 0f);
    }

    // Actualiza el ancho y el color de la barra segun la vida.
    private void UpdateFill()
    {
        float pct = Mathf.Clamp01(_health.CurrentHealth / Mathf.Max(1f, _health.maxHealth));

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

        // El collider sobra: la barra es solo visual.
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);

        quad.GetComponent<Renderer>().sharedMaterial = material;
        return quad.transform;
    }

    private static Material GetBackgroundMaterial()
    {
        if (_backgroundMaterial == null)
            _backgroundMaterial = CreateBarMaterial(new Color(0.08f, 0.08f, 0.1f, 0.85f));
        return _backgroundMaterial;
    }

    private static Material GetFillMaterial()
    {
        if (_fillMaterial == null)
            _fillMaterial = CreateBarMaterial(Color.white);
        return _fillMaterial;
    }

    // Material sin luces para que la barra se vea siempre bien.
    private static Material CreateBarMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material material = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
        material.color = color;
        return material;
    }
}
