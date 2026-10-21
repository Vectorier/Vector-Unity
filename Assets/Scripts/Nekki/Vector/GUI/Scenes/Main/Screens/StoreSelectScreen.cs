using UnityEngine.InputSystem;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class StoreSelectScreen : UIModule
    {
        public void OnGadgetsTap()
        {
            StoreScreen module = Scene<MainScene>.Current.GetScreen<StoreScreen>();
            module.CurrentStoreType = StoreItemType.Gadgets;
            Scene<MainScene>.Current.SwitchScreen<StoreScreen>();
        }

        public void OnTricksTap()
        {
            StoreScreen module = Scene<MainScene>.Current.GetScreen<StoreScreen>();
            module.CurrentStoreType = StoreItemType.Tricks;
            Scene<MainScene>.Current.SwitchScreen<StoreScreen>();
        }

        public void OnGearTap()
        {
            StoreScreen module = Scene<MainScene>.Current.GetScreen<StoreScreen>();
            module.CurrentStoreType = StoreItemType.Gear;
            Scene<MainScene>.Current.SwitchScreen<StoreScreen>();
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