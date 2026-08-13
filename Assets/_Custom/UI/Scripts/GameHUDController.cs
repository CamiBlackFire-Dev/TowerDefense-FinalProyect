using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Custom.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameHUDController : MonoBehaviour
    {
        #region UI Elements
        private UIDocument _uiDocument;
        private Label _currencyText;
        private Label _healthText;
        private VisualElement _healthBarFill;
        private Label _waveText;
        private Button _optionsButton;
        private Button _startWaveButton;
        private Button _buyTowerButton;

        private Button _ability1;
        private Button _ability2;
        private Button _ability3;
        private Button _ability4;

        private VisualElement _bigAnnouncerContainer;
        private Label _bigAnnouncerText;

        private VisualElement _optionsOverlay;
        private Button _backButton;
        private Slider _masterVolumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;

        private VisualElement _pauseOverlay;
        private Button _resumeButton;
        private Button _pauseOptionsButton;
        private Button _quitMenuButton;

        private VisualElement _victoryOverlay;
        private Button _nextLevelButton;
        private Button _victoryMenuButton;

        private VisualElement _defeatOverlay;
        private Button _retryButton;
        private Button _defeatMenuButton;

        private VisualElement _gameCompleteOverlay;
        private Button _viewCreditsButton;
        private Button _gameCompleteMenuButton;

        private VisualElement _creditsOverlay;
        private Button _githubButton;
        private Button _closeCreditsButton;
        #endregion

        #region State & Dependencies
        private bool _isPaused = false;
        private bool _optionsFromPause = false;

        [Header("Audio")]
        public AudioClip hoverSound;
        public AudioClip clickSound;
        public AudioClip waveAnnouncerSound;

        private int _currentCurrency = 0;
        private EconomyManager _economyManager;
        private TowerShop _towerShop;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _currencyText = root.Q<Label>("CurrencyText");
            _healthText = root.Q<Label>("HealthText");
            _healthBarFill = root.Q<VisualElement>("HealthBarFill");
            _waveText = root.Q<Label>("WaveText");
            _optionsButton = root.Q<Button>("OptionsButton");
            _startWaveButton = root.Q<Button>("StartWaveButton");
            _buyTowerButton = root.Q<Button>("BuyTowerButton");

            _bigAnnouncerContainer = root.Q<VisualElement>("BigAnnouncerContainer");
            _bigAnnouncerText = root.Q<Label>("BigAnnouncerText");

            _optionsOverlay = root.Q<VisualElement>("OptionsOverlay");
            _backButton = root.Q<Button>("BackButton");
            _masterVolumeSlider = root.Q<Slider>("MasterVolumeSlider");
            _musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
            _sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");

            _pauseOverlay = root.Q<VisualElement>("PauseOverlay");
            _resumeButton = root.Q<Button>("ResumeButton");
            _pauseOptionsButton = root.Q<Button>("PauseOptionsButton");
            _quitMenuButton = root.Q<Button>("QuitMenuButton");

            _victoryOverlay = root.Q<VisualElement>("VictoryOverlay");
            _nextLevelButton = root.Q<Button>("NextLevelButton");
            _victoryMenuButton = root.Q<Button>("VictoryMenuButton");

            _defeatOverlay = root.Q<VisualElement>("DefeatOverlay");
            _retryButton = root.Q<Button>("RetryButton");
            _defeatMenuButton = root.Q<Button>("DefeatMenuButton");

            _gameCompleteOverlay = root.Q<VisualElement>("GameCompleteOverlay");
            _viewCreditsButton = root.Q<Button>("ViewCreditsButton");
            _gameCompleteMenuButton = root.Q<Button>("GameCompleteMenuButton");

            _creditsOverlay = root.Q<VisualElement>("CreditsOverlay");
            _githubButton = root.Q<Button>("GithubButton");
            _closeCreditsButton = root.Q<Button>("CloseCreditsButton");

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

            if (_buyTowerButton != null)
            {
                _buyTowerButton.clicked += OnBuyTowerClicked;
                _buyTowerButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_backButton != null)
            {
                _backButton.clicked += OnBackClicked;
                _backButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_resumeButton != null)
            {
                _resumeButton.clicked += OnResumeClicked;
                _resumeButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_pauseOptionsButton != null)
            {
                _pauseOptionsButton.clicked += OnPauseOptionsClicked;
                _pauseOptionsButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_quitMenuButton != null)
            {
                _quitMenuButton.clicked += OnQuitMenuClicked;
                _quitMenuButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_nextLevelButton != null) { _nextLevelButton.clicked += OnNextLevelClicked; _nextLevelButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_victoryMenuButton != null) { _victoryMenuButton.clicked += OnQuitMenuClicked; _victoryMenuButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_retryButton != null) { _retryButton.clicked += OnRetryClicked; _retryButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_defeatMenuButton != null) { _defeatMenuButton.clicked += OnQuitMenuClicked; _defeatMenuButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_viewCreditsButton != null) { _viewCreditsButton.clicked += ShowCreditsScreen; _viewCreditsButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_gameCompleteMenuButton != null) { _gameCompleteMenuButton.clicked += OnQuitMenuClicked; _gameCompleteMenuButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_githubButton != null) { _githubButton.clicked += OnGithubClicked; _githubButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_closeCreditsButton != null) { _closeCreditsButton.clicked += OnQuitMenuClicked; _closeCreditsButton.RegisterCallback<PointerEnterEvent>(OnButtonHover); }

            _ability1 = root.Q<Button>("Ability1");
            _ability2 = root.Q<Button>("Ability2");
            _ability3 = root.Q<Button>("Ability3");
            _ability4 = root.Q<Button>("Ability4");

            if (_ability1 != null) { _ability1.clicked += () => TryUnlockAbility(_ability1, 100); _ability1.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_ability2 != null) { _ability2.clicked += () => TryUnlockAbility(_ability2, 200); _ability2.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_ability3 != null) { _ability3.clicked += () => TryUnlockAbility(_ability3, 300); _ability3.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_ability4 != null) { _ability4.clicked += () => TryUnlockAbility(_ability4, 400); _ability4.RegisterCallback<PointerEnterEvent>(OnButtonHover); }

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


            _economyManager = Object.FindFirstObjectByType<EconomyManager>();
            if (_economyManager != null)
            {
                _economyManager.MoneyChanged += UpdateCurrency;
                UpdateCurrency(_economyManager.Money);
            }
            else
            {
                UpdateCurrency(0);
            }

            _towerShop = Object.FindFirstObjectByType<TowerShop>();
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
            if (_buyTowerButton != null)
            {
                _buyTowerButton.clicked -= OnBuyTowerClicked;
                _buyTowerButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_backButton != null)
            {
                _backButton.clicked -= OnBackClicked;
                _backButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_resumeButton != null)
            {
                _resumeButton.clicked -= OnResumeClicked;
                _resumeButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_pauseOptionsButton != null)
            {
                _pauseOptionsButton.clicked -= OnPauseOptionsClicked;
                _pauseOptionsButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_quitMenuButton != null)
            {
                _quitMenuButton.clicked -= OnQuitMenuClicked;
                _quitMenuButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_nextLevelButton != null) { _nextLevelButton.clicked -= OnNextLevelClicked; _nextLevelButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_victoryMenuButton != null) { _victoryMenuButton.clicked -= OnQuitMenuClicked; _victoryMenuButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_retryButton != null) { _retryButton.clicked -= OnRetryClicked; _retryButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_defeatMenuButton != null) { _defeatMenuButton.clicked -= OnQuitMenuClicked; _defeatMenuButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_viewCreditsButton != null) { _viewCreditsButton.clicked -= ShowCreditsScreen; _viewCreditsButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_gameCompleteMenuButton != null) { _gameCompleteMenuButton.clicked -= OnQuitMenuClicked; _gameCompleteMenuButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_githubButton != null) { _githubButton.clicked -= OnGithubClicked; _githubButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_closeCreditsButton != null) { _closeCreditsButton.clicked -= OnQuitMenuClicked; _closeCreditsButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }

            if (_economyManager != null)
            {
                _economyManager.MoneyChanged -= UpdateCurrency;
            }
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    HandleEscapePressed();
                }

                if (UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame)
                {
                    if (_victoryOverlay != null && _victoryOverlay.style.display == DisplayStyle.Flex) { _victoryOverlay.style.display = DisplayStyle.None; Time.timeScale = 1f; }
                    else ShowVictoryScreen(false);
                }
                if (UnityEngine.InputSystem.Keyboard.current.dKey.wasPressedThisFrame)
                {
                    if (_defeatOverlay != null && _defeatOverlay.style.display == DisplayStyle.Flex) { _defeatOverlay.style.display = DisplayStyle.None; Time.timeScale = 1f; }
                    else ShowDefeatScreen();
                }
                if (UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
                {
                    if (_creditsOverlay != null && _creditsOverlay.style.display == DisplayStyle.Flex) { _creditsOverlay.style.display = DisplayStyle.None; }
                    else ShowCreditsScreen();
                }
                if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
                {
                    if (_gameCompleteOverlay != null && _gameCompleteOverlay.style.display == DisplayStyle.Flex) { _gameCompleteOverlay.style.display = DisplayStyle.None; Time.timeScale = 1f; }
                    else ShowVictoryScreen(true);
                }

                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    UpdateWave(UnityEngine.Random.Range(2, 10));
                    SetWaveActive(true);

                    if (_bigAnnouncerContainer != null)
                    {
                        _bigAnnouncerContainer.schedule.Execute(() =>
                        {
                            SetWaveActive(false);
                        }).StartingIn(5000);
                    }
                }
            }
        }
        #endregion

        #region Public API
        public void UpdateCurrency(int amount)
        {
            _currentCurrency = amount;
            if (_currencyText != null)
            {
                _currencyText.text = _currentCurrency.ToString();
            }
        }

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (_healthText != null)
            {
                _healthText.text = $"{currentHealth}/{maxHealth}";
            }
            if (_healthBarFill != null)
            {
                float percentage = Mathf.Clamp01((float)currentHealth / maxHealth);
                _healthBarFill.style.width = Length.Percent(percentage * 100f);

                if (percentage > 0.6f)
                {
                    _healthBarFill.style.backgroundColor = new StyleColor(new Color32(46, 204, 113, 255));
                }
                else if (percentage > 0.3f)
                {
                    _healthBarFill.style.backgroundColor = new StyleColor(new Color32(241, 196, 15, 255));
                }
                else
                {
                    _healthBarFill.style.backgroundColor = new StyleColor(new Color32(231, 76, 60, 255));
                }
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

        public void ShowVictoryScreen(bool isFinalLevel = false)
        {
            Time.timeScale = 0f;
            if (isFinalLevel)
            {
                if (_gameCompleteOverlay != null) _gameCompleteOverlay.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (_victoryOverlay != null) _victoryOverlay.style.display = DisplayStyle.Flex;
            }
        }

        public void ShowDefeatScreen()
        {
            Time.timeScale = 0f;
            if (_defeatOverlay != null) _defeatOverlay.style.display = DisplayStyle.Flex;
        }

        public void ShowCreditsScreen()
        {
            if (_gameCompleteOverlay != null) _gameCompleteOverlay.style.display = DisplayStyle.None;
            if (_creditsOverlay != null) _creditsOverlay.style.display = DisplayStyle.Flex;
        }
        #endregion

        #region Input & Core Logic
        private void HandleEscapePressed()
        {
            if (_optionsOverlay != null && _optionsOverlay.style.display == DisplayStyle.Flex)
            {
                OnBackClicked();
            }
            else
            {
                TogglePause(!_isPaused);
            }
        }

        private void TogglePause(bool pause)
        {
            _isPaused = pause;
            Time.timeScale = _isPaused ? 0f : 1f;

            if (_pauseOverlay != null)
            {
                _pauseOverlay.style.display = _isPaused ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void TryUnlockAbility(Button abilityButton, int cost)
        {
            if (!abilityButton.ClassListContains("locked"))
            {
                Debug.Log($"Activando habilidad: {abilityButton.name}");
                return;
            }

            if (_economyManager != null && _economyManager.TrySpend(cost))
            {
                PlayClickSound();
                abilityButton.RemoveFromClassList("locked");
                Debug.Log($"Habilidad {abilityButton.name} desbloqueada.");
            }
            else
            {
                Debug.LogWarning($"Oro insuficiente para desbloquear {abilityButton.name} ({cost} necesarios).");
            }
        }
        #endregion

        #region UI Callbacks
        private void OnStartWaveClicked()
        {
            PlayClickSound();
            Debug.Log("Iniciando Oleada por click...");
            SetWaveActive(true);
        }

        private void OnBuyTowerClicked()
        {
            PlayClickSound();
            if (_towerShop != null)
            {
                _towerShop.TryBuyTower();
            }
            else
            {
                Debug.LogWarning("TowerShop no encontrado en la escena. La torre no se comprará.");
            }
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
            _optionsFromPause = false;
            if (_optionsOverlay != null) _optionsOverlay.style.display = DisplayStyle.Flex;
        }

        private void OnBackClicked()
        {
            PlayClickSound();
            if (_optionsOverlay != null) _optionsOverlay.style.display = DisplayStyle.None;

            if (_optionsFromPause)
            {
                if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.Flex;
                _optionsFromPause = false;
            }
        }

        private void OnResumeClicked()
        {
            PlayClickSound();
            TogglePause(false);
        }

        private void OnPauseOptionsClicked()
        {
            PlayClickSound();
            _optionsFromPause = true;
            if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.None;
            if (_optionsOverlay != null) _optionsOverlay.style.display = DisplayStyle.Flex;
        }

        private void OnQuitMenuClicked()
        {
            PlayClickSound();
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
        }

        private void OnNextLevelClicked()
        {
            PlayClickSound();
            Time.timeScale = 1f;
            Debug.Log("Cargando el siguiente nivel...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnRetryClicked()
        {
            PlayClickSound();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnGithubClicked()
        {
            PlayClickSound();
            Application.OpenURL("https://github.com/CamiBlackFire-Dev/TowerDefense-FinalProyect");
        }
        #endregion
    }
}
