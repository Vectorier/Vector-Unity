using Nekki.Vector.Core.Gadgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class GameplayScreen : UIModule
    {
        [SerializeField] private Text _gadgetCount;
        [SerializeField] private GameObject _gadget;

        [SerializeField] Image _gadgetCooldownIcon;

        protected override void Init()
        {
            base.Init();
            UserDataManager.Instance.ShopData.Updated += InventoryOnUpdated;;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            InventoryOnUpdated(UserDataManager.Instance.ShopData);
        }

        public void OnPauseTap()
        {
            Pause();
        }

        public void OnRestartTap()
        {
            Restart();
        }

        public void OnGadgetTap()
        {
            UseGadget();
        }

        void Pause()
        {
            if (!LevelMainController.current.CanPauseOrReload)
            {
                return;
            }

            MainScene.Current.GetScreen<Wallet>().Activate();
            MainScene.Current.SwitchScreen<GameplayPauseScreen>(false);
            LevelMainController.current.pauseRender = true;
        }
        
        void Restart()
        {
            if (!LevelMainController.current.CanPauseOrReload)
            {
                return;
            }

            LevelMainController.current.ReloadButton();
        }

        void UseGadget()
        {
            if (UserDataManager.RuntimeInfo.IsHunterMode)
            {
                return;
            }

            if (!UserDataManager.Instance.ShopData.IsEquippedGadgetNotEmpty())
            {
                return;
            }

            LevelMainController.current.controllerGadgets.ActivateGadget(GadgetType.KillBot);
        }

        private void InventoryOnUpdated(ShopData inventory)
        {
            var equipped = inventory.IsEquipped("GADGET_FORCEBLASTER");
            var count = inventory.GetCount("GADGET_FORCEBLASTER");
            if (count < 1 || !equipped || UserDataManager.RuntimeInfo.IsHunterMode)
            {
                _gadget.SetActive(false);
                return;
            }

            _gadget.SetActive(true);
            _gadgetCount.text = count.ToString();
        }

        public void SetGadgetCooldownProgress(float progress)
        {
            _gadgetCooldownIcon.fillAmount = progress;
        }

        protected override void SetInputActions(PlayerInputActions actions)
        {
            base.SetInputActions(actions);
            actions.Gameplay.Restart.performed += RestartPress;
            actions.Gameplay.Gadget.performed += GadgetPress;
        }

        protected override void DisableInputActions(PlayerInputActions actions)
        {
            base.DisableInputActions(actions);
            actions.Gameplay.Restart.performed -= RestartPress;
            actions.Gameplay.Gadget.performed -= GadgetPress;
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            Pause();
        }
        
        protected void RestartPress(InputAction.CallbackContext ctx = default)
        {
            Restart();
        }

        protected void GadgetPress(InputAction.CallbackContext ctx = default)
        {
            UseGadget();
        }
    }
}