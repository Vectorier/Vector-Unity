using System.Linq;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class ChooseModeScreen : UIModule
    {
        [SerializeField] private ModeItemView _classicMode;

        [SerializeField] private ModeItemView _hunterMode;

        protected override void OnActivated()
        {
            base.OnActivated();
            _hunterMode.Lock.SetActive(!isHunterModeAvailible());
        }

        public void OnClassicTap()
        {
            UserDataManager.RuntimeInfo.IsHunterMode = false;
            UserDataManager.Instance.SetClassicMode();
            Scene<MainScene>.Current.SwitchScreen<SelectLocationScreen>();
        }

        public void OnHunterTap()
        {
            if (isHunterModeAvailible())
            {
                UserDataManager.RuntimeInfo.IsHunterMode = true;
                UserDataManager.Instance.SetHunterMode();
                Scene<MainScene>.Current.SwitchScreen<SelectLocationScreen>();
            }
            else
            {
                if (LocationManager.Instance.locations.Count == 0)
                {
                    return;
                }
                var loc =  LocationManager.Instance.locations.Values.First()[LocationModeType.Hunter];
                if (loc == null || loc.UnlockInfo == null ||
                    loc.UnlockInfo.StarsLocationUnlockInfo == null)
                {
                    return;
                }

                var stars = loc.UnlockInfo.StarsLocationUnlockInfo.Stars;
                Scene<MainScene>.Current.GetDialog<HunterUnlockDialog>().Show(stars);
            }
        }

        public void OnBackTap()
        {
            Scene<MainScene>.Current.GoToPreviousScreen();
        }

        private bool isHunterModeAvailible()
        {
            var locations = LocationManager.Instance.locations;

            if (locations.Count == 0)
                return false;

            var firstLocation = locations.Values.First();
            
            var hunterLoc =  firstLocation[LocationModeType.Hunter];

            if (hunterLoc == null || hunterLoc.UnlockInfo == null ||
                hunterLoc.UnlockInfo.StarsLocationUnlockInfo == null)
            {
                return true;
            }

            return UserDataManager.Instance.GameStats.GetLocationsStars(LocationModeType.Classic) >=
                   hunterLoc.UnlockInfo
                       .StarsLocationUnlockInfo.Stars;
        }
        
        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }
    }
}