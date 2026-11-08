using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core._Common;
using Nekki.Vector.Core;
using Nekki.Vector.Core.Camera;
using Nekki.Vector.Core.Controllers;
using Nekki.Vector.Core.Gadgets;
using Nekki.Vector.Core.Location;
using Nekki.Vector.Core.Models;
using Nekki.Vector.GUI.Scenes.Main;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using Nekki.Vector.GUI.Scenes.Main.Screens;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/// <summary>
/// Owns one running level: its location, camera and the win / death / restart flow.
/// Simulation is stepped by <see cref="LevelSceneController"/> at a fixed 60 Hz through <see cref="Render"/>.
/// </summary>
public class LevelMainController
{
    /// <summary>Consecutive failed runs without input before the game pauses itself (player left the game).</summary>
    private const int AfkLossesBeforePause = 3;

    /// <summary>Seconds without input, after <see cref="AfkLossesBeforePause"/> failed runs, before pausing.</summary>
    private const float AfkDelay = 10f;

    private static readonly Color GoldCoinColor = new Color(1f, 0.8117647f, 0.28235295f);

    private static readonly Color BronzeCoinColor = new Color(0.9647059f, 0.2901961f, 0.27450982f);

    private Location _location;

    private LocationCamera _camera;

    private bool _pauseRender;

    private bool _tutorialPause;

    private uint _frameCount;

    private ControllerGadgets _controllerGadgets;

    private int _consecutiveLosses;

    private float _lastInputTime;

    private bool _pauseAfterReload;

    private float _gameplayTimeScale = 1f;

    /// <summary>Seconds left before the pending end-of-run action (restart or results); negative when none.</summary>
    private float _endDelay = -1f;

    private Action _endAction;

    public static LevelMainController current { get; private set; }

    public static bool IsHunterMode { get; private set; }

    public LevelSceneController levelSceneController { get; private set; }

    public Location Location => _location;

    public LocationCamera Camera => _camera;

    public ControllerGadgets controllerGadgets => _controllerGadgets;

    public GameObject LevelObject { get; set; }

    public bool tutorialPause => _tutorialPause;

    public uint FrameCount => _frameCount;

    public bool CanPauseOrReload { get; private set; }

    public bool IsDeath { get; private set; }

    public bool IsWin { get; private set; }

    /// <summary>Gameplay speed: 0 stops, 0.5 is half speed, 1 is normal.</summary>
    public float slowModeFrames
    {
        get => _gameplayTimeScale;
        set
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            _gameplayTimeScale = Mathf.Max(0f, value);
        }
    }

    public bool pauseRender
    {
        get => _pauseRender;
        set
        {
            if (_pauseRender != value)
                FixedRenderInterpolation.ResetHistory();
            _pauseRender = value;
        }
    }

    private ModelHuman UserModel => _location.Models.FirstOrDefault(model => !model.IsBot);

    public static void Init(LevelSceneController levelSceneController)
    {
        IsHunterMode = UserDataManager.RuntimeInfo.IsHunterMode;
        current = new LevelMainController { levelSceneController = levelSceneController };
        current.InitLevel();
        current.pauseRender = true;
    }

    public static void Clear()
    {
        current = null;
    }

    private void InitLevel()
    {
        _lastInputTime = Time.unscaledTime;
        _location = new Location(VectorPaths.XmlLevels, CurrentTrackInfo.Current.LocationFile);
        foreach (var data in _location.UserData)
        {
            _location.CreateModelHuman(data);
        }
        InitCoins();
        _camera = new LocationCamera();
        _camera.Init();
        _camera.Layers(_location.Sets);
        _camera.Zooming(LocationCamera.CurrentZoom, true);
        _camera.Update();
        _controllerGadgets = new ControllerGadgets();
        SoundsManager.Instance.PlayBackground(_location.Music);
        _location.Start();
        CanPauseOrReload = true;
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    /// <summary>Enables this run's coins and upgrades a few of them to gold / bronze coins.</summary>
    private void InitCoins()
    {
        var coins = _location.GetAllCoins();
        CoinSetter.SetCoins(coins, _location.Sets.TotalCoins);

        var coinsSystem = LocationManager.Instance.coinsSystem;
        var candidates = coins.Where(coin => coin.enabled).ToList();
        UpgradeCoins(candidates, coinsSystem.GetCoinsSettings(CoinsSystemType.CST_DEFAULT, CoinType.CT_GOLD), GoldCoinColor);
        UpgradeCoins(candidates, coinsSystem.GetCoinsSettings(CoinsSystemType.CST_DEFAULT, CoinType.CT_BRONZE), BronzeCoinColor);
    }

    private static void UpgradeCoins(List<CoinRunner> candidates, CoinsSettings settings, Color color)
    {
        for (int i = 0; i < settings.count && candidates.Count > 0; i++)
        {
            if (Random.value >= settings.chance)
            {
                continue;
            }
            var index = Random.Range(0, candidates.Count);
            var coin = candidates[index];
            coin.color = color;
            coin.score = settings.nominal;
            candidates.RemoveAt(index);
        }
    }

    /// <summary>One fixed simulation step.</summary>
    public void Render()
    {
        if (!_pauseRender && !_tutorialPause)
        {
            _camera.UpdatePosition();
            _location.Render();
            _frameCount++;
        }
    }

    /// <summary>Advances the pending end-of-run countdown; called every frame while the game is not paused.</summary>
    public void Tick(float deltaTime)
    {
        CheckAfkPause();

        if (_endDelay < 0f)
        {
            return;
        }
        _endDelay -= deltaTime;
        if (_endDelay <= 0f)
        {
            _endDelay = -1f;
            var action = _endAction ?? Reload;
            _endAction = null;
            action();
        }
    }

    public void HandleNewInput(KeyVariables key)
    {
        _lastInputTime = Time.unscaledTime;
        UserModel?.ControllerKeys.SetKeyVariable(key);
    }

    private void CheckAfkPause()
    {
        if (_pauseAfterReload || _consecutiveLosses < AfkLossesBeforePause)
            return;
        if (Time.unscaledTime - _lastInputTime >= AfkDelay)
            _pauseAfterReload = true;
    }

    public void TutorialAreaActivate(TutorialAreaRunner area)
    {
        CoroutineRunner.Instance.StartCoroutine(RampToSlowTime(0.25f, 0));
        levelSceneController.ShowTutorialUIController(area.Key, area.Description);
    }

    public void TutorialLockGame()
    {
        FixedRenderInterpolation.ResetHistory();
        _tutorialPause = true;
    }

    public void TutorialUnLockGame()
    {
        FixedRenderInterpolation.ResetHistory();
        levelSceneController.HideTutorialUIController();
        _tutorialPause = false;
        slowModeFrames = 1;
    }

    public bool HasCurrentLocationEverBeenCompleted()
    {
        var info = UserDataManager.Instance.CurrentBalanceLocation
            .CurrentStoryModeStoryInfos[UserDataManager.RuntimeInfo.CurrentStory];
        var stats = UserDataManager.Instance.GameStats.GetTrackStats(info.Name);
        return stats != null && stats.Stars > 0;
    }

    private IEnumerator RampToSlowTime(float rampDuration, float slowModeValue)
    {
        float start = slowModeFrames;
        float lastWritten = start;
        float elapsed = 0f;

        while (elapsed < rampDuration)
        {
            if (!pauseRender)
            {
                // Something else changed the speed meanwhile: it wins.
                if (slowModeFrames != lastWritten)
                    yield break;

                elapsed += Time.deltaTime;
                slowModeFrames = Mathf.Lerp(start, slowModeValue, Mathf.Clamp01(elapsed / rampDuration));
                lastWritten = slowModeFrames;
            }
            yield return null;
        }
    }

    public void RefreshTricks()
    {
        _location.RefreshTricks();
    }

    public void Win(ModelHuman modelHuman, float time)
    {
        if (IsWin || IsDeath)
        {
            return;
        }
        modelHuman.IsGadget = false;
        IsWin = true;
        CanPauseOrReload = false;
        UserDataManager.Instance.Stats.Add(modelHuman.controllerStatistics);
        UserDataManager.Instance.SaveUserDate();
        LevelResult.LastLevelResult = new LevelResult(_location, _frameCount);
        EndRunAfter(time, () => CoroutineRunner.Instance.Run(GoToWinScene()));
    }

    private IEnumerator GoToWinScene()
    {
        var storyInfo = UserDataManager.Instance.CurrentBalanceLocation
            .CurrentStoryModeStoryInfos[UserDataManager.RuntimeInfo.CurrentStory];

        MainScene.Current.ModuleSwitcher.Switch(() =>
        {
            MainScene.Current.GetScreen<Wallet>().Activate();
            MainScene.Current.SwitchScreen<PostGameplayScreen>(false);
            MainScene.Current.RunController.gameObject.SetActive(false);
        });

        if (!string.IsNullOrEmpty(storyInfo.CutsceneEnd))
        {
            yield return null;
            yield return MainScene.Current.GetDialog<VideoDialog>().Show(storyInfo.CutsceneEnd);
        }
    }

    public void Murder(ModelHuman modelHuman)
    {
        if (BeginLoss(modelHuman))
        {
            modelHuman.Death(GameEndType.GE_MURDER);
            EndRunAfter(0.5f, Reload);
        }
    }

    public void Death(ModelHuman modelHuman, float time)
    {
        if (!modelHuman.IsBot && BeginLoss(modelHuman))
        {
            modelHuman.Death(GameEndType.GE_DEATH);
            modelHuman.StartPhysics();
            EndRunAfter(time, Reload);
        }
    }

    public void Loss(ModelHuman modelHuman, float time)
    {
        if (BeginLoss(modelHuman))
        {
            LocationCamera.Current.Stop();
            modelHuman.Death(GameEndType.GE_LOSS);
            EndRunAfter(time, Reload);
        }
    }

    public void Arrest(ModelHuman modelHuman)
    {
        if (BeginLoss(modelHuman))
        {
            EndRunAfter(1f, Reload);
        }
    }

    /// <summary>Marks the run as lost; false when the run already ended.</summary>
    private bool BeginLoss(ModelHuman modelHuman)
    {
        if (IsWin || IsDeath)
        {
            return false;
        }
        _consecutiveLosses++;
        CanPauseOrReload = false;
        modelHuman.IsGadget = false;
        IsDeath = true;
        return true;
    }

    private void EndRunAfter(float seconds, Action action)
    {
        _endDelay = Mathf.Max(0f, seconds);
        _endAction = action;
    }

    public void ClearScene()
    {
        if (LevelObject != null)
        {
            Object.Destroy(LevelObject);
        }
        ResourceManager.ClearTextureCache();
        AtlasDecoder.ClearCache();
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    public void ReloadButton()
    {
        Reload();
    }

    private void Reload()
    {
        _endDelay = -1f; // a manual restart during the end delay cancels the automatic one
        _endAction = null;
        FixedRenderInterpolation.ResetHistory();
        _location.Reload();
        TutorialUnLockGame();
        InitCoins();
        _camera.Reset();
        _camera.Zooming(LocationCamera.CurrentZoom, true);
        _location.Start();
        _camera.Update();
        _pauseRender = false;
        IsWin = false;
        IsDeath = false;
        slowModeFrames = 1;
        CanPauseOrReload = true;
        _frameCount = 0;

        if (_pauseAfterReload)
        {
            _pauseAfterReload = false;
            _consecutiveLosses = 0;
            MainScene.Current.GetScreen<Wallet>().Activate();
            MainScene.Current.SwitchScreen<GameplayPauseScreen>(false);
            pauseRender = true;
        }
    }

    public List<ModelHuman> GetModelsByNames(List<string> modelHumanNames)
    {
        var list = new List<ModelHuman>();
        foreach (var modelName in modelHumanNames)
        {
            var model = GetModelByName(modelName);
            if (model != null)
                list.Add(model);
        }
        return list;
    }

    public ModelHuman GetModelByName(string name)
    {
        return _location.GetModelByName(name);
    }
}
