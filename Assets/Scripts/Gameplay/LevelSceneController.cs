using Nekki.Vector.Core.Controllers;
using Nekki.Vector.Core.Location;
using Nekki.Vector.Core.Trigger.Actions;
using Nekki.Vector.GUI.InputControllers;
using Nekki.Vector.GUI.Scenes.Main;
using Nekki.Vector.GUI.Scenes.Main.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Key = Nekki.Vector.Core.Controllers.Key;

/// <summary>
/// Scene-side host of a running level (the "RunController" object in Main): it is enabled when a
/// level starts and disabled when the player leaves. Feeds input, steps the fixed 60 Hz simulation
/// and drives render interpolation and the in-run HUD widgets.
/// </summary>
public class LevelSceneController : MonoBehaviour
{
    [SerializeField]
    private bool _renderInterpolation = true;

    [SerializeField]
    private TutorialUIController _tutorialUIController;

    [SerializeField]
    private TouchController _touchController;

    [SerializeField]
    private BotIcon _botIcon;

    [SerializeField]
    private TrickDescription _trickDescription;

    [SerializeField]
    private MessageOnScreen _messageOnScreen;

    private readonly GameplayClock _gameplayClock = new GameplayClock();

    private bool _canRender;

    private PlayerInputActions.GameplayActions Gameplay => InputController.instance.InputActions.Gameplay;

    private void Awake()
    {
        FixedRenderInterpolation.Clear();
        HideTutorialUIController();
    }

    private void OnEnable()
    {
        Load();
        _touchController.OnSlide += OnSlide;
        RenderPipelineManager.beginCameraRendering += BeginCameraRender;
        RenderPipelineManager.endCameraRendering += EndCameraRender;
        FixedRenderInterpolation.IsRunning = CanSimulate;
        FixedRenderInterpolation.AlphaProvider = () => _gameplayClock.Alpha;

        Gameplay.Up.performed += OnUp;
        Gameplay.Down.performed += OnDown;
        Gameplay.Left.performed += OnLeft;
        Gameplay.Right.performed += OnRight;
        Gameplay.Enable();
    }

    private void OnDisable()
    {
        Gameplay.Up.performed -= OnUp;
        Gameplay.Down.performed -= OnDown;
        Gameplay.Left.performed -= OnLeft;
        Gameplay.Right.performed -= OnRight;

        RenderPipelineManager.beginCameraRendering -= BeginCameraRender;
        RenderPipelineManager.endCameraRendering -= EndCameraRender;
        _touchController.OnSlide -= OnSlide;
        FixedRenderInterpolation.IsRunning = () => false;
        FixedRenderInterpolation.AlphaProvider = null;
        FixedRenderInterpolation.Clear();
        _gameplayClock.Reset();
        _canRender = false;

        RunnerRender.Reset();
        LevelMainController.current?.ClearScene();
        LevelMainController.Clear();
    }

    private void Load()
    {
        LevelMainController.Init(this);
        _botIcon.Init(LevelMainController.current.Location.GetAllBotModels());
        _canRender = true;
    }

    private void OnUp(InputAction.CallbackContext _) => HandleNewInput(Key.Up);

    private void OnDown(InputAction.CallbackContext _) => HandleNewInput(Key.Down);

    private void OnLeft(InputAction.CallbackContext _) => HandleNewInput(Key.Left);

    private void OnRight(InputAction.CallbackContext _) => HandleNewInput(Key.Right);

    private void OnSlide(int _, Vector2 from, Vector2 to)
    {
        var recognizer = new SwipeGestureRecognizer(from, to);
        HandleNewInput(KeyMapping.MapFromSwipe(recognizer.Direction));
    }

    private static void HandleNewInput(Key key)
    {
        if (key != Key.None)
        {
            LevelMainController.current?.HandleNewInput(new KeyVariables(key));
        }
    }

    private void Update()
    {
        var level = LevelMainController.current;
        if (level == null || !_canRender)
        {
            return;
        }

        // End-of-run delays (restart, results) only count down while the game is not paused.
        if (!level.pauseRender)
        {
            level.Tick(Time.deltaTime);
        }

        if (!CanSimulate())
        {
            _gameplayClock.Reset();
            return;
        }

        int ticks = _gameplayClock.Advance(Time.deltaTime, level.slowModeFrames, Time.fixedDeltaTime);
        for (int i = 0; i < ticks; i++)
        {
            if (_renderInterpolation)
            {
                FixedRenderInterpolation.BeginTick();
            }
            level.Render();
            if (_renderInterpolation)
            {
                FixedRenderInterpolation.EndTick();
            }
            if (!CanSimulate() || level.slowModeFrames == 0f)
                break;
        }
    }

    private bool CanSimulate()
    {
        var level = LevelMainController.current;
        return _canRender && level != null && !level.pauseRender && !level.tutorialPause;
    }

    private void BeginCameraRender(ScriptableRenderContext context, Camera camera)
    {
        if (!_canRender || camera != Camera.main)
            return;
        FixedRenderInterpolation.ApplyTransforms(FixedRenderInterpolation.Alpha);
        _botIcon.Render();
    }

    private void EndCameraRender(ScriptableRenderContext context, Camera camera)
    {
        if (camera == Camera.main)
            FixedRenderInterpolation.RestoreTransforms();
    }

    public void ShowTutorialUIController(KeyVariables key, string description)
    {
        _tutorialUIController.gameObject.SetActive(true);
        _tutorialUIController.ShowKey(key, description);
    }

    public void HideTutorialUIController()
    {
        _tutorialUIController.Reset();
        _tutorialUIController.gameObject.SetActive(false);
    }

    public void ShowActivatedTrick(TrickAreaRunner current)
    {
        _trickDescription.Show(current.itemName, current.score.ToString());
    }

    public void TrickNotBuy(TrickAreaRunner current)
    {
        _trickDescription.ShowTrickNotBuy();
    }

    public void MessageOnScreen(string text, int timeInFrame, Color color, TA_MessageOnScreen.Animation appearAnimation, TA_MessageOnScreen.Animation disappearAnimation)
    {
        _messageOnScreen.Show(text, timeInFrame, color, appearAnimation, disappearAnimation);
    }

    // Leaving the game (alt-tab, minimise, phone app switch) opens the pause menu.
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PauseGame();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        var level = LevelMainController.current;
        if (!_canRender || level == null || !level.CanPauseOrReload || level.pauseRender || level.tutorialPause)
        {
            return;
        }
        MainScene.Current.GetScreen<Wallet>().Activate();
        MainScene.Current.SwitchScreen<GameplayPauseScreen>(false);
        level.pauseRender = true;
    }
}
