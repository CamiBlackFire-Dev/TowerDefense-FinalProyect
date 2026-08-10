using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Custom.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _gameLogo;
        private Button _playButton;
        private Button _optionsButton;
        private Button _quitButton;

        [Header("Animación del Logo")]
        public float animSpeed = 2f;
        public float animAmplitude = 10f;

        [Header("Audio")]
        public AudioClip menuMusic;
        public AudioClip hoverSound;
        public AudioClip clickSound;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("MainMenuController: No se encontró el rootVisualElement.");
                return;
            }

            // Iniciar música
            if (AudioManager.Instance != null && menuMusic != null)
            {
                AudioManager.Instance.PlayMusic(menuMusic);
            }

            _gameLogo = root.Q<VisualElement>("GameLogo");
            _playButton = root.Q<Button>("PlayButton");
            _optionsButton = root.Q<Button>("OptionsButton");
            _quitButton = root.Q<Button>("QuitButton");

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
        }

        private void Update()
        {
            if (_gameLogo != null)
            {
                float offset = Mathf.Sin(Time.time * animSpeed) * animAmplitude;
                _gameLogo.style.translate = new StyleTranslate(new Translate(0, offset, 0));
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

        private void OnPlayClicked()
        {
            PlayClickSound();
            Debug.Log("Cargando escena principal...");
            SceneManager.LoadScene("0_Main");
        }

        private void OnOptionsClicked()
        {
            PlayClickSound();
            Debug.Log("Abriendo opciones...");
        }

        private void OnQuitClicked()
        {
            PlayClickSound();
            Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
