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

        #region Unity Lifecycle
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;

            if (root == null) return;

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

            string sceneName = gameFlow != null && !string.IsNullOrEmpty(gameFlow.gameplayScene)
                ? gameFlow.gameplayScene
                : FallbackGameplayScene;

            if (gameFlow == null)
                Debug.LogWarning("MainMenuController: no hay GameFlowConfig asignado, se usa " + FallbackGameplayScene + " por defecto.", this);

            SceneManager.LoadScene(sceneName);
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
