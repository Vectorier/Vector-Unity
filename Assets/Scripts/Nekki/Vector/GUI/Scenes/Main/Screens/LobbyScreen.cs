using System;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class LobbyScreen : UIModule
    {
        protected override void Init()
        {
            base.Init();
        }

        protected override void OnActivated()
        {
            base.OnActivated();
        }

        public void OnPlayTap()
        {
            Scene<MainScene>.Current.SwitchScreen<ChooseModeScreen>();
        }

        public void OnShopTap()
        {
            Scene<MainScene>.Current.SwitchScreen<StoreSelectScreen>();
        }

        public void OnStatsTap()
        {
            Scene<MainScene>.Current.SwitchScreen<StatsScreen>();
        }

        public void OnSettingsTap()
        {
            Scene<MainScene>.Current.SwitchScreen<OptionsScreen>();
        }

        public void OnTikTokTap()
        {
            Application.OpenURL("https://www.tiktok.com/@vector_the_game");
        }

        public void OnFacebookTap()
        {
            Application.OpenURL("https://m.facebook.com/VectorTheGame");
        }

        public void OnTwitterTap()
        {
            Application.OpenURL("https://mobile.twitter.com/VectorTheGame");
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            var title = LocalizationManager.Instance.GetTranslation("msg_quit_tit");
            var message = LocalizationManager.Instance.GetTranslation("msg_quit");
            var ok = LocalizationManager.Instance.GetTranslation("ctrl_ok");
            var cancel = LocalizationManager.Instance.GetTranslation("ctrl_cancel");

            var dialog = MainScene.Current.GetDialog<AlertDialog>();
            dialog.Show(title, message, ok, cancel, (r) =>
            {
                if (r)
                {
                    Utils.Utils.QuitGame();
                }
            });
        }
    }
}