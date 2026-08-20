using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Custom.UI;

public enum TutorialState
{
    Init,
    BuyTowers,
    MoveTowers,
    MergeTowers,
    StartWave,
    EarnGold,
    RetryWave,
    UnlockBomb,
    UseBomb,
    Finished
}

[RequireComponent(typeof(UIDocument))]
public class TutorialManager : MonoBehaviour
{
    #region Public Fields
    public TutorialState currentState = TutorialState.Init;
    #endregion

    #region Private Fields
    private UIDocument _tutorialUIDocument;
    private GameHUDController _gameHUD;
    private BoardManager _boardManager;
    private EconomyManager _economyManager;
    private EnemySpawner _enemySpawner;

    private Button _buyTowerButton;
    private Button _startWaveButton;
    private Button _ability1Button;

    private VisualElement _tutorialOverlay;
    private VisualElement _dimTop, _dimBottom, _dimLeft, _dimRight;
    private VisualElement _spotlightBox;
    private VisualElement _wasdHint;
    private Label _step1, _step2, _step3, _step4, _step5, _step6, _step7;
    private Label _tutorialMessage;

    private VisualElement _fadeOverlay;

    private VisualElement _introOverlay;
    private VisualElement _outroOverlay;
    private Button _introStartButton;
    private Button _outroMenuButton;
    private Button _outroNextButton;

    private int _towersBought = 0;
    private int _lastMoney = 0;
    private int _targetGold = 0;
    private bool _hasMoved = false;

    private Button _activeTargetButton;
    private bool _isGlowUrgent;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        _tutorialUIDocument = GetComponent<UIDocument>();

        _gameHUD = FindFirstObjectByType<GameHUDController>();
        _boardManager = FindFirstObjectByType<BoardManager>();
        _economyManager = FindFirstObjectByType<EconomyManager>();
        _enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (_gameHUD != null)
        {
            var root = _gameHUD.GetComponent<UIDocument>().rootVisualElement;
            _buyTowerButton = root.Q<Button>("BuyTowerButton");
            _startWaveButton = root.Q<Button>("StartWaveButton");
            _ability1Button = root.Q<Button>("Ability1");
        }

        if (_tutorialUIDocument != null)
        {
            var root = _tutorialUIDocument.rootVisualElement;

            _fadeOverlay = new UnityEngine.UIElements.VisualElement();
            _fadeOverlay.style.position = UnityEngine.UIElements.Position.Absolute;
            _fadeOverlay.style.left = 0;
            _fadeOverlay.style.right = 0;
            _fadeOverlay.style.top = 0;
            _fadeOverlay.style.bottom = 0;
            _fadeOverlay.style.backgroundColor = UnityEngine.Color.black;
            _fadeOverlay.style.opacity = 1f;
            _fadeOverlay.style.transitionDuration = new System.Collections.Generic.List<UnityEngine.UIElements.TimeValue> { new UnityEngine.UIElements.TimeValue(0.5f) };
            _fadeOverlay.style.transitionProperty = new System.Collections.Generic.List<UnityEngine.UIElements.StylePropertyName> { new UnityEngine.UIElements.StylePropertyName("opacity") };
            _fadeOverlay.pickingMode = UnityEngine.UIElements.PickingMode.Ignore;
            root.Add(_fadeOverlay);
            _fadeOverlay.schedule.Execute(() => _fadeOverlay.style.opacity = 0f).StartingIn(100);

            _tutorialOverlay = root.Q<VisualElement>("TutorialOverlay");
            _dimTop = root.Q<VisualElement>("DimTop");
            _dimBottom = root.Q<VisualElement>("DimBottom");
            _dimLeft = root.Q<VisualElement>("DimLeft");
            _dimRight = root.Q<VisualElement>("DimRight");
            _spotlightBox = root.Q<VisualElement>("SpotlightBox");
            _wasdHint = root.Q<VisualElement>("WASDHint");

            _step1 = root.Q<Label>("Step1");
            _step2 = root.Q<Label>("Step2");
            _step3 = root.Q<Label>("Step3");
            _step4 = root.Q<Label>("Step4");
            _step5 = root.Q<Label>("Step5");
            _step6 = root.Q<Label>("Step6");
            _step7 = root.Q<Label>("Step7");
            _tutorialMessage = root.Q<Label>("TutorialMessage");

            _introOverlay = root.Q<VisualElement>("TutorialIntroOverlay");
            _outroOverlay = root.Q<VisualElement>("TutorialOutroOverlay");
            _introStartButton = root.Q<Button>("IntroStartButton");
            _outroMenuButton = root.Q<Button>("OutroMenuButton");
            _outroNextButton = root.Q<Button>("OutroNextButton");

            if (_introStartButton != null) _introStartButton.clicked += OnIntroStartClicked;
            if (_outroMenuButton != null) _outroMenuButton.clicked += OnOutroMenuClicked;
            if (_outroNextButton != null) _outroNextButton.clicked += OnOutroNextClicked;
        }

        if (_boardManager != null && _boardManager.inputController != null)
        {
            _boardManager.inputController.MoveRequested += OnPlayerMoved;
        }

        if (_economyManager != null)
        {
            _lastMoney = _economyManager.Money;
        }

        if (_tutorialOverlay != null) _tutorialOverlay.style.display = DisplayStyle.None;
        if (_introOverlay != null) _introOverlay.style.display = DisplayStyle.Flex;
        if (_outroOverlay != null) _outroOverlay.style.display = DisplayStyle.None;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        UpdateSpotlight();

        if (_economyManager != null)
        {
            if (_economyManager.Money < _lastMoney)
            {
                if (currentState == TutorialState.BuyTowers)
                {
                    _towersBought++;
                }
            }
            _lastMoney = _economyManager.Money;
        }

        switch (currentState)
        {
            case TutorialState.BuyTowers:
                if (_towersBought >= 3)
                {
                    AdvanceState(TutorialState.MoveTowers);
                }
                break;

            case TutorialState.MoveTowers:
                if (_hasMoved)
                {
                    AdvanceState(TutorialState.MergeTowers);
                }
                break;

            case TutorialState.MergeTowers:
                if (HasTowerOfLevel(2))
                {
                    AdvanceState(TutorialState.StartWave);
                }
                break;

            case TutorialState.StartWave:
                bool isGood = IsTowerInGoodPosition();
                if (isGood && _activeTargetButton == null)
                {
                    SetTargetButton(_startWaveButton, false);
                    _tutorialMessage.text = "¡Excelente! Ahora estás listo. Pulsa EMPEZAR OLEADA para que vengan los enemigos.";
                }
                else if (!isGood && _activeTargetButton != null)
                {
                    SetTargetButton(null);
                    _tutorialMessage.text = "¡Cuidado! Tu torre quedó muy lejos. Usa las teclas para moverla hacia la izquierda y asegurar la defensa.";
                }

                if (_enemySpawner != null && _enemySpawner.IsRunning)
                {
                    AdvanceState(TutorialState.EarnGold);
                }
                break;

            case TutorialState.EarnGold:
                if (_economyManager != null && _economyManager.Money >= _targetGold)
                {
                    AdvanceState(TutorialState.UnlockBomb);
                }
                else if (_enemySpawner != null && !_enemySpawner.IsRunning)
                {
                    AdvanceState(TutorialState.RetryWave);
                }
                break;

            case TutorialState.RetryWave:
                if (_economyManager != null && _economyManager.Money >= _targetGold)
                {
                    AdvanceState(TutorialState.UnlockBomb);
                }
                else if (_enemySpawner != null && _enemySpawner.IsRunning)
                {
                    AdvanceState(TutorialState.EarnGold);
                }
                break;

            case TutorialState.UnlockBomb:
                if (_ability1Button != null && !_ability1Button.ClassListContains("locked"))
                {
                    AdvanceState(TutorialState.UseBomb);
                }
                break;

            case TutorialState.UseBomb:
                BombAbility[] explosions = FindObjectsByType<BombAbility>(FindObjectsSortMode.None);
                if (explosions.Length > 0 && !_step7.ClassListContains("tutorial-step-done"))
                {
                    MarkStepDone(_step7);
                    _tutorialMessage.text = "¡Excelente! Has usado la bomba. Ahora defiende tu castillo hasta que termine la oleada.";
                }

                if (_step7.ClassListContains("tutorial-step-done") && _enemySpawner != null && !_enemySpawner.IsRunning)
                {
                    AdvanceState(TutorialState.Finished);
                }
                break;

            case TutorialState.Finished:
                break;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (_boardManager != null && _boardManager.inputController != null)
        {
            _boardManager.inputController.MoveRequested -= OnPlayerMoved;
        }
    }
    #endregion

    #region State Management
    private void AdvanceState(TutorialState newState)
    {
        currentState = newState;

        if (Time.timeScale == 0f && newState != TutorialState.UnlockBomb && newState != TutorialState.Finished && newState != TutorialState.Init)
        {
            Time.timeScale = 1f;
        }

        switch (newState)
        {
            case TutorialState.BuyTowers:
                SetTargetButton(_buyTowerButton, false);
                _tutorialMessage.text = "¡Necesitas defensas! Compra 3 torres haciendo click en el botón inferior derecho.";
                if (_wasdHint != null) _wasdHint.style.display = DisplayStyle.None;
                break;

            case TutorialState.MoveTowers:
                SetTargetButton(null);
                MarkStepDone(_step1);
                _tutorialMessage.text = "Usa las teclas W, A, S, D para deslizar las torres por el tablero.";
                if (_wasdHint != null) _wasdHint.style.display = DisplayStyle.Flex;
                break;

            case TutorialState.MergeTowers:
                SetTargetButton(null);
                MarkStepDone(_step2);
                _tutorialMessage.text = "¡Eso es! Sigue moviéndolas hasta que 2 torres iguales choquen. ¡Se fusionarán y subirán de nivel!";
                if (_wasdHint != null) _wasdHint.style.display = DisplayStyle.Flex;
                break;

            case TutorialState.StartWave:
                MarkStepDone(_step3);
                SetTargetButton(null);
                if (_wasdHint != null) _wasdHint.style.display = DisplayStyle.None;
                break;

            case TutorialState.EarnGold:
                SetTargetButton(null);
                MarkStepDone(_step4);
                if (_targetGold == 0 && _economyManager != null) _targetGold = _economyManager.Money + 100;
                _tutorialMessage.text = "Observa cómo tus torres atacan. Al eliminar enemigos ganarás oro. Ahorra hasta conseguir 100 de oro.";
                break;

            case TutorialState.RetryWave:
                SetTargetButton(_startWaveButton, true);
                _tutorialMessage.text = "¡La oleada terminó y no te alcanzó el oro! Pulsa EMPEZAR OLEADA para jugar otra oleada e intentarlo de nuevo.";
                break;

            case TutorialState.UnlockBomb:
                Time.timeScale = 0f;
                MarkStepDone(_step5);
                SetTargetButton(_ability1Button, true);
                _tutorialMessage.text = "¡Llegaste a 100 de oro! El tiempo se ha pausado. Clickea el candado de la habilidad (Bomba) para desbloquearla.";
                break;

            case TutorialState.UseBomb:
                SetTargetButton(null);
                MarkStepDone(_step6);
                _tutorialMessage.text = "Mantén pulsado el botón de la Bomba, arrástrala sobre el camino de los enemigos y suéltala.";
                break;

            case TutorialState.Finished:
                SetTargetButton(null);
                MarkStepDone(_step7);
                _tutorialMessage.text = "¡Felicidades! Has completado el tutorial. ¡Mucha suerte en la batalla real!";

                if (PlayerPrefs.GetInt("MaxLevelUnlocked", 0) < 1)
                {
                    PlayerPrefs.SetInt("MaxLevelUnlocked", 1);
                    PlayerPrefs.Save();
                }

                if (_tutorialOverlay != null) _tutorialOverlay.style.display = DisplayStyle.None;
                if (_outroOverlay != null) _outroOverlay.style.display = DisplayStyle.Flex;
                Time.timeScale = 0f;
                break;
        }
    }
    #endregion

    #region Event Handlers
    private void OnPlayerMoved(GridDirection direction)
    {
        if (currentState == TutorialState.MoveTowers)
        {
            _hasMoved = true;
        }
    }

    private void OnIntroStartClicked()
    {
        Time.timeScale = 1f;
        if (_introOverlay != null) _introOverlay.style.display = DisplayStyle.None;
        if (_tutorialOverlay != null) _tutorialOverlay.style.display = DisplayStyle.Flex;

        AdvanceState(TutorialState.BuyTowers);
    }

    private void OnOutroMenuClicked()
    {
        Time.timeScale = 1f;
        LoadSceneWithFade("Main Menu");
    }

    private void OnOutroNextClicked()
    {
        Time.timeScale = 1f;
        LoadSceneWithFade("LevelPrueba");
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
    #endregion

    #region UI Helpers
    private void SetTargetButton(Button btn, bool urgent = false)
    {
        _activeTargetButton = btn;
        _isGlowUrgent = urgent;

        if (btn == null)
        {
            _spotlightBox.style.display = DisplayStyle.None;
            _dimTop.style.display = DisplayStyle.None;
            _dimBottom.style.display = DisplayStyle.None;
            _dimLeft.style.display = DisplayStyle.None;
            _dimRight.style.display = DisplayStyle.None;
        }
        else
        {
            _spotlightBox.style.display = DisplayStyle.Flex;
            _dimTop.style.display = DisplayStyle.Flex;
            _dimBottom.style.display = DisplayStyle.Flex;
            _dimLeft.style.display = DisplayStyle.Flex;
            _dimRight.style.display = DisplayStyle.Flex;
        }
    }

    private void UpdateSpotlight()
    {
        float t = Mathf.PingPong(Time.unscaledTime * 2.5f, 1f);
        float scale = Mathf.Lerp(1.0f, 1.05f, t);

        if (_wasdHint != null && _wasdHint.style.display == DisplayStyle.Flex)
        {
            _wasdHint.style.scale = new StyleScale(new Vector2(scale, scale));
        }

        if (_activeTargetButton == null || _spotlightBox == null) return;

        Rect r = _activeTargetButton.worldBound;

        if (_activeTargetButton.name == "BuyTowerButton")
        {
            r = new Rect(r.x + 23f, r.y + 34f, 203f, 105f);
        }

        _spotlightBox.style.left = r.x;
        _spotlightBox.style.top = r.y;
        _spotlightBox.style.width = r.width;
        _spotlightBox.style.height = r.height;

        _dimTop.style.height = r.y;

        _dimBottom.style.top = r.yMax;

        _dimLeft.style.top = r.y;
        _dimLeft.style.height = r.height;
        _dimLeft.style.width = r.x;
        _dimLeft.style.left = 0f;

        _dimRight.style.top = r.y;
        _dimRight.style.height = r.height;
        _dimRight.style.left = r.xMax;
        _dimRight.style.right = 0f;

        Color32 baseColor = _isGlowUrgent ? new Color32(241, 196, 15, 255) : new Color32(46, 204, 113, 255);
        Color32 pulseColor = _isGlowUrgent ? new Color32(255, 235, 100, 255) : new Color32(100, 255, 150, 255);
        Color32 currentColor = Color32.Lerp(baseColor, pulseColor, t);
        float borderWidth = Mathf.Lerp(4f, 7f, t);

        _spotlightBox.style.borderTopColor = new StyleColor(currentColor);
        _spotlightBox.style.borderBottomColor = new StyleColor(currentColor);
        _spotlightBox.style.borderLeftColor = new StyleColor(currentColor);
        _spotlightBox.style.borderRightColor = new StyleColor(currentColor);
        _spotlightBox.style.borderTopWidth = borderWidth;
        _spotlightBox.style.borderBottomWidth = borderWidth;
        _spotlightBox.style.borderLeftWidth = borderWidth;
        _spotlightBox.style.borderRightWidth = borderWidth;

        _spotlightBox.style.scale = new StyleScale(new Vector2(scale, scale));

        if (_wasdHint != null && _wasdHint.style.display == DisplayStyle.Flex)
        {
            _wasdHint.style.scale = new StyleScale(new Vector2(scale, scale));
        }
    }

    private void MarkStepDone(Label stepLabel)
    {
        if (stepLabel != null)
        {
            stepLabel.text = stepLabel.text.Replace("[ ]", "[✓]");
            stepLabel.AddToClassList("tutorial-step-done");
        }
    }
    #endregion

    #region Game Logic Helpers
    private bool HasTowerOfLevel(int targetLevel)
    {
        if (_boardManager == null) return false;
        for (int x = 0; x < _boardManager.GridWidth; x++)
        {
            for (int y = 0; y < _boardManager.GridHeight; y++)
            {
                if (_boardManager.Grid.GetLevel(x, y) >= targetLevel)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsTowerInGoodPosition()
    {
        if (_boardManager == null) return true;
        for (int x = 0; x < _boardManager.GridWidth; x++)
        {
            for (int y = 0; y < _boardManager.GridHeight; y++)
            {
                if (_boardManager.Grid.GetLevel(x, y) >= 2)
                {
                    if (x < _boardManager.GridWidth / 2) return true;
                }
            }
        }
        return false;
    }
    #endregion
}
