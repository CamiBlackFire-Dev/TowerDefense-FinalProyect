using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Custom.UI
{
    public enum PowerUpUsageLimitMode
    {
        Unlimited,
        PerMatch,
        PerWave
    }

    // Configuracion de un powerup (cooldown + limite de usos opcional).
    // Se edita entera desde el Inspector: para otro powerup, o para cambiar
    // como se recarga este, solo hay que tocar estos valores, no el codigo.
    [System.Serializable]
    public class PowerUpCooldownConfig
    {
        [Tooltip("Segundos de cooldown despues de usarse. 0 = sin cooldown.")]
        public float cooldownSeconds = 5f;

        [Tooltip("Unlimited: sin tope. PerMatch: un total fijo para toda la partida. PerWave: se recarga al empezar cada oleada.")]
        public PowerUpUsageLimitMode usageLimitMode = PowerUpUsageLimitMode.Unlimited;

        [Tooltip("Usos disponibles cuando el modo no es Unlimited (por ejemplo 3 usos por partida).")]
        public int maxUses = 3;
    }

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

        [Header("Referencias")]
        // Las cuatro se buscan solas en la escena si se dejan vacias.
        public EnemySpawner spawner;   // quien lanza las oleadas
        public EconomyManager economy; // dinero que se muestra arriba
        public PlayerBase playerBase;  // vidas del jugador (barra de vida y derrota)
        public TowerShop towerShop;    // compra de torres desde el boton del HUD
        public BombDragAbility bombAbility; // arrastre de la bomba desde la ranura 1

        // True mientras se mantiene presionada la ranura 1 (ya desbloqueada).
        private bool _ability1Dragging;

        [Header("Cooldown y usos de habilidades")]
        // Cada ranura tiene su propio cooldown y, si se quiere, un limite de
        // usos (fijo por partida o recargable cada oleada). Todo editable
        // aca: para cambiar el powerup de una ranura o su forma de recarga
        // no hace falta tocar codigo.
        public PowerUpCooldownConfig ability1Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 6f };
        public PowerUpCooldownConfig ability2Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 5f };
        public PowerUpCooldownConfig ability3Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 5f };
        public PowerUpCooldownConfig ability4Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 5f };
        // Color del "reloj" que cubre el icono mientras esta en cooldown.
        public Color cooldownWipeColor = new Color(0f, 0f, 0f, 0.65f);

        // Estado en vivo de cada ranura (boton + overlay + cooldown/usos
        // restantes). Se arma una vez en OnEnable a partir de los campos de
        // arriba y de los elementos del UXML.
        private AbilitySlot _slotAbility1;
        private AbilitySlot _slotAbility2;
        private AbilitySlot _slotAbility3;
        private AbilitySlot _slotAbility4;

        private class AbilitySlot
        {
            public Button Button;
            public VisualElement CooldownOverlay;
            public Label UsesLabel;
            public PowerUpCooldownConfig Config;
            public float RemainingCooldown;
            public int UsesRemaining;
            public Action<MeshGenerationContext> DrawCallback;
        }

        [Header("Audio")]
        public AudioClip hoverSound;
        public AudioClip clickSound;
        public AudioClip waveAnnouncerSound;

        [Header("Debug")]
        // Teclas de prueba V, C, F y Espacio. Escape (pausa) nunca se desactiva.
        // La derrota no tiene tecla de prueba: solo la dispara PlayerBase.Defeated
        // al llegar a 0 vidas (ademas D ya se usa para mover torres a la derecha).
        public bool debugKeys = true;

        private int _currentCurrency = 0;
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

            _ability1 = root.Q<Button>("Ability1");
            _ability2 = root.Q<Button>("Ability2");
            _ability3 = root.Q<Button>("Ability3");
            _ability4 = root.Q<Button>("Ability4");

            _slotAbility1 = BuildAbilitySlot(_ability1, root.Q<VisualElement>("Ability1CooldownOverlay"), root.Q<Label>("Ability1UsesLabel"), ability1Cooldown);
            _slotAbility2 = BuildAbilitySlot(_ability2, root.Q<VisualElement>("Ability2CooldownOverlay"), root.Q<Label>("Ability2UsesLabel"), ability2Cooldown);
            _slotAbility3 = BuildAbilitySlot(_ability3, root.Q<VisualElement>("Ability3CooldownOverlay"), root.Q<Label>("Ability3UsesLabel"), ability3Cooldown);
            _slotAbility4 = BuildAbilitySlot(_ability4, root.Q<VisualElement>("Ability4CooldownOverlay"), root.Q<Label>("Ability4UsesLabel"), ability4Cooldown);

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

            if (_ability1 != null)
            {
                // El desbloqueo usa el mismo clicked probado que las otras
                // ranuras. El arrastre se registra en fase de captura
                // (TrickleDown): el Clickable interno del Button ya consume
                // el PointerDown en fase de burbuja para armar su propio
                // clicked, asi que si nos registramos igual que el resto
                // (burbuja) nuestro callback nunca llega a verlo.
                _ability1.clicked += OnAbility1Clicked;
                _ability1.RegisterCallback<PointerDownEvent>(OnAbility1PointerDown, TrickleDown.TrickleDown);
                _ability1.RegisterCallback<PointerUpEvent>(OnAbility1PointerUp, TrickleDown.TrickleDown);
                _ability1.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_ability2 != null) { _ability2.clicked += OnAbility2Clicked; _ability2.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_ability3 != null) { _ability3.clicked += OnAbility3Clicked; _ability3.RegisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_ability4 != null) { _ability4.clicked += OnAbility4Clicked; _ability4.RegisterCallback<PointerEnterEvent>(OnButtonHover); }

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
            if (playerBase == null)
                playerBase = PlayerBase.Instance;
            if (towerShop == null)
                towerShop = FindFirstObjectByType<TowerShop>();
            if (bombAbility == null)
                bombAbility = FindFirstObjectByType<BombDragAbility>();

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
            else
            {
                UpdateCurrency(0);
            }

            // Las vidas alimentan la barra de vida y la pantalla de derrota.
            if (playerBase != null)
            {
                playerBase.LivesChanged += OnLivesChanged;
                playerBase.Defeated += OnPlayerDefeated;
                UpdateHealth(playerBase.Lives, playerBase.maxLives);
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

            if (_ability1 != null)
            {
                _ability1.clicked -= OnAbility1Clicked;
                _ability1.UnregisterCallback<PointerDownEvent>(OnAbility1PointerDown, TrickleDown.TrickleDown);
                _ability1.UnregisterCallback<PointerUpEvent>(OnAbility1PointerUp, TrickleDown.TrickleDown);
                _ability1.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_ability2 != null) { _ability2.clicked -= OnAbility2Clicked; _ability2.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_ability3 != null) { _ability3.clicked -= OnAbility3Clicked; _ability3.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }
            if (_ability4 != null) { _ability4.clicked -= OnAbility4Clicked; _ability4.UnregisterCallback<PointerEnterEvent>(OnButtonHover); }

            UnregisterAbilitySlot(_slotAbility1);
            UnregisterAbilitySlot(_slotAbility2);
            UnregisterAbilitySlot(_slotAbility3);
            UnregisterAbilitySlot(_slotAbility4);

            if (spawner != null)
            {
                spawner.WaveStarted -= OnWaveStarted;
                spawner.WaveFinished -= OnWaveFinished;
            }

            if (economy != null)
                economy.MoneyChanged -= OnMoneyChanged;

            if (playerBase != null)
            {
                playerBase.LivesChanged -= OnLivesChanged;
                playerBase.Defeated -= OnPlayerDefeated;
            }
        }

        private void Update()
        {
            // Mientras se sostiene la ranura de la bomba, la posicion del
            // mouse/dedo se sigue leyendo aunque no llegue un evento nuevo
            // (Pointer.current sirve igual para mouse que para touch).
            if (_ability1Dragging && bombAbility != null)
            {
                var pointer = UnityEngine.InputSystem.Pointer.current;
                if (pointer != null)
                    bombAbility.UpdateDrag(pointer.position.ReadValue());
            }

            TickAbilityCooldowns();

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapePressed();
            }

            if (!debugKeys) return;

            if (keyboard.vKey.wasPressedThisFrame)
            {
                if (_victoryOverlay != null && _victoryOverlay.style.display == DisplayStyle.Flex) { _victoryOverlay.style.display = DisplayStyle.None; Time.timeScale = 1f; }
                else ShowVictoryScreen(false);
            }
            if (keyboard.cKey.wasPressedThisFrame)
            {
                if (_creditsOverlay != null && _creditsOverlay.style.display == DisplayStyle.Flex) { _creditsOverlay.style.display = DisplayStyle.None; }
                else ShowCreditsScreen();
            }
            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (_gameCompleteOverlay != null && _gameCompleteOverlay.style.display == DisplayStyle.Flex) { _gameCompleteOverlay.style.display = DisplayStyle.None; Time.timeScale = 1f; }
                else ShowVictoryScreen(true);
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                // Con spawner en escena arranca la oleada de verdad;
                // sin el (escena de pruebas de UI) solo se muestra el cartel.
                if (spawner != null)
                {
                    OnStartWaveClicked();
                }
                else
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
                float percentage = Mathf.Clamp01((float)currentHealth / Mathf.Max(1, maxHealth));
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
            if (abilityButton == null) return;

            if (!abilityButton.ClassListContains("locked"))
            {
                Debug.Log($"Activando habilidad: {abilityButton.name}");
                return;
            }

            if (economy != null && economy.TrySpend(cost))
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

        // Para las ranuras sin arrastre (2, 3 y 4): si esta bloqueada intenta
        // desbloquearla como siempre; si ya esta desbloqueada, el click es el
        // "uso" y pasa por el cooldown/limite de usos de la ranura.
        private void TryUnlockOrUseAbility(AbilitySlot slot, int cost)
        {
            if (slot == null || slot.Button == null) return;

            if (slot.Button.ClassListContains("locked"))
            {
                TryUnlockAbility(slot.Button, cost);
                return;
            }

            TryUseAbility(slot);
        }

        // Chequeo de solo lectura: cooldown en 0 y, si el modo no es
        // Unlimited, todavia quedan usos. No consume nada por si solo.
        private bool CanUseAbility(AbilitySlot slot)
        {
            if (slot == null || slot.Config == null)
                return false;

            if (slot.RemainingCooldown > 0f)
            {
                Debug.Log($"{slot.Button.name} en cooldown ({slot.RemainingCooldown:0.0}s restantes).");
                return false;
            }

            if (slot.Config.usageLimitMode != PowerUpUsageLimitMode.Unlimited && slot.UsesRemaining <= 0)
            {
                Debug.Log($"{slot.Button.name} sin usos restantes.");
                return false;
            }

            return true;
        }

        // Arranca el cooldown y descuenta un uso (si aplica). Se llama solo
        // cuando la habilidad realmente se uso (por ejemplo la bomba ya cayo
        // en un punto valido), no solo por intentarlo.
        private void ConsumeAbility(AbilitySlot slot)
        {
            if (slot == null || slot.Config == null) return;

            slot.RemainingCooldown = slot.Config.cooldownSeconds;
            if (slot.Config.usageLimitMode != PowerUpUsageLimitMode.Unlimited)
                slot.UsesRemaining = Mathf.Max(0, slot.UsesRemaining - 1);

            RefreshSlotVisual(slot);
        }

        // Version "todo en uno" para ranuras sin arrastre: chequea, consume
        // y deja el log de activacion de siempre.
        private bool TryUseAbility(AbilitySlot slot)
        {
            if (!CanUseAbility(slot))
                return false;

            ConsumeAbility(slot);
            Debug.Log($"Activando habilidad: {slot.Button.name}");
            return true;
        }

        private void ResetPerWaveUses(AbilitySlot slot)
        {
            if (slot == null || slot.Config == null || slot.Config.usageLimitMode != PowerUpUsageLimitMode.PerWave)
                return;

            slot.UsesRemaining = slot.Config.maxUses;
            RefreshSlotVisual(slot);
        }

        // Arma el estado en vivo de una ranura a partir de su boton y de los
        // elementos de overlay/label que ya existen en el UXML, y engancha
        // el dibujo del "reloj" de cooldown sobre el icono.
        private AbilitySlot BuildAbilitySlot(Button button, VisualElement cooldownOverlay, Label usesLabel, PowerUpCooldownConfig config)
        {
            var slot = new AbilitySlot
            {
                Button = button,
                CooldownOverlay = cooldownOverlay,
                UsesLabel = usesLabel,
                Config = config,
                RemainingCooldown = 0f,
                UsesRemaining = config != null && config.usageLimitMode != PowerUpUsageLimitMode.Unlimited ? config.maxUses : -1
            };

            if (cooldownOverlay != null)
            {
                slot.DrawCallback = context => DrawCooldownWipe(context, slot);
                cooldownOverlay.generateVisualContent += slot.DrawCallback;
            }

            RefreshSlotVisual(slot);
            return slot;
        }

        private void UnregisterAbilitySlot(AbilitySlot slot)
        {
            if (slot == null || slot.CooldownOverlay == null || slot.DrawCallback == null)
                return;

            slot.CooldownOverlay.generateVisualContent -= slot.DrawCallback;
        }

        // Recorre las 4 ranuras cada frame y solo redibuja las que siguen en
        // cooldown (las que ya estan listas no cuestan nada).
        private void TickAbilityCooldowns()
        {
            TickSlot(_slotAbility1);
            TickSlot(_slotAbility2);
            TickSlot(_slotAbility3);
            TickSlot(_slotAbility4);
        }

        private void TickSlot(AbilitySlot slot)
        {
            if (slot == null || slot.RemainingCooldown <= 0f)
                return;

            slot.RemainingCooldown = Mathf.Max(0f, slot.RemainingCooldown - Time.deltaTime);
            RefreshSlotVisual(slot);
        }

        // Actualiza el numero de usos restantes (si el modo lo usa) y pide
        // que se redibuje el overlay de cooldown de la ranura.
        private void RefreshSlotVisual(AbilitySlot slot)
        {
            if (slot == null) return;

            if (slot.UsesLabel != null && slot.Config != null)
            {
                bool limited = slot.Config.usageLimitMode != PowerUpUsageLimitMode.Unlimited;
                slot.UsesLabel.style.display = limited ? DisplayStyle.Flex : DisplayStyle.None;
                if (limited)
                    slot.UsesLabel.text = slot.UsesRemaining.ToString();
            }

            if (slot.CooldownOverlay != null)
                slot.CooldownOverlay.MarkDirtyRepaint();
        }

        // Dibuja el "reloj" de cooldown: una cuna que cubre 360 grados justo
        // al usarse y se va cerrando en sentido horario hasta desaparecer
        // cuando la habilidad vuelve a estar lista. Si no quedan usos (modo
        // limitado) se queda cubierta del todo hasta la siguiente recarga.
        private void DrawCooldownWipe(MeshGenerationContext context, AbilitySlot slot)
        {
            if (slot.Config == null) return;

            Rect rect = slot.CooldownOverlay.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            bool outOfUses = slot.Config.usageLimitMode != PowerUpUsageLimitMode.Unlimited && slot.UsesRemaining <= 0;
            float fraction = slot.Config.cooldownSeconds > 0f
                ? Mathf.Clamp01(slot.RemainingCooldown / slot.Config.cooldownSeconds)
                : 0f;

            if (!outOfUses && fraction <= 0f)
                return;

            float sweep = outOfUses ? 360f : fraction * 360f;
            Vector2 center = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            // Radio de sobra para que la cuna tape hasta las esquinas del
            // boton cuadrado; el propio boton recorta lo que sobre (overflow: hidden).
            float radius = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height) * 0.5f + 4f;

            Painter2D painter = context.painter2D;
            painter.fillColor = cooldownWipeColor;
            painter.BeginPath();
            painter.MoveTo(center);
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + sweep), ArcDirection.Clockwise);
            painter.LineTo(center);
            painter.Fill();
        }
        #endregion

        #region UI Callbacks
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
            ResetPerWaveUses(_slotAbility1);
            ResetPerWaveUses(_slotAbility2);
            ResetPerWaveUses(_slotAbility3);
            ResetPerWaveUses(_slotAbility4);
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

        // Se escapo un enemigo: se refresca la barra de vida.
        private void OnLivesChanged(int lives)
        {
            UpdateHealth(lives, playerBase != null ? playerBase.maxLives : lives);
        }

        // Se acabaron las vidas: aparece la pantalla de derrota.
        private void OnPlayerDefeated()
        {
            ShowDefeatScreen();
        }

        private void OnBuyTowerClicked()
        {
            PlayClickSound();
            if (towerShop != null)
            {
                towerShop.TryBuyTower();
            }
            else
            {
                Debug.LogWarning("TowerShop no encontrado en la escena. La torre no se comprara.", this);
            }
        }

        // Ranura 1 (bomba): el desbloqueo es un click normal, igual que las
        // otras tres ranuras.
        private void OnAbility1Clicked() { TryUnlockAbility(_ability1, 100); }

        // Ya desbloqueada, sostener arrastra la bomba; soltar la deja caer
        // donde este el mouse/dedo en ese momento. Mientras siga bloqueada
        // no hace nada aqui: el desbloqueo lo maneja OnAbility1Clicked.
        private void OnAbility1PointerDown(PointerDownEvent evt)
        {
            if (_ability1.ClassListContains("locked"))
                return;

            if (bombAbility == null)
            {
                Debug.LogWarning("GameHUD: falta BombDragAbility en la escena, la ranura 1 no puede arrastrar nada.", this);
                return;
            }

            // Si esta en cooldown o sin usos ni siquiera se deja empezar a
            // arrastrar (CanUseAbility solo mira, no consume nada).
            if (!CanUseAbility(_slotAbility1))
                return;

            PlayClickSound();
            _ability1Dragging = true;
            _ability1.CapturePointer(evt.pointerId);
            bombAbility.BeginDrag();
        }

        private void OnAbility1PointerUp(PointerUpEvent evt)
        {
            if (!_ability1Dragging)
                return;

            _ability1Dragging = false;
            if (_ability1.HasPointerCapture(evt.pointerId))
                _ability1.ReleasePointer(evt.pointerId);

            GameObject dropped = bombAbility.EndDrag();
            // Solo se consume cooldown/uso si la bomba de verdad cayo en un
            // punto valido (soltar fuera del tablero no cuenta como uso).
            if (dropped != null)
                ConsumeAbility(_slotAbility1);
        }

        private void OnAbility2Clicked() { TryUnlockOrUseAbility(_slotAbility2, 200); }
        private void OnAbility3Clicked() { TryUnlockOrUseAbility(_slotAbility3, 300); }
        private void OnAbility4Clicked() { TryUnlockOrUseAbility(_slotAbility4, 400); }

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
