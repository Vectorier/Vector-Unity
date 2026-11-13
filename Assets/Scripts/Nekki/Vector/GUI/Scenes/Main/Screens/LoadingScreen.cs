using System.Collections;
using Nekki.Vector.Core;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class LoadingScreen : UIModule
    {
        /// <summary>Seconds the menu music fades out before the level loads.</summary>
        private const float MusicFadeOutTime = 1f;

        protected override void OnActivated()
        {
            base.OnActivated();

            CoroutineRunner.Instance.Run(Load());
        }

        IEnumerator Load()
        {
            SoundsManager.Instance.FadeOutMusic(MusicFadeOutTime);
            yield return new WaitForSecondsRealtime(MusicFadeOutTime);
            
            var storyInfo = UserDataManager.Instance.CurrentBalanceLocation.CurrentStoryModeStoryInfos[UserDataManager.RuntimeInfo.CurrentStory];
            CurrentTrackInfo.Current.LocationFile = storyInfo.Name.Replace("_HUNTER", "") + ".xml";
            
            MainScene.Current.RunController.gameObject.SetActive(true);
            
            yield return null;

            // The level could not be loaded (the error log shows why): back to the story menu.
            if (LevelSceneController.LoadFailed)
            {
                MainScene.Current.ClearHistoryUntil<SelectStoryScreen>();
                MainScene.Current.ModuleSwitcher.Switch(() =>
                {
                    MainScene.Current.RunController.gameObject.SetActive(false);
                    MainScene.Current.GoToPreviousScreen(false);
                });
                SoundsManager.Instance.PlayBackground(MusicType.menu, 1.5f);
                yield break;
            }
            
            MainScene.Current.SwitchScreen<GameplayScreen>(string.IsNullOrEmpty(storyInfo.CutsceneStart));

            // yield return new WaitForEndOfFrame();
            
            if (!string.IsNullOrEmpty(storyInfo.CutsceneStart))
            {
                yield return MainScene.Current.GetDialog<VideoDialog>().Show(storyInfo.CutsceneStart);
            }
            
            
            yield return null;

            if (LevelMainController.current != null)
                LevelMainController.current.pauseRender = false;
        }
    }
}