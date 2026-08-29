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
        private Label _waveSubtitleText;
        private Button _optionsButton;
        private Button _lockCameraButton;
        private Button _startWaveButton;
        private Button _buyTowerButton;
        private Button _nextBoardButton;
        // Candado dibujado a mano (Painter2D): los emojis (🔒⚡💨🔧🔓) se ven
        // en el Editor porque Windows les busca una fuente de respaldo, pero
        // en un build (WebGL incluido) no hay ese respaldo y no se dibuja
        // nada. Dibujar el icono con vectores evita depender de una fuente.
        private Label _buyTowerPriceText;
        private VisualElement _buyTowerLockIcon;
        private Action<MeshGenerationContext> _buyTowerLockIconDrawCallback;
        private Button _speed1xButton;
        private Button _speed2xButton;

        private Button _ability1;
        private Button _ability2;
        private Button _ability3;
        private Button _ability4;
        private Button _ability5;

        private VisualElement _bigAnnouncerContainer;
        private Label _bigAnnouncerText;
        private VisualElement _bossWarning;
        private VisualElement _dangerVignette;
        private IVisualElementScheduledItem _dangerPulse;

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
        // Powerups de arrastrar y soltar. Todos cumplen IDragAbility, asi
        // que el HUD los maneja igual; lo unico que cambia por ranura es
        // cual va en cual (ver BuildAllAbilitySlots).
        public BombDragAbility bombAbility;           // ranura 1
        public EMPDragAbility empAbility;             // ranura 2 (ralentiza enemigos)
        public RepulsionDragAbility repulsionAbility; // ranura 3 (empuja enemigos hacia atras)
        public RepairDragAbility repairAbility;       // ranura 4 (cura torres)
        public BoardManager boardManager;             // ranura 5 (desbloqueo temporal del tablero)
        // Lista de niveles del juego. Se usa para saber que nivel es este
        // (y asi abrir el siguiente al ganar) y a donde va el boton
        // "siguiente nivel". Sin el, ambas cosas se quedan quietas.
        public GameFlowConfig gameFlow;

        // Ranura que se esta arrastrando ahora mismo (null si ninguna).
        private AbilitySlot _draggingSlot;

        [Header("Ballesta")]
        public bool instantBallistaPlacement = false;
        public BallistaDragAbility ballistaAbility;
        public int ballistaCost = 100;
        public int arrowUnlockCost = 120;
        public int arrowSwitchCost = 25;

        // Config global de oro. Si esta puesta, los costos de la ballesta,
        // las flechas y los powerups salen de ahi en vez de los campos de
        // esta escena, para que no se desincronicen entre niveles.
        public GameEconomyConfig economyConfig;

        public int BallistaCost => economyConfig != null ? economyConfig.ballistaCost : ballistaCost;
        public int ArrowUnlockCost => economyConfig != null ? economyConfig.arrowUnlockCost : arrowUnlockCost;
        public int ArrowSwitchCost => economyConfig != null ? economyConfig.arrowSwitchCost : arrowSwitchCost;

        // Costo del powerup de la ranura pedida (1 a 5), resuelto contra la
        // config global si la hay.
        public int AbilityCost(int slot)
        {
            if (economyConfig != null)
            {
                switch (slot)
                {
                    case 1: return economyConfig.ability1Cost;
                    case 2: return economyConfig.ability2Cost;
                    case 3: return economyConfig.ability3Cost;
                    case 4: return economyConfig.ability4Cost;
                    case 5: return economyConfig.ability5Cost;
                }
            }

            switch (slot)
            {
                case 1: return ability1Cost;
                case 2: return ability2Cost;
                case 3: return ability3Cost;
                case 4: return ability4Cost;
                default: return ability5Cost;
            }
        }
        public ArrowData arrowNormalData;
        public ArrowData arrowFireData;
        public ArrowData arrowIceData;
        public ArrowData arrowPoisonData;

        private int _ballistaCount = 0;
        private bool _fireUnlocked = false;
        private bool _iceUnlocked = false;
        private bool _poisonUnlocked = false;
        private ArrowData _currentGlobalArrow;

        private Button _ballistaBuyBtn;
        private Label _ballistaCountLabel;
        private Label _ballistaPriceText;
        private Button _ballistaUpgradeBtn;
        private VisualElement _ballistaUpgradePopup;
        private Button _arrowNormalBtn;
        private Button _arrowFireBtn;
        private Button _arrowIceBtn;
        private Button _arrowPoisonBtn;
        private VisualElement _firePriceContainer;
        private VisualElement _icePriceContainer;
        private VisualElement _poisonPriceContainer;
        private Label _firePriceText;
        private Label _icePriceText;
        private Label _poisonPriceText;
        private bool _isBallistaDragging = false;

        [Header("Cooldown y usos de habilidades")]
        // Cada ranura tiene su propio cooldown y, si se quiere, un limite de
        // usos (fijo por partida o recargable cada oleada). Todo editable
        // aca: para cambiar el powerup de una ranura o su forma de recarga
        // no hace falta tocar codigo.
        public PowerUpCooldownConfig ability1Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 6f };
        public PowerUpCooldownConfig ability2Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 8f };
        public PowerUpCooldownConfig ability3Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 10f };
        public PowerUpCooldownConfig ability4Cooldown = new PowerUpCooldownConfig { cooldownSeconds = 12f };
        // Ranura 5 (desbloqueo del tablero): una vez por oleada, con un
        // cooldown igual a la duracion del desbloqueo para que no se pueda
        // reactivar antes de que termine el anterior.
        public PowerUpCooldownConfig ability5Cooldown = new PowerUpCooldownConfig
        {
            cooldownSeconds = 30f,
            usageLimitMode = PowerUpUsageLimitMode.PerWave,
            maxUses = 1
        };
        // Color del "reloj" que cubre el icono mientras esta en cooldown.
        public Color cooldownWipeColor = new Color(0f, 0f, 0f, 0.65f);

        [Header("Costo de desbloqueo")]
        // Oro que cuesta destrabar cada ranura la primera vez. Se lee solo
        // al armar los slots (OnEnable), asi que un cambio en Play Mode no
        // se nota hasta la proxima vez que se abra la escena.
        public int ability1Cost = 100;
        public int ability2Cost = 150;
        public int ability3Cost = 150;
        public int ability4Cost = 150;
        public int ability5Cost = 150;

        // Estado en vivo de cada ranura (boton + overlay + cooldown/usos
        // restantes). Se arma una vez en OnEnable a partir de los campos de
        // arriba y de los elementos del UXML.
        private AbilitySlot _slotAbility1;
        private AbilitySlot _slotAbility2;
        private AbilitySlot _slotAbility3;
        private AbilitySlot _slotAbility4;
        private AbilitySlot _slotAbility5;
        // Las cinco juntas, para recorrerlas sin repetir codigo.
        private AbilitySlot[] _allSlots = new AbilitySlot[0];

        private class AbilitySlot
        {
            public Button Button;
            public VisualElement CooldownOverlay;
            public Label UsesLabel;
            public PowerUpCooldownConfig Config;
            public float RemainingCooldown;
            public int UsesRemaining;
            public Action<MeshGenerationContext> DrawCallback;
            public VisualElement IconElement;
            public Action<MeshGenerationContext> IconDrawCallback;
            // Cuanto cuesta desbloquearla la primera vez.
            public int Cost;
            // Sonido al desbloquearla (distinto del click generico).
            public AudioClip UnlockSound;
            // Powerup de arrastre de esta ranura (null si se usa con click).
            public IDragAbility DragAbility;
            // Que hace al usarse, para las ranuras de click.
            public Action OnUse;
            // Se guardan para poder quitarlos en OnDisable.
            public EventCallback<PointerDownEvent> PointerDownCallback;
            public EventCallback<PointerUpEvent> PointerUpCallback;
            public Action ClickedCallback;
        }

        [Header("Audio")]
        public AudioClip hoverSound;
        public AudioClip clickSound;
        public AudioClip waveAnnouncerSound;
        public AudioClip defeatSound;
        // Al usar el desbloqueo temporal del tablero (ranura 5): el efecto
        // en si (empujar el tablero) no tiene sonido propio, esto es lo
        // unico que se oye al activarlo.
        public AudioClip boardUnlockSound;
        // Al intentar desbloquear una ranura sin oro suficiente. El mismo
        // clip que TowerShop usa para "no se pudo comprar torre", pero
        // asignado aca aparte: no hay un registro central de sonidos, cada
        // script que necesita uno tiene su propio campo (igual que ya hacen
        // hoverSound/clickSound).
        public AudioClip abilityUnlockFailedSound;
        // Sonido al desbloquear cada ranura, en el orden 1..5 del HUD.
        public AudioClip ability1UnlockSound;
        public AudioClip ability2UnlockSound;
        public AudioClip ability3UnlockSound;
        public AudioClip ability4UnlockSound;
        public AudioClip ability5UnlockSound;



        private int _currentCurrency = 0;
        #endregion

        private VisualElement _fadeOverlay;

        private bool _canChangeBoard = false;
        private CameraEdgePan _cameraPan;

        #region Unity Lifecycle
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;
            if (arrowNormalData == null) arrowNormalData = Resources.Load<ArrowData>("Arrows/NormalArrow");
            if (arrowFireData == null) arrowFireData = Resources.Load<ArrowData>("Arrows/FireArrow");
            if (arrowIceData == null) arrowIceData = Resources.Load<ArrowData>("Arrows/IceArrow");
            if (arrowPoisonData == null) arrowPoisonData = Resources.Load<ArrowData>("Arrows/PoisonArrow");

#if UNITY_EDITOR
            if (arrowNormalData == null) arrowNormalData = UnityEditor.AssetDatabase.LoadAssetAtPath<ArrowData>("Assets/JeanAssets/ScriptableObjects/Arrow_Normal.asset");
            if (arrowFireData == null) arrowFireData = UnityEditor.AssetDatabase.LoadAssetAtPath<ArrowData>("Assets/JeanAssets/ScriptableObjects/Arrow_Fire.asset");
            if (arrowIceData == null) arrowIceData = UnityEditor.AssetDatabase.LoadAssetAtPath<ArrowData>("Assets/JeanAssets/ScriptableObjects/Arrow_Ice.asset");
            if (arrowPoisonData == null) arrowPoisonData = UnityEditor.AssetDatabase.LoadAssetAtPath<ArrowData>("Assets/JeanAssets/ScriptableObjects/Arrow_Poison.asset");
#endif
            if (root == null) return;

            _fadeOverlay = new VisualElement();
            _fadeOverlay.style.position = Position.Absolute;
            _fadeOverlay.style.left = 0;
            _fadeOverlay.style.right = 0;
            _fadeOverlay.style.top = 0;
            _fadeOverlay.style.bottom = 0;
            _fadeOverlay.style.backgroundColor = Color.black;
            _fadeOverlay.style.opacity = 1f;
            _fadeOverlay.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0.5f) };
            _fadeOverlay.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("opacity") };
            _fadeOverlay.pickingMode = PickingMode.Ignore;
            root.Add(_fadeOverlay);
            _fadeOverlay.schedule.Execute(() => _fadeOverlay.style.opacity = 0f).StartingIn(100);

            _currencyText = root.Q<Label>("CurrencyText");
            _healthText = root.Q<Label>("HealthText");
            _healthBarFill = root.Q<VisualElement>("HealthBarFill");
            _waveText = root.Q<Label>("WaveText");
            _waveSubtitleText = root.Q<Label>("WaveSubtitleText");
            _optionsButton = root.Q<Button>("OptionsButton");
            _lockCameraButton = root.Q<Button>("LockCameraButton");
            _startWaveButton = root.Q<Button>("StartWaveButton");
            _startWaveButton.BringToFront();
            _buyTowerButton = root.Q<Button>("BuyTowerButton");
            _buyTowerPriceText = root.Q<Label>("BuyTowerPriceText");
            _nextBoardButton = root.Q<Button>("NextBoardButton");
            _speed1xButton = root.Q<Button>("Speed1xButton");
            _speed2xButton = root.Q<Button>("Speed2xButton");

            if (_nextBoardButton != null)
            {
                BoardSelector selector = FindFirstObjectByType<BoardSelector>();
                _canChangeBoard = (selector != null && selector.BoardCount > 1);
                _nextBoardButton.style.display = _canChangeBoard ? DisplayStyle.Flex : DisplayStyle.None;

                _nextBoardButton.BringToFront();
            }

            _bigAnnouncerContainer = root.Q<VisualElement>("BigAnnouncerContainer");
            _bigAnnouncerText = root.Q<Label>("BigAnnouncerText");
            _bossWarning = root.Q<VisualElement>("BossWarning");
            _dangerVignette = root.Q<VisualElement>("DangerVignette");

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
            _ability5 = root.Q<Button>("Ability5");

            _ballistaBuyBtn = root.Q<Button>("BallistaBuyButton");
            _ballistaCountLabel = root.Q<Label>("BallistaCountLabel");
            _ballistaPriceText = root.Q<Label>("BallistaPriceText");
            _ballistaUpgradeBtn = root.Q<Button>("BallistaUpgradeButton");
            _ballistaUpgradePopup = root.Q<VisualElement>("BallistaUpgradePopup");
            _arrowNormalBtn = root.Q<Button>("ArrowNormalBtn");
            _arrowFireBtn = root.Q<Button>("ArrowFireBtn");
            _arrowIceBtn = root.Q<Button>("ArrowIceBtn");
            _arrowPoisonBtn = root.Q<Button>("ArrowPoisonBtn");
            _firePriceContainer = root.Q<VisualElement>("FirePriceContainer");
            _icePriceContainer = root.Q<VisualElement>("IcePriceContainer");
            _poisonPriceContainer = root.Q<VisualElement>("PoisonPriceContainer");
            _firePriceText = root.Q<Label>("FirePriceText");
            _icePriceText = root.Q<Label>("IcePriceText");
            _poisonPriceText = root.Q<Label>("PoisonPriceText");

            if (_ballistaBuyBtn != null)
            {
                _ballistaBuyBtn.RegisterCallback<PointerDownEvent>(OnBallistaPointerDown, TrickleDown.TrickleDown);
                _ballistaBuyBtn.RegisterCallback<PointerUpEvent>(OnBallistaPointerUp);
                _ballistaBuyBtn.clicked += OnBallistaBuyClicked;
                _ballistaBuyBtn.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_ballistaUpgradeBtn != null)
            {
                _ballistaUpgradeBtn.clicked += () =>
                {
                    PlayClickSound();
                    if (_ballistaUpgradePopup != null)
                    {
                        bool isVisible = _ballistaUpgradePopup.resolvedStyle.display == DisplayStyle.Flex;
                        _ballistaUpgradePopup.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
                    }
                };
                _ballistaUpgradeBtn.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_arrowNormalBtn != null) _arrowNormalBtn.clicked += () => OnArrowUpgradeClicked(ArrowEffectType.Normal);
            if (_arrowFireBtn != null) _arrowFireBtn.clicked += () => OnArrowUpgradeClicked(ArrowEffectType.Fire);
            if (_arrowIceBtn != null) _arrowIceBtn.clicked += () => OnArrowUpgradeClicked(ArrowEffectType.Ice);
            if (_arrowPoisonBtn != null) _arrowPoisonBtn.clicked += () => OnArrowUpgradeClicked(ArrowEffectType.Poison);

            UpdateBallistaUI();
            
            if (_buyTowerPriceText != null && towerShop != null)
            {
                _buyTowerPriceText.text = $"({towerShop.TowerCost} ORO)";
            }

            // Ocultar sección de ballestas si no hay spots en la escena
            var ballistaContainer = root.Q<VisualElement>("BallistaContainer");
            if (ballistaContainer != null)
            {
                var slots = FindObjectsByType<CrossbowSlot>(FindObjectsSortMode.None);
                if (slots.Length == 0)
                {
                    ballistaContainer.style.visibility = Visibility.Hidden;
                }
            }


            _cameraPan = FindFirstObjectByType<CameraEdgePan>();
            if (_optionsButton != null)
            {
                _optionsButton.clicked += OnOptionsClicked;
                _optionsButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_lockCameraButton != null)
            {
                _lockCameraButton.clicked += OnLockCameraClicked;
                _lockCameraButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
                RefreshLockCameraVisuals();
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

            if (_nextBoardButton != null)
            {
                _nextBoardButton.clicked += OnNextBoardClicked;
                _nextBoardButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_waveSubtitleText != null)
            {
                _waveSubtitleText.schedule.Execute(() => _waveSubtitleText.ToggleInClassList("breathe")).Every(1000);
            }

            _buyTowerLockIcon = root.Q<VisualElement>("BuyTowerLockIcon");
            if (_buyTowerLockIcon != null)
            {
                _buyTowerLockIconDrawCallback = DrawLockIcon;
                _buyTowerLockIcon.generateVisualContent += _buyTowerLockIconDrawCallback;
            }

            if (_speed1xButton != null)
            {
                _speed1xButton.clicked += OnSpeed1xClicked;
                _speed1xButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_speed2xButton != null)
            {
                _speed2xButton.clicked += OnSpeed2xClicked;
                _speed2xButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            RefreshSpeedButtons();

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

            if (ballistaAbility != null)
                ballistaAbility.OnBallistaPlaced -= HandleBallistaPlaced;

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
            if (empAbility == null)
                empAbility = FindFirstObjectByType<EMPDragAbility>();
            if (repulsionAbility == null)
                repulsionAbility = FindFirstObjectByType<RepulsionDragAbility>();
            if (repairAbility == null)
                repairAbility = FindFirstObjectByType<RepairDragAbility>();
            if (boardManager == null)
                boardManager = FindFirstObjectByType<BoardManager>();
            if (ballistaAbility == null)
            {
                ballistaAbility = FindFirstObjectByType<BallistaDragAbility>();
                if (ballistaAbility == null)
                {
                    GameObject ballistaObj = new GameObject("BallistaDragAbility");
                    ballistaAbility = ballistaObj.AddComponent<BallistaDragAbility>();
                }
                ballistaAbility.OnBallistaPlaced -= HandleBallistaPlaced;
                ballistaAbility.OnBallistaPlaced += HandleBallistaPlaced;
            }

            // Las ranuras se arman aca (y no antes) porque necesitan los
            // powerups ya resueltos.
            BuildAllAbilitySlots(root);

            // La oleada avisa al HUD cuando arranca y cuando termina.
            if (spawner != null)
            {
                spawner.WaveStarted += OnWaveStarted;
                spawner.WaveFinished += OnWaveFinished;
                spawner.BossSpawned += OnBossSpawned;
                SetWaveActive(spawner.IsRunning);
                SetFinalWaveDanger(spawner.IsRunning && IsFinalWave());
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

        private void HandleBallistaPlaced(CrossbowSlot slot)
        {
            if (_currentGlobalArrow != null)
            {
                slot.UnlockArrow(_currentGlobalArrow);
                slot.SelectArrow(_currentGlobalArrow);
            }
        }

        private void OnDisable()
        {
            if (_optionsButton != null)
            {
                _optionsButton.clicked -= OnOptionsClicked;
                _optionsButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_lockCameraButton != null)
            {
                _lockCameraButton.clicked -= OnLockCameraClicked;
                _lockCameraButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
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

            if (_nextBoardButton != null)
            {
                _nextBoardButton.clicked -= OnNextBoardClicked;
                _nextBoardButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_buyTowerLockIcon != null && _buyTowerLockIconDrawCallback != null)
                _buyTowerLockIcon.generateVisualContent -= _buyTowerLockIconDrawCallback;
            if (_speed1xButton != null)
            {
                _speed1xButton.clicked -= OnSpeed1xClicked;
                _speed1xButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_speed2xButton != null)
            {
                _speed2xButton.clicked -= OnSpeed2xClicked;
                _speed2xButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
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

            foreach (AbilitySlot slot in _allSlots)
                UnregisterAbilitySlot(slot);

            if (spawner != null)
            {
                spawner.WaveStarted -= OnWaveStarted;
                spawner.WaveFinished -= OnWaveFinished;
                spawner.BossSpawned -= OnBossSpawned;
            }

            SetFinalWaveDanger(false);

            if (economy != null)
                economy.MoneyChanged -= OnMoneyChanged;

            if (playerBase != null)
            {
                playerBase.LivesChanged -= OnLivesChanged;
                playerBase.Defeated -= OnPlayerDefeated;
            }

            if (ballistaAbility != null)
                ballistaAbility.OnBallistaPlaced -= HandleBallistaPlaced;
        }

        private void Update()
        {
            // Mientras se sostiene una ranura de arrastre, la posicion del
            // mouse/dedo se sigue leyendo aunque no llegue un evento nuevo
            // (Pointer.current sirve igual para mouse que para touch).
            if (_draggingSlot != null && _draggingSlot.DragAbility != null)
            {
                var pointer = UnityEngine.InputSystem.Pointer.current;
                if (pointer != null)
                    _draggingSlot.DragAbility.UpdateDrag(pointer.position.ReadValue());
            }
            if (_isBallistaDragging && ballistaAbility != null)
            {
                var pointer = UnityEngine.InputSystem.Pointer.current;
                if (pointer != null)
                    ballistaAbility.UpdateDrag(pointer.position.ReadValue());
            }

            TickAbilityCooldowns();

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapePressed();
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
            RefreshBuyTowerButtonState();
        }

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (_healthText != null)
            {
                _healthText.text = $"❤️ {currentHealth}/{maxHealth}";
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
            if (_nextBoardButton != null)
            {
                _nextBoardButton.style.display = (isActive || !_canChangeBoard) ? DisplayStyle.None : DisplayStyle.Flex;
            }
            if (_waveSubtitleText != null)
            {
                if (isActive)
                    _waveSubtitleText.AddToClassList("fade-out");
                else
                    _waveSubtitleText.RemoveFromClassList("fade-out");
            }
            RefreshBuyTowerButtonState();
        }

        private void RefreshBuyTowerButtonState()
        {
            if (_buyTowerButton == null) return;

            bool inWave = spawner != null && spawner.IsRunning;
            bool canAfford = towerShop != null && _currentCurrency >= towerShop.TowerCost;

            if (inWave || !canAfford)
                _buyTowerButton.AddToClassList("locked");
            else
                _buyTowerButton.RemoveFromClassList("locked");
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
            UnlockNextLevel();

            if (isFinalLevel)
            {
                if (_gameCompleteOverlay != null) _gameCompleteOverlay.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (_victoryOverlay != null) _victoryOverlay.style.display = DisplayStyle.Flex;
            }
        }

        // Superar un nivel abre el siguiente en el menu principal, que lee
        // el mismo MaxLevelUnlocked. Sin esto los botones de NIVEL 2 y 3 se
        // quedan con el candado para siempre: el tutorial solo llega a 1.
        // Se guarda el mayor alcanzado, asi rejugar un nivel viejo no
        // vuelve a cerrar los que ya estaban abiertos.
        private void UnlockNextLevel()
        {
            if (gameFlow == null)
                return;

            string scene = SceneManager.GetActiveScene().name;
            int levelNumber = gameFlow.GetLevelNumber(scene);
            if (levelNumber <= 0)
                return;   // esta escena no es un nivel de la lista

            int unlocked = Mathf.Min(levelNumber + 1, gameFlow.LevelCount);
            if (unlocked > PlayerPrefs.GetInt("MaxLevelUnlocked", 0))
            {
                PlayerPrefs.SetInt("MaxLevelUnlocked", unlocked);
                PlayerPrefs.Save();
            }
        }

        public void ShowDefeatScreen()
        {
            Time.timeScale = 0f;
            if (_defeatOverlay != null) _defeatOverlay.style.display = DisplayStyle.Flex;

            if (AudioManager.Instance != null && defeatSound != null)
                AudioManager.Instance.PlaySFX(defeatSound);
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
            if (_isPaused)
                Time.timeScale = 0f;
            else
                GameSpeedController.ApplyCurrentSpeed(); // vuelve a la velocidad que tenia antes de pausar, no siempre 1x

            if (_pauseOverlay != null)
            {
                _pauseOverlay.style.display = _isPaused ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void TryUnlockAbility(Button abilityButton, int cost, AudioClip unlockSound)
        {
            if (abilityButton == null) return;

            if (!abilityButton.ClassListContains("locked"))
            {
                Debug.Log($"Activando habilidad: {abilityButton.name}");
                return;
            }

            if (economy != null && economy.TrySpend(cost))
            {
                if (AudioManager.Instance != null && unlockSound != null)
                    AudioManager.Instance.PlaySFX(unlockSound);
                else
                    PlayClickSound();

                abilityButton.RemoveFromClassList("locked");
                Debug.Log($"Habilidad {abilityButton.name} desbloqueada.");
            }
            else
            {
                if (AudioManager.Instance != null && abilityUnlockFailedSound != null)
                    AudioManager.Instance.PlaySFX(abilityUnlockFailedSound);

                Debug.LogWarning($"Oro insuficiente para desbloquear {abilityButton.name} ({cost} necesarios).");
                ShakeElement(abilityButton);
            }
        }

        // Para las ranuras sin arrastre (2, 3 y 4): si esta bloqueada intenta
        // desbloquearla como siempre; si ya esta desbloqueada, el click es el
        // "uso" y pasa por el cooldown/limite de usos de la ranura.
        // Devuelve true solo si de verdad se uso (no si solo se desbloqueo o
        // si estaba en cooldown/sin usos), para que quien la llama sepa si
        // debe disparar el efecto real de la habilidad.
        private bool TryUnlockOrUseAbility(AbilitySlot slot, int cost)
        {
            if (slot == null || slot.Button == null) return false;

            if (slot.Button.ClassListContains("locked"))
            {
                TryUnlockAbility(slot.Button, cost, slot.UnlockSound);
                return false;
            }

            return TryUseAbility(slot);
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

        // --- Debug (llamado desde GameDebugWindow, nunca desde el juego) ---

        // Pone en 0 el cooldown y recarga los usos de las 5 ranuras, sin
        // gastar oro ni tocar si estan desbloqueadas.
        public void DebugResetAllCooldowns()
        {
            if (_allSlots == null)
                return;

            foreach (AbilitySlot slot in _allSlots)
            {
                if (slot == null)
                    continue;

                slot.RemainingCooldown = 0f;
                if (slot.Config != null && slot.Config.usageLimitMode != PowerUpUsageLimitMode.Unlimited)
                    slot.UsesRemaining = slot.Config.maxUses;

                RefreshSlotVisual(slot);
            }
        }

        // Quita el candado de las 5 ranuras sin gastar oro.
        public void DebugUnlockAllAbilities()
        {
            if (_allSlots == null)
                return;

            foreach (AbilitySlot slot in _allSlots)
            {
                if (slot != null && slot.Button != null)
                    slot.Button.RemoveFromClassList("locked");
            }
        }

        // AQUI se decide que powerup va en cada ranura y cuanto cuesta.
        // Para mover un powerup de ranura o cambiarle el precio, este es el
        // unico sitio que hay que tocar.
        private void BuildAllAbilitySlots(VisualElement root)
        {
            // La ranura 1 (bomba) ya trae su propio sprite de fondo en el
            // UXML, asi que no necesita un icono dibujado a mano.
            _slotAbility1 = BuildAbilitySlot(_ability1, root, "Ability1", ability1Cooldown, AbilityCost(1), bombAbility, null, null, ability1UnlockSound);
            _slotAbility2 = BuildAbilitySlot(_ability2, root, "Ability2", ability2Cooldown, AbilityCost(2), empAbility, null, DrawLightningIcon, ability2UnlockSound);
            _slotAbility3 = BuildAbilitySlot(_ability3, root, "Ability3", ability3Cooldown, AbilityCost(3), repulsionAbility, null, DrawRepulsionIcon, ability3UnlockSound);
            _slotAbility4 = BuildAbilitySlot(_ability4, root, "Ability4", ability4Cooldown, AbilityCost(4), repairAbility, null, DrawHealIcon, ability4UnlockSound);

            // La ranura 5 no es de arrastre: se usa con un click y su efecto
            // es desbloquear el tablero durante unos segundos.
            _slotAbility5 = BuildAbilitySlot(_ability5, root, "Ability5", ability5Cooldown, AbilityCost(5), null, UnlockBoardTemporarily, DrawUnlockIcon, ability5UnlockSound);

            _allSlots = new AbilitySlot[]
            {
                _slotAbility1, _slotAbility2, _slotAbility3, _slotAbility4, _slotAbility5
            };
        }

        // Arma el estado en vivo de una ranura a partir de su boton y de los
        // elementos de overlay/label que ya existen en el UXML, engancha el
        // dibujo del "reloj" de cooldown y registra los eventos que le tocan
        // segun sea de arrastre o de click.
        private AbilitySlot BuildAbilitySlot(Button button, VisualElement root, string elementPrefix,
            PowerUpCooldownConfig config, int cost, IDragAbility dragAbility, Action onUse,
            Action<MeshGenerationContext> drawIcon = null, AudioClip unlockSound = null)
        {
            if (button == null)
                return null;

            var slot = new AbilitySlot
            {
                Button = button,
                CooldownOverlay = root.Q<VisualElement>(elementPrefix + "CooldownOverlay"),
                UsesLabel = root.Q<Label>(elementPrefix + "UsesLabel"),
                Config = config,
                Cost = cost,
                DragAbility = dragAbility,
                OnUse = onUse,
                UnlockSound = unlockSound,
                RemainingCooldown = 0f,
                UsesRemaining = config != null && config.usageLimitMode != PowerUpUsageLimitMode.Unlimited ? config.maxUses : -1
            };

            var priceLabel = button.Q<Label>(className: "lock-price");
            if (priceLabel != null)
            {
                priceLabel.text = cost.ToString();
            }

            if (slot.CooldownOverlay != null)
            {
                slot.DrawCallback = context => DrawCooldownWipe(context, slot);
                slot.CooldownOverlay.generateVisualContent += slot.DrawCallback;
            }

            // Icono de la habilidad: la ranura 1 (bomba) trae su propio
            // sprite de fondo y no pasa drawIcon (queda null a proposito).
            slot.IconElement = root.Q<VisualElement>(elementPrefix + "Icon");
            if (slot.IconElement != null && drawIcon != null)
            {
                slot.IconDrawCallback = drawIcon;
                slot.IconElement.generateVisualContent += slot.IconDrawCallback;
            }

            // El desbloqueo (comprar el powerup) siempre es un click normal.
            slot.ClickedCallback = () => OnSlotClicked(slot);
            button.clicked += slot.ClickedCallback;
            button.RegisterCallback<PointerEnterEvent>(OnButtonHover);

            // El arrastre se registra en fase de captura (TrickleDown): el
            // Clickable interno del Button ya consume el PointerDown en fase
            // de burbuja para armar su propio clicked, asi que si nos
            // registramos en burbuja nuestro callback nunca llega a verlo.
            if (dragAbility != null)
            {
                slot.PointerDownCallback = evt => OnSlotPointerDown(slot, evt);
                slot.PointerUpCallback = evt => OnSlotPointerUp(slot, evt);
                button.RegisterCallback<PointerDownEvent>(slot.PointerDownCallback, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerUpEvent>(slot.PointerUpCallback, TrickleDown.TrickleDown);
            }

            RefreshSlotVisual(slot);
            return slot;
        }

        private void UnregisterAbilitySlot(AbilitySlot slot)
        {
            if (slot == null || slot.Button == null)
                return;

            if (slot.CooldownOverlay != null && slot.DrawCallback != null)
                slot.CooldownOverlay.generateVisualContent -= slot.DrawCallback;

            if (slot.IconElement != null && slot.IconDrawCallback != null)
                slot.IconElement.generateVisualContent -= slot.IconDrawCallback;

            if (slot.ClickedCallback != null)
                slot.Button.clicked -= slot.ClickedCallback;
            slot.Button.UnregisterCallback<PointerEnterEvent>(OnButtonHover);

            if (slot.PointerDownCallback != null)
                slot.Button.UnregisterCallback<PointerDownEvent>(slot.PointerDownCallback, TrickleDown.TrickleDown);
            if (slot.PointerUpCallback != null)
                slot.Button.UnregisterCallback<PointerUpEvent>(slot.PointerUpCallback, TrickleDown.TrickleDown);
        }

        // Recorre las ranuras cada frame y solo redibuja las que siguen en
        // cooldown (las que ya estan listas no cuestan nada).
        private void TickAbilityCooldowns()
        {
            foreach (AbilitySlot slot in _allSlots)
                TickSlot(slot);
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

        // --- Iconos de habilidades ---
        // Antes estos iconos eran emojis (🔒⚡💨🔧🔓) en un Label: se ven
        // bien en el Editor porque Windows les busca una fuente de
        // respaldo, pero un build (WebGL incluido) no tiene ese respaldo y
        // el glifo no se dibuja. Estos metodos los dibujan a mano con
        // Painter2D (la misma tecnica de DrawCooldownWipe), asi que no
        // dependen de ninguna fuente y se ven igual en Editor y en build.

        // Candado cerrado: una sola version para las 5 ranuras bloqueadas
        // y para el boton de comprar torre cuando no alcanza el oro.
        private void DrawLockIcon(MeshGenerationContext context)
        {
            Rect rect = context.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            float w = rect.width;
            float h = rect.height;
            Painter2D painter = context.painter2D;

            painter.fillColor = Color.white;
            painter.BeginPath();
            painter.MoveTo(new Vector2(w * 0.16f, h * 0.42f));
            painter.LineTo(new Vector2(w * 0.84f, h * 0.42f));
            painter.LineTo(new Vector2(w * 0.84f, h * 0.94f));
            painter.LineTo(new Vector2(w * 0.16f, h * 0.94f));
            painter.ClosePath();
            painter.Fill();

            painter.strokeColor = Color.white;
            painter.lineWidth = w * 0.14f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(new Vector2(w * 0.5f, h * 0.40f), w * 0.28f, Angle.Degrees(180f), Angle.Degrees(360f), ArcDirection.Clockwise);
            painter.Stroke();
        }

        // Candado abierto: icono de la ranura 5 (desbloqueo del tablero).
        // Misma base que el candado cerrado, pero con la argolla despegada
        // y flotando arriba en vez de apoyada sobre el cuerpo.
        private void DrawUnlockIcon(MeshGenerationContext context)
        {
            Rect rect = context.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            float w = rect.width;
            float h = rect.height;
            Color gold = new Color(0.945f, 0.769f, 0.059f);
            Painter2D painter = context.painter2D;

            painter.fillColor = gold;
            painter.BeginPath();
            painter.MoveTo(new Vector2(w * 0.20f, h * 0.50f));
            painter.LineTo(new Vector2(w * 0.80f, h * 0.50f));
            painter.LineTo(new Vector2(w * 0.80f, h * 0.94f));
            painter.LineTo(new Vector2(w * 0.20f, h * 0.94f));
            painter.ClosePath();
            painter.Fill();

            painter.strokeColor = gold;
            painter.lineWidth = w * 0.13f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(new Vector2(w * 0.62f, h * 0.26f), w * 0.26f, Angle.Degrees(180f), Angle.Degrees(360f), ArcDirection.Clockwise);
            painter.Stroke();
        }

        // Rayo: icono de la ranura 2 (EMP).
        private void DrawLightningIcon(MeshGenerationContext context)
        {
            Rect rect = context.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            float w = rect.width;
            float h = rect.height;
            Painter2D painter = context.painter2D;

            painter.fillColor = new Color(0.35f, 0.85f, 1f);
            painter.strokeColor = new Color(0.05f, 0.25f, 0.45f);
            painter.lineWidth = w * 0.03f;
            painter.lineJoin = LineJoin.Round;

            painter.BeginPath();
            painter.MoveTo(new Vector2(w * 0.60f, h * 0.04f));
            painter.LineTo(new Vector2(w * 0.26f, h * 0.56f));
            painter.LineTo(new Vector2(w * 0.46f, h * 0.56f));
            painter.LineTo(new Vector2(w * 0.36f, h * 0.96f));
            painter.LineTo(new Vector2(w * 0.76f, h * 0.42f));
            painter.LineTo(new Vector2(w * 0.54f, h * 0.42f));
            painter.ClosePath();
            painter.Fill();
            painter.Stroke();
        }

        // Estallido de flechas hacia afuera: icono de la ranura 3
        // (Repulsion). Representa lo que hace de verdad la habilidad:
        // empuja a los enemigos lejos del punto donde cae, haciendolos
        // retroceder por el camino.
        private void DrawRepulsionIcon(MeshGenerationContext context)
        {
            Rect rect = context.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            float w = rect.width;
            float h = rect.height;
            Painter2D painter = context.painter2D;
            Color orange = new Color(1f, 0.55f, 0.15f);

            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            float innerR = w * 0.14f;
            float shaftR = w * 0.34f;
            float tipR = w * 0.46f;
            float headHalfWidth = w * 0.13f;

            painter.fillColor = orange;
            painter.strokeColor = orange;
            painter.lineCap = LineCap.Round;
            painter.lineWidth = w * 0.09f;

            // Tres flechas repartidas en circulo (120 grados entre si),
            // cada una con su vara y su punta.
            float[] angles = { -90f, 30f, 150f };
            for (int i = 0; i < angles.Length; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector2 perp = new Vector2(-dir.y, dir.x);

                Vector2 innerPoint = center + dir * innerR;
                Vector2 shaftEnd = center + dir * shaftR;
                Vector2 tip = center + dir * tipR;
                Vector2 headBaseA = shaftEnd + perp * headHalfWidth;
                Vector2 headBaseB = shaftEnd - perp * headHalfWidth;

                painter.BeginPath();
                painter.MoveTo(innerPoint);
                painter.LineTo(shaftEnd);
                painter.Stroke();

                painter.BeginPath();
                painter.MoveTo(tip);
                painter.LineTo(headBaseA);
                painter.LineTo(headBaseB);
                painter.ClosePath();
                painter.Fill();
            }
        }

        // Cruz de curacion: icono de la ranura 4 (Reparacion).
        private void DrawHealIcon(MeshGenerationContext context)
        {
            Rect rect = context.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            float w = rect.width;
            float h = rect.height;
            Color green = new Color(0.25f, 0.80f, 0.35f);
            Painter2D painter = context.painter2D;

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float barThickness = w * 0.26f;
            float barLength = w * 0.74f;

            painter.fillColor = green;

            painter.BeginPath();
            painter.MoveTo(new Vector2(cx - barThickness * 0.5f, cy - barLength * 0.5f));
            painter.LineTo(new Vector2(cx + barThickness * 0.5f, cy - barLength * 0.5f));
            painter.LineTo(new Vector2(cx + barThickness * 0.5f, cy + barLength * 0.5f));
            painter.LineTo(new Vector2(cx - barThickness * 0.5f, cy + barLength * 0.5f));
            painter.ClosePath();
            painter.Fill();

            painter.BeginPath();
            painter.MoveTo(new Vector2(cx - barLength * 0.5f, cy - barThickness * 0.5f));
            painter.LineTo(new Vector2(cx + barLength * 0.5f, cy - barThickness * 0.5f));
            painter.LineTo(new Vector2(cx + barLength * 0.5f, cy + barThickness * 0.5f));
            painter.LineTo(new Vector2(cx - barLength * 0.5f, cy + barThickness * 0.5f));
            painter.ClosePath();
            painter.Fill();
        }
        #endregion

        #region UI Callbacks
        private void OnStartWaveClicked()
        {
            if (spawner != null && spawner.IsRunning) return;

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
            SetFinalWaveDanger(IsFinalWave());
            foreach (AbilitySlot slot in _allSlots)
                ResetPerWaveUses(slot);
        }

        // La oleada termino: vuelve a aparecer el boton para la siguiente,
        // salvo que esa oleada era la del jefe: ahi el nivel ya se gano.
        private void OnWaveFinished()
        {
            SetWaveActive(false);
            SetFinalWaveDanger(false);

            if (IsBossWaveJustCleared())
            {
                bool esNivelFinal = gameFlow != null
                    && gameFlow.GetLevelNumber(SceneManager.GetActiveScene().name) >= gameFlow.LevelCount;
                ShowVictoryScreen(esNivelFinal);
            }
        }

        private void OnBossSpawned(GameObject boss)
        {
            if (_bossWarning == null)
                return;

            _bossWarning.RemoveFromClassList("boss-warning-visible");
            _bossWarning.schedule.Execute(() =>
            {
                _bossWarning.AddToClassList("boss-warning-visible");
            }).StartingIn(20);
            _bossWarning.schedule.Execute(() =>
            {
                _bossWarning.RemoveFromClassList("boss-warning-visible");
            }).StartingIn(3200);
        }

        private bool IsFinalWave()
        {
            return spawner != null
                && spawner.bossPrefab != null
                && spawner.bossWave > 0
                && spawner.WaveNumber == spawner.bossWave;
        }

        private void SetFinalWaveDanger(bool active)
        {
            if (_dangerVignette == null)
                return;

            if (!active)
            {
                if (_dangerPulse != null)
                    _dangerPulse.Pause();
                _dangerPulse = null;
                _dangerVignette.RemoveFromClassList("danger-vignette-active");
                _dangerVignette.RemoveFromClassList("danger-vignette-bright");
                return;
            }

            _dangerVignette.AddToClassList("danger-vignette-active");
            if (_dangerPulse != null)
                _dangerPulse.Pause();
            _dangerPulse = _dangerVignette.schedule.Execute(() =>
            {
                _dangerVignette.ToggleInClassList("danger-vignette-bright");
            }).Every(450);
        }

        // True justo cuando termino la oleada del jefe (WaveFinished solo se
        // dispara con la oleada entera resuelta, jefe incluido: mientras el
        // jefe siga vivo la oleada no cierra, ver EnemySpawner). Sin jefe
        // configurado (bossPrefab o bossWave en 0) el nivel no se da nunca
        // por ganado solo, para no cambiar el comportamiento de niveles que
        // todavia no tengan uno.
        private bool IsBossWaveJustCleared()
        {
            return spawner != null
                && spawner.bossPrefab != null
                && spawner.bossWave > 0
                && spawner.WaveNumber == spawner.bossWave;
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
            SetFinalWaveDanger(false);
            ShowDefeatScreen();
        }

        private void OnBuyTowerClicked()
        {
            PlayClickSound();
            if (towerShop != null)
            {
                if (!towerShop.TryBuyTower())
                {
                    ShakeElement(_buyTowerButton);
                }
            }
            else
            {
                Debug.LogWarning("TowerShop no encontrado en la escena. La torre no se comprara.", this);
            }
        }

        private void ShakeElement(VisualElement element)
        {
            if (element == null) return;
            var seq = element.schedule;
            seq.Execute(() => element.style.translate = new Translate(new Length(-8, LengthUnit.Pixel), 0, 0)).StartingIn(0);
            seq.Execute(() => element.style.translate = new Translate(new Length(8, LengthUnit.Pixel), 0, 0)).StartingIn(50);
            seq.Execute(() => element.style.translate = new Translate(new Length(-8, LengthUnit.Pixel), 0, 0)).StartingIn(100);
            seq.Execute(() => element.style.translate = new Translate(new Length(8, LengthUnit.Pixel), 0, 0)).StartingIn(150);
            seq.Execute(() => element.style.translate = new StyleTranslate(StyleKeyword.Null)).StartingIn(200);
        }

        private void OnNextBoardClicked()
        {
            PlayClickSound();
            BoardSelector selector = FindFirstObjectByType<BoardSelector>();
            if (selector != null)
            {
                selector.SelectNext();
            }
        }

        private void OnSpeed1xClicked() { ChangeGameSpeed(1f); }
        private void OnSpeed2xClicked() { ChangeGameSpeed(2f); }

        // Cambia la velocidad de verdad (GameSpeedController, la misma que
        // usa la ventana Tower Defense > Game Debug) y deja el boton
        // correspondiente marcado. Si el juego esta congelado (pausa,
        // victoria, derrota...) el cambio se guarda pero no lo reanuda solo:
        // se vuelve a poner Time.timeScale en 0 despues de aplicarlo.
        private void ChangeGameSpeed(float multiplier)
        {
            PlayClickSound();

            bool wasFrozen = Time.timeScale == 0f;
            GameSpeedController.SetSpeed(multiplier);
            if (wasFrozen)
                Time.timeScale = 0f;

            RefreshSpeedButtons();
        }

        // Marca cual boton de velocidad esta activo ahora mismo.
        private void RefreshSpeedButtons()
        {
            SetSpeedButtonSelected(_speed1xButton, Mathf.Approximately(GameSpeedController.CurrentSpeed, 1f));
            SetSpeedButtonSelected(_speed2xButton, Mathf.Approximately(GameSpeedController.CurrentSpeed, 2f));
        }

        private void SetSpeedButtonSelected(Button button, bool selected)
        {
            if (button == null) return;

            if (selected)
                button.AddToClassList("speed-btn-selected");
            else
                button.RemoveFromClassList("speed-btn-selected");
        }

        // Click en una ranura: si sigue bloqueada intenta comprarla; si ya
        // esta desbloqueada solo hacen algo las de click (las de arrastre se
        // usan sosteniendo, no con un click suelto).
        private void OnSlotClicked(AbilitySlot slot)
        {
            if (slot.Button.ClassListContains("locked"))
            {
                TryUnlockAbility(slot.Button, slot.Cost, slot.UnlockSound);
                return;
            }

            if (slot.DragAbility != null)
                return;

            if (TryUseAbility(slot) && slot.OnUse != null)
                slot.OnUse();
        }

        // Sostener una ranura de arrastre ya desbloqueada empieza a mover el
        // powerup; soltar lo deja caer donde este el mouse/dedo. Mientras
        // siga bloqueada no hace nada aqui: comprarla es cosa de OnSlotClicked.
        private void OnSlotPointerDown(AbilitySlot slot, PointerDownEvent evt)
        {
            if (slot.Button.ClassListContains("locked"))
                return;

            // Si esta en cooldown o sin usos ni siquiera se deja empezar a
            // arrastrar (CanUseAbility solo mira, no consume nada).
            if (!CanUseAbility(slot))
                return;

            PlayClickSound();
            _draggingSlot = slot;
            slot.Button.CapturePointer(evt.pointerId);
            slot.DragAbility.BeginDrag();
        }

        private void OnSlotPointerUp(AbilitySlot slot, PointerUpEvent evt)
        {
            if (_draggingSlot != slot)
                return;

            _draggingSlot = null;
            if (slot.Button.HasPointerCapture(evt.pointerId))
                slot.Button.ReleasePointer(evt.pointerId);

            slot.DragAbility.EndDrag();

            // Solo se cobra el uso si el powerup de verdad cayo en un punto
            // valido (soltar fuera del mapa no cuenta como uso).
            if (slot.DragAbility.HasValidTarget)
                ConsumeAbility(slot);
        }

        // Efecto de la ranura 5: deja reordenar el tablero aunque haya una
        // oleada en curso, por temporaryUnlockDuration segundos.
        private void UnlockBoardTemporarily()
        {
            if (boardManager != null)
            {
                boardManager.UnlockTemporarily();

                if (AudioManager.Instance != null && boardUnlockSound != null)
                    AudioManager.Instance.PlaySFX(boardUnlockSound);
            }
            else
                Debug.LogWarning("GameHUD: falta BoardManager en la escena, no se puede desbloquear el tablero.", this);
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

        private void OnLockCameraClicked()
        {
            PlayClickSound();
            if (_cameraPan != null)
            {
                _cameraPan.panEnabled = !_cameraPan.panEnabled;
                RefreshLockCameraVisuals();
            }
        }

        private void RefreshLockCameraVisuals()
        {
            if (_lockCameraButton == null || _cameraPan == null) return;

            if (_cameraPan.panEnabled)
            {
                _lockCameraButton.text = "LIBRE";
                _lockCameraButton.RemoveFromClassList("speed-btn-selected");
            }
            else
            {
                _lockCameraButton.text = "FIJA";
                _lockCameraButton.AddToClassList("speed-btn-selected");
            }
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
            GameSpeedController.SetSpeed(GameSpeedController.DefaultSpeed);
            LoadSceneWithFade("Main Menu");
        }

        private void OnRetryLevelClicked()
        {
            PlayClickSound();
            GameSpeedController.SetSpeed(GameSpeedController.DefaultSpeed);
            LoadSceneWithFade(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void OnNextLevelClicked()
        {
            PlayClickSound();
            GameSpeedController.SetSpeed(GameSpeedController.DefaultSpeed);

            // El siguiente nivel sale de la lista del GameFlowConfig. Si no
            // hay config, o este era el ultimo, se recarga el actual (que es
            // lo que hacia antes siempre).
            string next = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (gameFlow != null)
            {
                int levelNumber = gameFlow.GetLevelNumber(next);
                string candidate = gameFlow.GetLevelScene(levelNumber + 1);
                if (levelNumber > 0 && !string.IsNullOrEmpty(candidate))
                    next = candidate;
            }

            LoadSceneWithFade(next);
        }

        private void LoadSceneWithFade(string sceneName)
        {
            if (_fadeOverlay != null)
            {
                _fadeOverlay.style.opacity = 1f;
                _fadeOverlay.schedule.Execute(() => UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName)).StartingIn(500);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        private void OnRetryClicked()
        {
            PlayClickSound();
            GameSpeedController.SetSpeed(GameSpeedController.DefaultSpeed);
            LoadSceneWithFade(SceneManager.GetActiveScene().name);
        }

        private void OnGithubClicked()
        {
            PlayClickSound();
            Application.OpenURL("https://github.com/CamiBlackFire-Dev/TowerDefense-FinalProyect");
        }

        #region Ballista Logic
        private void OnBallistaBuyClicked()
        {
            if (instantBallistaPlacement)
            {
                CrossbowSlot emptySlot = null;
                var allSlots = FindObjectsByType<CrossbowSlot>(FindObjectsSortMode.None);
                foreach (var slot in allSlots)
                {
                    if (!slot.IsUnlocked)
                    {
                        emptySlot = slot;
                        break;
                    }
                }

                if (emptySlot == null)
                {
                    // No hay slots vacios
                    if (AudioManager.Instance != null && abilityUnlockFailedSound != null)
                        AudioManager.Instance.PlaySFX(abilityUnlockFailedSound);
                    ShakeElement(_ballistaBuyBtn);
                    return;
                }

                if (economy != null && economy.Money >= BallistaCost)
                {
                    economy.TrySpend(BallistaCost);

                    emptySlot.Unlock();
                    if (_currentGlobalArrow != null)
                    {
                        emptySlot.UnlockArrow(_currentGlobalArrow);
                        emptySlot.SelectArrow(_currentGlobalArrow);
                    }

                    PlayClickSound();
                    UpdateBallistaUI();
                }
                else
                {
                    if (AudioManager.Instance != null && abilityUnlockFailedSound != null)
                        AudioManager.Instance.PlaySFX(abilityUnlockFailedSound);
                    ShakeElement(_ballistaBuyBtn);
                }
            }
            else
            {
                if (economy != null && economy.Money >= BallistaCost)
                {
                    economy.TrySpend(BallistaCost);
                    _ballistaCount++;
                    UpdateBallistaUI();
                    PlayClickSound();
                }
                else
                {
                    if (AudioManager.Instance != null && abilityUnlockFailedSound != null)
                        AudioManager.Instance.PlaySFX(abilityUnlockFailedSound);
                    ShakeElement(_ballistaBuyBtn);
                }
            }
        }

        private void OnBallistaPointerDown(PointerDownEvent evt)
        {
            if (_ballistaCount <= 0 || ballistaAbility == null)
                return;

            PlayClickSound();
            _isBallistaDragging = true;
            _ballistaBuyBtn.CapturePointer(evt.pointerId);
            ballistaAbility.BeginDrag();
        }

        private void OnBallistaPointerUp(PointerUpEvent evt)
        {
            if (!_isBallistaDragging || ballistaAbility == null)
                return;

            _isBallistaDragging = false;
            if (_ballistaBuyBtn.HasPointerCapture(evt.pointerId))
                _ballistaBuyBtn.ReleasePointer(evt.pointerId);

            ballistaAbility.EndDrag();

            if (ballistaAbility.HasValidTarget)
            {
                _ballistaCount--;
                UpdateBallistaUI();

                // Si hay una flecha global seleccionada, aplicarla al nuevo slot
                if (_currentGlobalArrow != null)
                {
                    ApplyGlobalArrowToAllSlots();
                }
            }
        }

        private void OnArrowUpgradeClicked(ArrowEffectType type)
        {
            bool isUnlocked = false;
            ArrowData dataToUse = null;

            switch (type)
            {
                case ArrowEffectType.Normal:
                    isUnlocked = true;
                    dataToUse = arrowNormalData;
                    break;
                case ArrowEffectType.Fire:
                    isUnlocked = _fireUnlocked;
                    dataToUse = arrowFireData;
                    break;
                case ArrowEffectType.Ice:
                    isUnlocked = _iceUnlocked;
                    dataToUse = arrowIceData;
                    break;
                case ArrowEffectType.Poison:
                    isUnlocked = _poisonUnlocked;
                    dataToUse = arrowPoisonData;
                    break;
            }

            if (!isUnlocked)
            {
                // Try to unlock
                if (economy != null && economy.Money >= ArrowUnlockCost)
                {
                    economy.TrySpend(ArrowUnlockCost);

                    switch (type)
                    {
                        case ArrowEffectType.Fire: _fireUnlocked = true; break;
                        case ArrowEffectType.Ice: _iceUnlocked = true; break;
                        case ArrowEffectType.Poison: _poisonUnlocked = true; break;
                    }

                    SelectGlobalArrow(dataToUse);
                    PlayClickSound();
                }
                else
                {
                    if (AudioManager.Instance != null && abilityUnlockFailedSound != null)
                        AudioManager.Instance.PlaySFX(abilityUnlockFailedSound);

                    if (type == ArrowEffectType.Fire) ShakeElement(_arrowFireBtn);
                    if (type == ArrowEffectType.Ice) ShakeElement(_arrowIceBtn);
                    if (type == ArrowEffectType.Poison) ShakeElement(_arrowPoisonBtn);
                }
            }
            else
            {
                // Already unlocked, try to switch
                if (_currentGlobalArrow != dataToUse)
                {
                    int cost = (type == ArrowEffectType.Normal) ? 0 : ArrowSwitchCost;

                    if (economy != null && economy.Money >= cost)
                    {
                        if (cost > 0) economy.TrySpend(cost);
                        SelectGlobalArrow(dataToUse);
                        PlayClickSound();
                    }
                    else
                    {
                        if (AudioManager.Instance != null && abilityUnlockFailedSound != null)
                            AudioManager.Instance.PlaySFX(abilityUnlockFailedSound);

                        if (type == ArrowEffectType.Normal) ShakeElement(_arrowNormalBtn);
                        if (type == ArrowEffectType.Fire) ShakeElement(_arrowFireBtn);
                        if (type == ArrowEffectType.Ice) ShakeElement(_arrowIceBtn);
                        if (type == ArrowEffectType.Poison) ShakeElement(_arrowPoisonBtn);
                    }
                }
            }

            UpdateBallistaUI();
        }

        private void SelectGlobalArrow(ArrowData data)
        {
            if (data == null) return;
            _currentGlobalArrow = data;
            ApplyGlobalArrowToAllSlots();
        }

        private void ApplyGlobalArrowToAllSlots()
        {
            if (_currentGlobalArrow == null) return;

            CrossbowSlot[] allSlots = FindObjectsByType<CrossbowSlot>(FindObjectsSortMode.None);
            foreach (var slot in allSlots)
            {
                if (slot.IsUnlocked)
                {
                    slot.UnlockArrow(_currentGlobalArrow);
                    slot.SelectArrow(_currentGlobalArrow);
                }
            }
        }

        private void UpdateBallistaUI()
        {
            if (_ballistaCountLabel != null)
            {
                if (instantBallistaPlacement)
                {
                    _ballistaCountLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    _ballistaCountLabel.style.display = DisplayStyle.Flex;
                    _ballistaCountLabel.text = _ballistaCount.ToString();
                }
            }

            if (_ballistaPriceText != null)
            {
                _ballistaPriceText.text = BallistaCost.ToString();
            }

            var goldColor = new StyleColor(new Color32(241, 196, 15, 255));
            var grayColor = new StyleColor(Color.gray);

            if (_arrowFireBtn != null)
            {
                _arrowFireBtn.RemoveFromClassList("locked");
                _arrowFireBtn.RemoveFromClassList("selected");
                _arrowFireBtn.style.borderTopColor = grayColor;
                _arrowFireBtn.style.borderBottomColor = grayColor;
                _arrowFireBtn.style.borderLeftColor = grayColor;
                _arrowFireBtn.style.borderRightColor = grayColor;

                if (!_fireUnlocked) _arrowFireBtn.AddToClassList("locked");
                else if (_currentGlobalArrow == arrowFireData)
                {
                    _arrowFireBtn.AddToClassList("selected");
                    _arrowFireBtn.style.borderTopColor = goldColor;
                    _arrowFireBtn.style.borderBottomColor = goldColor;
                    _arrowFireBtn.style.borderLeftColor = goldColor;
                    _arrowFireBtn.style.borderRightColor = goldColor;
                }

                if (_firePriceContainer != null)
                {
                    bool isSelected = (_currentGlobalArrow == arrowFireData);
                    _firePriceContainer.style.display = isSelected ? DisplayStyle.None : DisplayStyle.Flex;
                    var priceLabel = _firePriceContainer.Q<Label>("FirePriceText");
                    if (priceLabel != null) priceLabel.text = _fireUnlocked ? ArrowSwitchCost.ToString() : ArrowUnlockCost.ToString();
                }
            }

            if (_arrowIceBtn != null)
            {
                _arrowIceBtn.RemoveFromClassList("locked");
                _arrowIceBtn.RemoveFromClassList("selected");
                _arrowIceBtn.style.borderTopColor = grayColor;
                _arrowIceBtn.style.borderBottomColor = grayColor;
                _arrowIceBtn.style.borderLeftColor = grayColor;
                _arrowIceBtn.style.borderRightColor = grayColor;

                if (!_iceUnlocked) _arrowIceBtn.AddToClassList("locked");
                else if (_currentGlobalArrow == arrowIceData)
                {
                    _arrowIceBtn.AddToClassList("selected");
                    _arrowIceBtn.style.borderTopColor = goldColor;
                    _arrowIceBtn.style.borderBottomColor = goldColor;
                    _arrowIceBtn.style.borderLeftColor = goldColor;
                    _arrowIceBtn.style.borderRightColor = goldColor;
                }

                if (_icePriceContainer != null)
                {
                    bool isSelected = (_currentGlobalArrow == arrowIceData);
                    _icePriceContainer.style.display = isSelected ? DisplayStyle.None : DisplayStyle.Flex;
                    var priceLabel = _icePriceContainer.Q<Label>("IcePriceText");
                    if (priceLabel != null) priceLabel.text = _iceUnlocked ? ArrowSwitchCost.ToString() : ArrowUnlockCost.ToString();
                }
            }

            if (_arrowPoisonBtn != null)
            {
                _arrowPoisonBtn.RemoveFromClassList("locked");
                _arrowPoisonBtn.RemoveFromClassList("selected");
                _arrowPoisonBtn.style.borderTopColor = grayColor;
                _arrowPoisonBtn.style.borderBottomColor = grayColor;
                _arrowPoisonBtn.style.borderLeftColor = grayColor;
                _arrowPoisonBtn.style.borderRightColor = grayColor;

                if (!_poisonUnlocked) _arrowPoisonBtn.AddToClassList("locked");
                else if (_currentGlobalArrow == arrowPoisonData)
                {
                    _arrowPoisonBtn.AddToClassList("selected");
                    _arrowPoisonBtn.style.borderTopColor = goldColor;
                    _arrowPoisonBtn.style.borderBottomColor = goldColor;
                    _arrowPoisonBtn.style.borderLeftColor = goldColor;
                    _arrowPoisonBtn.style.borderRightColor = goldColor;
                }

                if (_poisonPriceContainer != null)
                {
                    bool isSelected = (_currentGlobalArrow == arrowPoisonData);
                    _poisonPriceContainer.style.display = isSelected ? DisplayStyle.None : DisplayStyle.Flex;
                    var priceLabel = _poisonPriceContainer.Q<Label>("PoisonPriceText");
                    if (priceLabel != null) priceLabel.text = _poisonUnlocked ? ArrowSwitchCost.ToString() : ArrowUnlockCost.ToString();
                }
            }

            if (_arrowNormalBtn != null)
            {
                _arrowNormalBtn.RemoveFromClassList("selected");
                _arrowNormalBtn.style.borderTopColor = grayColor;
                _arrowNormalBtn.style.borderBottomColor = grayColor;
                _arrowNormalBtn.style.borderLeftColor = grayColor;
                _arrowNormalBtn.style.borderRightColor = grayColor;

                if (_currentGlobalArrow == null || _currentGlobalArrow == arrowNormalData)
                {
                    _arrowNormalBtn.AddToClassList("selected");
                    _arrowNormalBtn.style.borderTopColor = goldColor;
                    _arrowNormalBtn.style.borderBottomColor = goldColor;
                    _arrowNormalBtn.style.borderLeftColor = goldColor;
                    _arrowNormalBtn.style.borderRightColor = goldColor;
                }
            }
        }
        #endregion
        #endregion
    }
}
