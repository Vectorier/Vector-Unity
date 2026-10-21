using UnityEngine.InputSystem;
using UnityEngine.UI;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class StatsScreen : UIModule
    {
        public Text TotalRunningDistance;

        public Text JumpsCount;

        public Text SlidesCount;

        public Text BonusCollected;

        public Text CoinsCollected;

        public Text TricksPerformed;

        public Text TracksPassed;

        public Text TracksPassedWith3Stars;

        public Text Death;

        public Text DeathByHunter;

        public Text HuntersKilled;

        public Text GlassBroken;

        protected override void OnActivated()
        {
            base.OnActivated();
            
            var stats = UserDataManager.Instance.Stats;
            
            TotalRunningDistance.text = string.Format("{0:F}", stats.TotalRunningDistance);
            JumpsCount.text = stats.JumpsCount.ToString();
            SlidesCount.text = stats.SlidesCount.ToString();
            BonusCollected.text = stats.BonusCollected.ToString();
            CoinsCollected.text = stats.CoinsCollected.ToString();
            TricksPerformed.text = stats.TricksPerformed.ToString();
            TracksPassed.text = UserDataManager.Instance.GameStats.GetAllTrackCompleted(0).ToString();
            TracksPassedWith3Stars.text = UserDataManager.Instance.GameStats.GetAllTrackCompleted(3).ToString();
            Death.text = stats.Death.ToString();
            DeathByHunter.text = stats.DeathByHunter.ToString();
            HuntersKilled.text = stats.HuntersKilled.ToString();
            GlassBroken.text = stats.GlassBroken.ToString();
        }

        public void OnBackTap()
        {
            Scene<MainScene>.Current.GoToPreviousScreen();
        }
        
        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }
    }
}