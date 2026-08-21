using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Custom.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        #region UI Elements
        private UIDocument _uiDocument;
        private VisualElement _gameLogo;
        private Button _playButton;
        private Button _optionsButton;
        private Button _quitButton;

        private VisualElement _optionsOverlay;
        private Button _backButton;
        private Slider _masterVolumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;

        private VisualElement _levelSelectOverlay;
        private Button _tutorialLevelButton;
        private Button _level1Button;
        private Button _level2Button;
        private Button _level3Button;
        private Button _closeLevelSelectButton;
        #endregion

        #region Audio & Animation
        [Header("Animación del Logo")]
        public float animSpeed = 2f;
        public float animAmplitude = 10f;

        [Header("Audio")]
        public AudioClip menuMusic;
        public AudioClip hoverSound;
        public AudioClip clickSound;
        #endregion

        [Header("Flujo de escenas")]
        // De aca sale la escena a la que entra el boton Jugar. Se edita mas
        // comodo desde la ventana Tower Defense > Escenas. Si se deja vacio
        // se usa FallbackGameplayScene para no dejar el boton roto.
        public GameFlowConfig gameFlow;
        private const string FallbackGameplayScene = "0_Main";

        private VisualElement _fadeOverlay;

        #region Unity Lifecycle
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;

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

            if (AudioManager.Instance != null && menuMusic != null)
            {
                AudioManager.Instance.PlayMusic(menuMusic);
            }

            _gameLogo = root.Q<VisualElement>("GameLogo");
            _playButton = root.Q<Button>("PlayButton");
            _optionsButton = root.Q<Button>("OptionsButton");
            _quitButton = root.Q<Button>("QuitButton");

            _optionsOverlay = root.Q<VisualElement>("OptionsOverlay");
            _backButton = root.Q<Button>("BackButton");
            _masterVolumeSlider = root.Q<Slider>("MasterVolumeSlider");
            _musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
            _sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");

            _levelSelectOverlay = root.Q<VisualElement>("LevelSelectOverlay");
            _tutorialLevelButton = root.Q<Button>("TutorialLevelButton");
            _level1Button = root.Q<Button>("Level1Button");
            _level2Button = root.Q<Button>("Level2Button");
            _level3Button = root.Q<Button>("Level3Button");
            _closeLevelSelectButton = root.Q<Button>("CloseLevelSelectButton");

            if (_playButton != null)
            {
                _playButton.clicked += OnPlayClicked;
                _playButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_optionsButton != null)
            {
                _optionsButton.clicked += OnOptionsClicked;
                _optionsButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_quitButton != null)
            {
                _quitButton.clicked += OnQuitClicked;
                _quitButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_backButton != null)
            {
                _backButton.clicked += OnBackClicked;
                _backButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            int maxLevel = PlayerPrefs.GetInt("MaxLevelUnlocked", 0);

            if (_tutorialLevelButton != null)
            {
                _tutorialLevelButton.clicked += OnTutorialLevelClicked;
                _tutorialLevelButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_level1Button != null)
            {
                _level1Button.clicked += OnLevel1Clicked;
                _level1Button.RegisterCallback<PointerEnterEvent>(OnButtonHover);
                _level1Button.SetEnabled(maxLevel >= 1);
                _level1Button.text = maxLevel >= 1 ? "NIVEL 1" : "NIVEL 1 🔒";
            }
            if (_level2Button != null)
            {
                _level2Button.clicked += OnLevel2Clicked;
                _level2Button.RegisterCallback<PointerEnterEvent>(OnButtonHover);
                _level2Button.SetEnabled(maxLevel >= 2);
                _level2Button.text = maxLevel >= 2 ? "NIVEL 2" : "NIVEL 2 🔒";
            }
            if (_level3Button != null)
            {
                _level3Button.clicked += OnLevel3Clicked;
                _level3Button.RegisterCallback<PointerEnterEvent>(OnButtonHover);
                _level3Button.SetEnabled(maxLevel >= 3);
                _level3Button.text = maxLevel >= 3 ? "NIVEL 3" : "NIVEL 3 🔒";
            }
            if (_closeLevelSelectButton != null)
            {
                _closeLevelSelectButton.clicked += OnCloseLevelSelectClicked;
                _closeLevelSelectButton.RegisterCallback<PointerEnterEvent>(OnButtonHover);
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
        }

        private void OnDisable()
        {
            if (_playButton != null)
            {
                _playButton.clicked -= OnPlayClicked;
                _playButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_optionsButton != null)
            {
                _optionsButton.clicked -= OnOptionsClicked;
                _optionsButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_quitButton != null)
            {
                _quitButton.clicked -= OnQuitClicked;
                _quitButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_backButton != null)
            {
                _backButton.clicked -= OnBackClicked;
                _backButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }

            if (_tutorialLevelButton != null)
            {
                _tutorialLevelButton.clicked -= OnTutorialLevelClicked;
                _tutorialLevelButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_level1Button != null)
            {
                _level1Button.clicked -= OnLevel1Clicked;
                _level1Button.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_level2Button != null)
            {
                _level2Button.clicked -= OnLevel2Clicked;
                _level2Button.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_level3Button != null)
            {
                _level3Button.clicked -= OnLevel3Clicked;
                _level3Button.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (_closeLevelSelectButton != null)
            {
                _closeLevelSelectButton.clicked -= OnCloseLevelSelectClicked;
                _closeLevelSelectButton.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
            }
        }

        private void Update()
        {
            if (_gameLogo != null)
            {
                float offset = Mathf.Sin(Time.time * animSpeed) * animAmplitude;
                _gameLogo.style.translate = new StyleTranslate(new Translate(0, offset, 0));
            }
        }
        #endregion

        #region UI Callbacks
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

        private void OnPlayClicked()
        {
            PlayClickSound();
            if (_levelSelectOverlay != null) _levelSelectOverlay.style.display = DisplayStyle.Flex;
        }

        private void OnTutorialLevelClicked()
        {
            PlayClickSound();
            LoadSceneWithFade("1_Tutorial");
        }

        private void OnLevel1Clicked()
        {
            LoadLevel(1);
        }

        // Carga el nivel N leyendo su escena del GameFlowConfig, para no
        // tener los nombres escritos a mano aqui. El nivel 1 cae en
        // gameplayScene si la lista de niveles esta vacia, que es como
        // funcionaba antes de que existiera esa lista.
        private void LoadLevel(int levelNumber)
        {
            PlayClickSound();

            string sceneName = gameFlow != null ? gameFlow.GetLevelScene(levelNumber) : string.Empty;

            if (string.IsNullOrEmpty(sceneName) && levelNumber == 1)
            {
                sceneName = gameFlow != null && !string.IsNullOrEmpty(gameFlow.gameplayScene)
                    ? gameFlow.gameplayScene
                    : FallbackGameplayScene;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("MainMenuController: el nivel " + levelNumber +
                    " no tiene escena asignada en el GameFlowConfig.", this);
                return;
            }

            LoadSceneWithFade(sceneName);
        }

        private void LoadSceneWithFade(string sceneName)
        {
            if (_fadeOverlay != null)
            {
                _fadeOverlay.style.opacity = 1f;
                _fadeOverlay.schedule.Execute(() => SceneManager.LoadScene(sceneName)).StartingIn(500);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private void OnLevel2Clicked()
        {
            LoadLevel(2);
        }

        private void OnLevel3Clicked()
        {
            LoadLevel(3);
        }

        private void OnCloseLevelSelectClicked()
        {
            PlayClickSound();
            if (_levelSelectOverlay != null) _levelSelectOverlay.style.display = DisplayStyle.None;
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

        private void OnQuitClicked()
        {
            PlayClickSound();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion
    }
}
