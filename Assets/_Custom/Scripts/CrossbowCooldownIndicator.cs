using UnityEngine;

[RequireComponent(typeof(CrossbowRainAttack))]
public class CrossbowCooldownIndicator : MonoBehaviour
{
    public float barWidth = 2f;
    public float barHeight = 0.22f;
    public float heightOffset = 3.8f;
    public Color rechargingColor = new Color(1f, 0.22f, 0.08f);
    public Color readyColor = new Color(0.2f, 1f, 0.35f);

    private CrossbowRainAttack _attack;
    private Transform _barRoot;
    private Transform _fill;
    private Renderer _fillRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private static Material _backgroundMaterial;
    private static Material _fillMaterial;

    private void Start()
    {
        _attack = GetComponent<CrossbowRainAttack>();
        CreateVisuals();
    }

    private void LateUpdate()
    {
        if (_attack == null || _barRoot == null)
            return;

        bool visible = _attack.IsRecharging;
        _barRoot.gameObject.SetActive(visible);
        if (!visible)
            return;

        _barRoot.position = transform.position + Vector3.up * heightOffset;
        if (Camera.main != null)
            _barRoot.rotation = Camera.main.transform.rotation;

        Vector3 scale = transform.lossyScale;
        _barRoot.localScale = new Vector3(
            scale.x != 0f ? 1f / scale.x : 1f,
            scale.y != 0f ? 1f / scale.y : 1f,
            scale.z != 0f ? 1f / scale.z : 1f);

        float progress = _attack.CooldownProgress;
        _fill.localScale = new Vector3(barWidth * progress, barHeight, 1f);
        _fill.localPosition = new Vector3(-barWidth * (1f - progress) * 0.5f, 0f, -0.01f);

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();
        _fillRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", Color.Lerp(rechargingColor, readyColor, progress));
        _fillRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void CreateVisuals()
    {
        _barRoot = new GameObject("CrossbowCooldownBar").transform;
        _barRoot.SetParent(transform, false);

        Transform background = CreateQuad("Background", GetBackgroundMaterial());
        background.SetParent(_barRoot, false);
        background.localScale = new Vector3(barWidth, barHeight, 1f);

        _fill = CreateQuad("Fill", GetFillMaterial());
        _fill.SetParent(_barRoot, false);
        _fillRenderer = _fill.GetComponent<Renderer>();
        _barRoot.gameObject.SetActive(false);
    }

    private static Transform CreateQuad(string objectName, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = objectName;
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
        quad.GetComponent<Renderer>().sharedMaterial = material;
        return quad.transform;
    }

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
