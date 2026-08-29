using System.Collections.Generic;
using UnityEngine;

// Marca a un enemigo como jefe y cambia lo que pasa cuando llega al final
// del camino: en vez de desaparecer llevandose unas vidas de golpe (que es
// lo que hace un enemigo normal), se queda ahi golpeando el castillo cada
// pocos segundos hasta que lo maten.
//
// El efecto secundario importante es que la oleada no puede terminar
// mientras el jefe siga vivo: EnemySpawner solo la da por acabada cuando no
// queda nadie vivo, y el jefe no se descuenta al llegar. O sea que hay que
// derrotarlo para pasar de oleada.
[RequireComponent(typeof(EnemyHealth))]
public class BossEnemy : MonoBehaviour
{
    [Header("Golpes al castillo")]
    public int damagePerHit = 2;
    public float secondsBetweenHits = 5f;
    // El primer golpe no es inmediato: da un margen para reaccionar.
    public float firstHitDelay = 1f;

    [Header("Animacion")]
    public string attackTrigger = "Attack";

    private PlayerBase _playerBase;
    private Animator _animator;
    private float _timer;
    private bool _attacking;
    private readonly List<Material> _armorMaterials = new List<Material>();

    public int VisualTier { get; private set; }
    public float ScaleMultiplier { get; private set; } = 1f;

    // True desde que llega al castillo y empieza a golpear.
    public bool IsAttackingCastle
    {
        get { return _attacking; }
    }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public void ConfigurePresentation(int visualTier, float scaleMultiplier)
    {
        VisualTier = Mathf.Clamp(visualTier, 1, 3);
        ScaleMultiplier = Mathf.Max(1f, scaleMultiplier);
        transform.localScale = Vector3.one * ScaleMultiplier;

        Transform head = FindChild("B-head");
        Transform chest = FindChild("B-chest");
        Transform leftShoulder = FindChild("B-shoulder.L");
        Transform rightShoulder = FindChild("B-shoulder.R");

        if (VisualTier == 1)
        {
            CreateArmorPiece("BossArmor_StoneHelm", PrimitiveType.Sphere, head,
                new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.72f, 0.42f, 0.72f),
                new Color(0.28f, 0.22f, 0.16f), 0.05f, 0.18f);
            CreateArmorPiece("BossArmor_StoneBrow", PrimitiveType.Cube, head,
                new Vector3(0f, 0.15f, 0.31f), Vector3.zero, new Vector3(0.82f, 0.16f, 0.18f),
                new Color(0.38f, 0.29f, 0.18f), 0.05f, 0.15f);
            return;
        }

        Color steel = VisualTier == 2
            ? new Color(0.18f, 0.22f, 0.27f)
            : new Color(0.24f, 0.035f, 0.025f);
        Color trim = VisualTier == 2
            ? new Color(0.48f, 0.55f, 0.62f)
            : new Color(0.85f, 0.52f, 0.08f);

        CreateArmorPiece("BossArmor_WarHelm", PrimitiveType.Sphere, head,
            new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.78f, 0.48f, 0.78f),
            steel, 0.85f, 0.5f);
        CreateArmorPiece("BossArmor_Visor", PrimitiveType.Cube, head,
            new Vector3(0f, 0.08f, 0.31f), Vector3.zero, new Vector3(0.58f, 0.1f, 0.1f),
            trim, 0.75f, 0.55f);
        CreateArmorPiece("BossArmor_Chest", PrimitiveType.Cube, chest,
            new Vector3(0f, 0.05f, 0.18f), new Vector3(8f, 0f, 0f), new Vector3(0.78f, 0.38f, 0.12f),
            steel, 0.9f, 0.42f);
        CreateArmorPiece("BossArmor_LeftShoulder", VisualTier == 2 ? PrimitiveType.Cube : PrimitiveType.Sphere,
            leftShoulder, new Vector3(-0.1f, 0f, 0f), Vector3.zero, new Vector3(0.42f, 0.26f, 0.46f),
            steel, 0.9f, 0.4f);
        CreateArmorPiece("BossArmor_RightShoulder", VisualTier == 2 ? PrimitiveType.Cube : PrimitiveType.Sphere,
            rightShoulder, new Vector3(0.1f, 0f, 0f), Vector3.zero, new Vector3(0.42f, 0.26f, 0.46f),
            steel, 0.9f, 0.4f);
    }

    private Transform FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == childName)
                return child;

        return transform;
    }

    private void CreateArmorPiece(string pieceName, PrimitiveType primitiveType, Transform parent,
        Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Color color,
        float metallic, float smoothness)
    {
        GameObject piece = GameObject.CreatePrimitive(primitiveType);
        piece.name = pieceName;
        piece.layer = gameObject.layer;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localEulerAngles = localEulerAngles;
        piece.transform.localScale = localScale;

        Collider pieceCollider = piece.GetComponent<Collider>();
        if (pieceCollider != null)
        {
            pieceCollider.enabled = false;
            if (Application.isPlaying)
                Destroy(pieceCollider);
            else
                DestroyImmediate(pieceCollider);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            return;

        Material material = new Material(shader);
        material.name = pieceName + " Material";
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        piece.GetComponent<Renderer>().sharedMaterial = material;
        _armorMaterials.Add(material);
    }

    private void OnDestroy()
    {
        foreach (Material material in _armorMaterials)
        {
            if (material == null)
                continue;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }
    }

    // Lo llama EnemySpawner cuando el jefe termina el recorrido, en vez de
    // destruirlo como haria con un enemigo normal.
    public void StartAttackingCastle(PlayerBase playerBase)
    {
        if (_attacking)
            return;

        _playerBase = playerBase != null ? playerBase : PlayerBase.Instance;
        _attacking = true;
        _timer = Mathf.Max(0f, firstHitDelay);

        // Deja de caminar: ya llego.
        TestEnemyMovement movement = GetComponent<TestEnemyMovement>();
        if (movement != null)
            movement.enabled = false;
    }

    private void Update()
    {
        if (!_attacking || !Application.isPlaying)
            return;

        // Con el juego congelado (pausa o pantalla de fin) no debe seguir
        // pegando: Time.deltaTime ya viene en 0, pero se corta explicito
        // para que quede claro que es a proposito.
        if (Time.timeScale == 0f)
            return;

        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;

        _timer = Mathf.Max(0.1f, secondsBetweenHits);
        Hit();
    }

    private void Hit()
    {
        if (_playerBase == null)
            _playerBase = PlayerBase.Instance;

        if (_playerBase != null)
            _playerBase.TakeDamage(damagePerHit);

        if (_animator != null && !string.IsNullOrEmpty(attackTrigger))
            _animator.SetTrigger(attackTrigger);
    }
}
