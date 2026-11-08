using Nekki.Vector.GUI.Scenes.Main.Screens;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class QuitStoryDialog : BaseDialog
    {
        public void OnQuitTap()
        {
            Dismiss();
            MainScene.Current.ClearHistoryUntil<SelectStoryScreen>();
            MainScene.Current.ModuleSwitcher.Switch(() =>
            {
                MainScene.Current.RunController.gameObject.SetActive(false);
                MainScene.Current.GoToPreviousScreen(false);
            });
            SoundsManager.Instance.PlayBackground(MusicType.menu, 1.5f);
        }

        public void OnCancelTap()
        {
            Dismiss();
        }
    }
}