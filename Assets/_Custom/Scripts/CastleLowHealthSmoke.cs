using UnityEngine;

// Feedback visual del castillo: mientras las vidas del jugador esten por
// debajo del umbral, sale humo del castillo en bucle. Se apaga solo si
// las vidas vuelven a subir (o se acaban del todo, ya no hace falta).
public class CastleLowHealthSmoke : MonoBehaviour
{
    [Header("Referencias")]
    // Se busca solo en la escena si se deja vacio.
    public PlayerBase playerBase;
    // Material del humo (sprite con alpha). Sin uno asignado el humo se
    // arma igual, pero se ve como un cuadrado blanco solido.
    public Material smokeMaterial;

    [Header("Humo")]
    public Vector3 smokeOffset = new Vector3(0f, 4f, 0f);
    // Gris bien oscuro: uno claro se pierde contra un cielo despejado.
    public Color smokeColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    public float smokeSize = 2f;
    public float emissionRate = 7f;

    [Header("Umbral")]
    public int lowHealthThreshold = 10;

    private ParticleSystem _smoke;

    private void OnEnable()
    {
        if (playerBase == null)
            playerBase = PlayerBase.Instance;

        EnsureSmoke();

        if (playerBase != null)
        {
            playerBase.LivesChanged += HandleLivesChanged;
            UpdateSmoke(playerBase.Lives);
        }
    }

    private void OnDisable()
    {
        if (playerBase != null)
            playerBase.LivesChanged -= HandleLivesChanged;
    }

    private void HandleLivesChanged(int lives)
    {
        UpdateSmoke(lives);
    }

    // Humea solo con vidas bajas y mientras siga habiendo castillo que defender.
    private void UpdateSmoke(int lives)
    {
        if (_smoke == null)
            return;

        bool shouldSmoke = lives > 0 && lives < lowHealthThreshold;
        bool isActive = _smoke.gameObject.activeSelf;

        if (shouldSmoke && !isActive)
        {
            _smoke.gameObject.SetActive(true);
            _smoke.Play();
        }
        else if (!shouldSmoke && isActive)
        {
            _smoke.gameObject.SetActive(false);
        }
    }

    // Arma el humo una sola vez, desactivado, listo para prenderse.
    private void EnsureSmoke()
    {
        if (_smoke != null)
            return;

        GameObject smokeGo = new GameObject("LowHealthSmoke");
        smokeGo.transform.SetParent(transform, false);
        smokeGo.transform.localPosition = smokeOffset;

        _smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(_smoke);
        smokeGo.SetActive(false);
    }

    // Humo simple: sube despacio, crece y se desvanece. Sin textura propia
    // (Sprites/Default sin sprite) se ve como un cuadrado; con smokeMaterial
    // asignado usa el sprite de humo que se le pase.
    private void ConfigureSmoke(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 2.5f;
        main.startSpeed = 0.5f;
        main.startSize = smokeSize;
        main.startColor = smokeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.25f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(smokeColor, 0f),
                new GradientColorKey(smokeColor, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(smokeColor.a, 0.25f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.6f);

        if (smokeMaterial != null)
        {
            ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = smokeMaterial;
        }
    }
}
