using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class OptionsScreen : UIModule
    {
        [SerializeField] private Slider MusicSlider;

        [SerializeField] private Slider SoundSlider;

        [SerializeField] private Toggle MusicToggle;

        [SerializeField] private Toggle SoundToggle;

        [SerializeField] Text ResolutionText;

        [SerializeField] Text FullScreenText;

        [SerializeField] Text VsyncText;

        protected override void OnActivated()
        {
            base.OnActivated();

            ResolutionText.text = ResolutionManager.Instance.CurrentResolution.width + "x" +
                                  ResolutionManager.Instance.CurrentResolution.height;
            FullScreenText.text = ResolutionManager.Instance.GetFullScreenModeName();
            VsyncText.text = QualitySettings.vSyncCount == 0 ? "Off" : "On";

            bool music = UserDataManager.Instance.Options.Music;
            bool sound = UserDataManager.Instance.Options.Sound;
            MusicToggle.isOn = music;
            SoundToggle.isOn = sound;

            MusicSlider.maxValue = music ? 1 : 0;
            SoundSlider.maxValue = sound ? 1 : 0;
            MusicSlider.SetValueWithoutNotify(UserDataManager.Instance.Options.MusicLevel);
            SoundSlider.SetValueWithoutNotify(UserDataManager.Instance.Options.SoundLevel);

            SoundsManager.Instance.PlaySounds(SoundType.ui_window_options);
        }

        public void OnBackTap()
        {
            Scene<MainScene>.Current.GoToPreviousScreen();
        }

        public void OnLanguageTap()
        {
            LocalizationManager.Instance.ChangeLocale(LocalizationManager.Instance.GetNextLocale());
            UserDataManager.Instance.Options.Locale = LocalizationManager.Instance.CurrentLocale.index;
            UserDataManager.Instance.Options.SaveData();
        }

        public void OnResolutionTap()
        {
            ResolutionManager.Instance.ChangeResolution(ResolutionManager.Instance.GetNextResolution());
            ResolutionText.text = ResolutionManager.Instance.CurrentResolution.width + "x" +
                                  ResolutionManager.Instance.CurrentResolution.height;
        }

        public void OnFullScreenTap()
        {
            ResolutionManager.Instance.ChangeFullScreenMode(ResolutionManager.Instance.GetNextFullScreenMode());
            FullScreenText.text = ResolutionManager.Instance.GetFullScreenModeName();
        }

        public void OnVsyncTap()
        {
            QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
            VsyncText.text = QualitySettings.vSyncCount == 0 ? "Off" : "On";
            PlayerPrefs.SetInt("VSyncCount", QualitySettings.vSyncCount);
        }

        public void OnMusicTap(bool isOn)
        {
            UserDataManager.Instance.Options.SetMusic(isOn);
            MusicSlider.maxValue = isOn ? 1 : 0;
            MusicSlider.SetValueWithoutNotify(UserDataManager.Instance.Options.MusicLevel);
        }

        public void OnSoundTap(bool isOn)
        {
            UserDataManager.Instance.Options.SetSound(isOn);
            SoundSlider.maxValue = isOn ? 1 : 0;
            SoundSlider.SetValueWithoutNotify(UserDataManager.Instance.Options.SoundLevel);
        }

        public void OnMusicSliderValueChanged(float value)
        {
            if (!UserDataManager.Instance.Options.Music)
                return;
            
            UserDataManager.Instance.Options.MusicLevel = value;
            UserDataManager.Instance.Options.LastUsedSetMusicValue = value;
            UserDataManager.Instance.Options.Update();
        }

        public void OnSoundSliderValueChanged(float value)
        {
            if (!UserDataManager.Instance.Options.Sound)
                return;
            
            UserDataManager.Instance.Options.SoundLevel = value;
            UserDataManager.Instance.Options.LastUsedSetSoundValue = value;
            UserDataManager.Instance.Options.Update();
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }
    }
}