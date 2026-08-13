using UnityEngine;
using UnityEngine.UIElements;

namespace Custom.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameHUDController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private Label _currencyText;
        private Label _waveText;
        private Button _optionsButton;
        private Button _startWaveButton;

        private VisualElement _bigAnnouncerContainer;
        private Label _bigAnnouncerText;

        private VisualElement _optionsOverlay;
        private Button _backButton;
        private Slider _masterVolumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;

        [Header("Referencias")]
        public EnemySpawner spawner;   // quien lanza las oleadas (se busca solo si queda vacio)
        public EconomyManager economy; // dinero que se muestra arriba

        [Header("Audio")]
        public AudioClip hoverSound;
        public AudioClip clickSound;
        public AudioClip waveAnnouncerSound;

        private int _currentCurrency = 0;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _currencyText = root.Q<Label>("CurrencyText");
            _waveText = root.Q<Label>("WaveText");
            _optionsButton = root.Q<Button>("OptionsButton");
            _startWaveButton = root.Q<Button>("StartWaveButton");

            _bigAnnouncerContainer = root.Q<VisualElement>("BigAnnouncerContainer");
            _bigAnnouncerText = root.Q<Label>("BigAnnouncerText");

            _optionsOverlay = root.Q<VisualElement>("OptionsOverlay");
            _backButton = root.Q<Button>("BackButton");
            _masterVolumeSlider = root.Q<Slider>("MasterVolumeSlider");
            _musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
            _sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");

            if (_optionsButton != null)
            {
                _optionsButton.clicked += OnOptionsClicked;
                _optionsButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_startWaveButton != null)
            {
                _startWaveButton.clicked += OnStartWaveClicked;
                _startWaveButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_backButton != null)
            {
                _backButton.clicked += OnBackClicked;
                _backButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (AudioManager.Instance != null)
            {
                if (_masterVolumeSlider != null)
                {
                    _masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
                    _masterVolumeSlider.RegisterValueChangedCallback(evt => AudioManager.Instance.SetMasterVolume(evt.newValue));
                }
                if (_musicVolumeSlider != null)
                {
                    _musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
                    _musicVolumeSlider.RegisterValueChangedCallback(evt => AudioManager.Instance.SetMusicVolume(evt.newValue));
                }
                if (_sfxVolumeSlider != null)
                {
                    _sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
                    _sfxVolumeSlider.RegisterValueChangedCallback(evt => AudioManager.Instance.SetSFXVolume(evt.newValue));
                }
            }

            if (spawner == null)
                spawner = FindFirstObjectByType<EnemySpawner>();
            if (economy == null)
                economy = FindFirstObjectByType<EconomyManager>();

            // La oleada avisa al HUD cuando arranca y cuando termina.
            if (spawner != null)
            {
                spawner.WaveStarted += OnWaveStarted;
                spawner.WaveFinished += OnWaveFinished;
                SetWaveActive(spawner.IsRunning);
            }

            // El dinero se actualiza solo cuando cambia.
            if (economy != null)
            {
                economy.MoneyChanged += OnMoneyChanged;
                UpdateCurrency(economy.Money);
            }
        }

        private void OnDisable()
        {
            if (_optionsButton != null)
            {
                _optionsButton.clicked -= OnOptionsClicked;
                _optionsButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_startWaveButton != null)
            {
                _startWaveButton.clicked -= OnStartWaveClicked;
                _startWaveButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_backButton != null)
            {
                _backButton.clicked -= OnBackClicked;
                _backButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (spawner != null)
            {
                spawner.WaveStarted -= OnWaveStarted;
                spawner.WaveFinished -= OnWaveFinished;
            }

            if (economy != null)
                economy.MoneyChanged -= OnMoneyChanged;
        }

        public void UpdateCurrency(int amount)
        {
            _currentCurrency = amount;
            if (_currencyText != null)
            {
                _currencyText.text = _currentCurrency.ToString();
            }
        }

        public void UpdateWave(int waveNumber)
        {
            if (_waveText != null)
            {
                _waveText.text = "OLEADA " + waveNumber;
            }
            ShowWaveAnnouncer(waveNumber);
        }

        public void SetWaveActive(bool isActive)
        {
            if (_startWaveButton != null)
            {
                _startWaveButton.style.display = isActive ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public void ShowWaveAnnouncer(int waveNumber)
        {
            if (_bigAnnouncerContainer == null || _bigAnnouncerText == null) return;

            _bigAnnouncerText.text = "WAVE " + waveNumber;

            if (AudioManager.Instance != null && waveAnnouncerSound != null)
                AudioManager.Instance.PlaySFX(waveAnnouncerSound);

            _bigAnnouncerContainer.AddToClassList("fade-in");

            _bigAnnouncerContainer.schedule.Execute(() =>
            {
                _bigAnnouncerContainer.RemoveFromClassList("fade-in");
            }).StartingIn(2500);
        }

        private void OnStartWaveClicked()
        {
            PlayClickSound();

            if (spawner == null)
            {
                Debug.LogWarning("GameHUD: falta el EnemySpawner, el boton no puede arrancar la oleada.", this);
                return;
            }

            spawner.StartWave();
        }

        // La oleada arranco: se muestra el numero y se oculta el boton.
        private void OnWaveStarted()
        {
            SetWaveActive(true);
            UpdateWave(spawner.WaveNumber);
        }

        // La oleada termino: vuelve a aparecer el boton para la siguiente.
        private void OnWaveFinished()
        {
            SetWaveActive(false);
        }

        // El dinero cambio: se refresca el numero de arriba.
        private void OnMoneyChanged(int money)
        {
            UpdateCurrency(money);
        }

        private void OnButtonHover(PointerEnterEvent evt)
        {
            if (AudioManager.Instance != null && hoverSound != null)
                AudioManager.Instance.PlaySFX(hoverSound, true);
        }

        private void PlayClickSound()
        {
            if (AudioManager.Instance != null && clickSound != null)
                AudioManager.Instance.PlaySFX(clickSound);
        }

        private void OnOptionsClicked()
        {
            PlayClickSound();
            if (_optionsOverlay != null) _optionsOverlay.style.display = DisplayStyle.Flex;
        }

        private void OnBackClicked()
        {
            PlayClickSound();
            if (_optionsOverlay != null) _optionsOverlay.style.display = DisplayStyle.None;
        }
    }
}
