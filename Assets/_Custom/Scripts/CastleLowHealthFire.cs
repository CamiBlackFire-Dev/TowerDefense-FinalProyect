using UnityEngine;

// Feedback visual del castillo: en cuanto las vidas del jugador bajan de la
// mitad del maximo, el castillo se prende fuego (en bucle). Se apaga solo
// si las vidas vuelven a subir por encima de la mitad (o se acaban del
// todo, ya no hace falta).
// Reemplaza al humo que tenia antes (CastleLowHealthSmoke): mismo patron de
// activar/desactivar, pero usando un VFX de fuego de verdad en vez de un
// ParticleSystem armado a mano.
public class CastleLowHealthFire : MonoBehaviour
{
    [Header("Referencias")]
    // Se busca solo en la escena si se deja vacio.
    public PlayerBase playerBase;
    // Prefab del fuego (Casual RPG VFX). Sin uno asignado no hay feedback,
    // pero tampoco rompe nada.
    public GameObject firePrefab;

    [Header("Fuego")]
    public Vector3 fireOffset = new Vector3(0f, 2f, 0f);
    public float fireScale = 1f;

    [Header("Audio")]
    // Sonido en bucle mientras arde. Va en un AudioSource propio (no el
    // AudioManager compartido, que solo hace disparos sueltos) porque tiene
    // que sonar y parar junto con el fuego, no una vez y listo. Vive en
    // este GameObject (el castillo) y no en _fireInstance: ese se apaga y
    // prende, y un AudioSource en un objeto inactivo no suena.
    public AudioClip fireLoopSound;
    [Range(0f, 1f)] public float fireLoopVolume = 0.5f;

    private GameObject _fireInstance;
    private ParticleSystem _fireRoot;
    private AudioSource _fireAudio;

    private void OnEnable()
    {
        if (playerBase == null)
            playerBase = PlayerBase.Instance;

        EnsureFire();

        if (playerBase != null)
        {
            playerBase.LivesChanged += HandleLivesChanged;
            UpdateFire(playerBase.Lives);
        }
    }

    private void OnDisable()
    {
        if (playerBase != null)
            playerBase.LivesChanged -= HandleLivesChanged;
    }

    private void HandleLivesChanged(int lives)
    {
        UpdateFire(lives);
    }

    // Arde solo con vidas por debajo de la mitad del maximo y mientras
    // siga habiendo castillo que defender.
    private void UpdateFire(int lives)
    {
        if (_fireInstance == null || playerBase == null)
            return;

        float half = playerBase.maxLives / 2f;
        bool shouldBurn = lives > 0 && lives < half;
        bool isActive = _fireInstance.activeSelf;

        if (shouldBurn && !isActive)
        {
            _fireInstance.SetActive(true);
            if (_fireRoot != null)
                _fireRoot.Play();

            EnsureFireAudio();
            if (_fireAudio != null)
                _fireAudio.Play();
        }
        else if (!shouldBurn && isActive)
        {
            if (_fireRoot != null)
                _fireRoot.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _fireInstance.SetActive(false);

            if (_fireAudio != null)
                _fireAudio.Stop();
        }
    }

    // El AudioSource del fuego se crea aparte (no en EnsureFire) porque
    // vive en este GameObject, siempre activo, mientras _fireInstance se
    // prende y apaga con cada oleada.
    private void EnsureFireAudio()
    {
        if (_fireAudio != null || fireLoopSound == null)
            return;

        _fireAudio = gameObject.AddComponent<AudioSource>();
        _fireAudio.clip = fireLoopSound;
        _fireAudio.loop = true;
        _fireAudio.playOnAwake = false;
        _fireAudio.spatialBlend = 1f; // 3D: mas fuerte cuanto mas cerca este la camara
        _fireAudio.volume = fireLoopVolume;
    }

    // Instancia el fuego una sola vez, desactivado, listo para prenderse.
    private void EnsureFire()
    {
        if (_fireInstance != null || firePrefab == null)
            return;

        _fireInstance = Instantiate(firePrefab, transform);
        _fireInstance.name = "LowHealthFire";
        _fireInstance.transform.localPosition = fireOffset;
        _fireInstance.transform.localRotation = Quaternion.identity;
        _fireInstance.transform.localScale = Vector3.one * fireScale;

        _fireRoot = _fireInstance.GetComponent<ParticleSystem>();
        _fireInstance.SetActive(false);
    }
}
