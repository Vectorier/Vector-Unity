using System;
using System.Collections;
using Nekki.Vector.GUI.Scenes.Main;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game
{
    private const string PaidApplicationIDAndroid = "com.nekki.vector.paid";

    private const string PaidApplicationIDIOS = "com.nekki.vector.iphone";

    public static bool Loaded = false;
    
    private static Game _instance;

    private bool _isInited;

    private bool _needShowRefreshApplication;

    public class SnailSettings
    {
        public bool ShowUI = true;

        public string SnailLevel;

        public bool HunterMode = false;

        public bool ShowPlatforms = false;

        public bool ShowAreas = false;

        public bool ShowTriggers = false;

        public bool ShowDetectors = false;

        public bool UsePrefab = false;
    }

    public bool Snail = false;

    public SnailSettings SnailSett = new SnailSettings();

    public static bool IsPaidID => true;

    public static Game Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Game();
            }
            return _instance;
        }
    }

    public static bool IsTrickBought(string trickId)
    {
        return UserDataManager.Instance.ShopData.IsBought(trickId);
    }

    public static bool IsItemBought(string itemId)
    {
        return UserDataManager.Instance.ShopData.IsBought(itemId);
    }

    public static bool IsItemEquipped(string itemId)
    {
        return UserDataManager.Instance.ShopData.IsEquipped(itemId);
    }

    public static bool IsHunterMode()
    {
        return UserDataManager.RuntimeInfo.IsHunterMode;
    }

    public void ShowRefreshApplication()
    {
        if (_isInited)
        {
            _needShowRefreshApplication = false;
            var onPlay = new Action(() =>
            {
                UserDataManager.Instance.MainData.Unlock();
                UserDataManager.Instance.SaveUserDate();
                Utils.Utils.QuitGame();
            });
            ShowRefreshApplication(onPlay);
        }
    }

    private void ShowRefreshApplication(Action onPlay)
    {
        var title = LocalizationManager.Instance.GetTranslation("terms_of_use_title");
        var message = LocalizationManager.Instance.GetTranslation("buy_premium_desc");
        var ok = LocalizationManager.Instance.GetTranslation("ctrl_ok");
        var onClick = new Action<bool>(result =>
        {
            if (result)
                onPlay.Invoke();
        });

        MainScene.Current.GetDialog<AlertDialog>().Show(title, message, ok, null, onClick);
    }
}
