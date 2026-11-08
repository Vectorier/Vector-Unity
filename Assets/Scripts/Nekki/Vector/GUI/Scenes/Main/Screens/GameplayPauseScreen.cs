using System.Collections.Generic;
using System.Linq;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class GameplayPauseScreen : UIModule
    {
        [SerializeField] ScrollSnap _scrollSnap;
        
        [SerializeField] Text _title;
        
        [SerializeField] private GameObject _gadgetIcon;

        [SerializeField] private Toggle _musicToggle;
        
        [SerializeField] private Toggle _soundToggle;

        [SerializeField] private HolderItem _holderItemPrefab;

        List<HolderItem> _items = new();

        private GameObject _firstObjectForSelection;
        
        private bool _tricksSelected = true;

        HolderItem CurrentItem
        {
            get
            {
                return _scrollSnap.CurrentObject.GetComponent<HolderItem>();
            }
        }
        
        protected override void Init()
        {
            base.Init();
            _firstObjectForSelection = _SelectedObjectOnActivation;
            _scrollSnap.SnapEvent += i =>
            {
                if (_tricksSelected)
                {
                    var go = CurrentItem.Button.gameObject;;
                    SetSelectedGameObject(go);
                    _SelectedObjectOnActivation = go;
                }
            };
        }

        protected override void OnActivated()
        {
            SoundsManager.Instance.PauseMusic();
            SetInputActions(InputController.instance.InputActions);
            InitItems();
            
            _scrollSnap.Recalculate();
            _scrollSnap.Snap(0, true);

            _musicToggle.isOn = UserDataManager.Instance.Options.Music;
            _soundToggle.isOn = UserDataManager.Instance.Options.Sound;
            
            SetState();
        }

        public void InitItems()
        {
            var playerData = UserDataManager.Instance;
            var storyInfo =
                playerData.CurrentBalanceLocation.CurrentStoryModeStoryInfos[UserDataManager.RuntimeInfo.CurrentStory];
            _title.text = LocalizationManager.Instance.GetTranslationByID(storyInfo.Name);
            _gadgetIcon.SetActive(playerData.ShopData.IsEquipped("GADGET_FORCEBLASTER"));
            var tricks = StoreManager.Instance.GetItems(StoreItemType.Tricks)
                .Where(t => storyInfo.TrickIds.Contains(t.Id)).ToList();

            int index = 0;
            for (int i = 0; i < tricks.Count; i++)
            {
                var item = tricks[i];
                index = i;

                var count = UserDataManager.Instance.ShopData.GetCount(item.Id);
                var isBought = UserDataManager.Instance.ShopData.IsBought(item.Id);

                HolderItem holder = null;

                if (_items.Count <= i)
                {
                    holder = Instantiate(_holderItemPrefab, _scrollSnap._content);
                    _items.Add(holder);
                }
                else
                {
                    holder = _items[i];
                    holder.gameObject.SetActive(true);
                }

                CoroutineRunner.Instance.Run(
                    ResourcesLoader.LoadItemSpriteAsync(item.IconId, loaded => holder.Icon.sprite = loaded));

                var moveto = index;
                holder.Button.onClick.RemoveAllListeners();
                holder.Button.onClick.AddListener(() =>
                {
                    var scrollItem = holder.GetComponent<ScrollSnapItem>();
                    if (scrollItem != null && !scrollItem.IsSelected)
                    {
                        _scrollSnap.Snap(moveto, false);
                        return;
                    }

                    if (isBought)
                    {
                        return;
                    }

                    Buy(item, holder.Icon.sprite);
                });

                holder.Set(LocalizationManager.Instance.GetTranslationByID(item.Id), false, count > 0, isBought,
                    item.Price, item.IconId);

                holder.EquipButton.gameObject.SetActive(false);


                holder.ItemShopView.IconCheck.SetActive(isBought);


                holder.SetCount(0);
            }

            if (_items.Count >= index)
            {
                for (int i = index + 1; i < _items.Count; i++)
                {
                    _items[i].gameObject.SetActive(false);
                }
            }

            _scrollSnap.StartIndex = 0;
            _scrollSnap.EndIndex = index;
        }

        private void Buy(Item item, Sprite sprite)
        {
            if (UserDataManager.Instance.MainData.GetCoins() < item.Price)
            {
                Scene<MainScene>.Current.GetDialog<NotEnoughCoinsDialog>().Show(item.Price);
                return;
            }

            UserDataManager.RuntimeInfo.CurentItemId = item.Id;
            UserDataManager.RuntimeInfo.CurentItemType = StoreItemType.Tricks;
            string desc = string.Empty;
            if (item.ItemType == StoreItemType.Gadgets)
            {
                desc = LocalizationManager.Instance.GetTranslation("store_blaster_des");
            }

            Scene<MainScene>.Current.GetDialog<BuyItemDialog>().Show(sprite, item, false, false, () =>
            {
                InitItems();
                LevelMainController.current.RefreshTricks();
            }, desc);
        }

        public void OnSettingsTap()
        {
            MainScene.Current.SwitchScreen<OptionsScreen>();
        }

        public void OnContinueTap()
        {
            MainScene.Current.GetScreen<Wallet>().DeActivate();
            DeActivate();
            MainScene.Current.GetDialog<CountdownDialog>().Show(1.5f, 3, () =>
            {
                MainScene.Current.GoToPreviousScreen(false);
                LevelMainController.current.pauseRender = false;
            });
        }

        public void OnRestartTap()
        {
            MainScene.Current.GetScreen<Wallet>().DeActivate();
            MainScene.Current.GoToPreviousScreen(false);
            LevelMainController.current.ReloadButton();
        }

        public void OnMusicTap(bool isOn)
        {
            UserDataManager.Instance.Options.SetMusic(isOn);
        }

        public void OnSoundTap(bool isOn)
        {
            UserDataManager.Instance.Options.SetSound(isOn);
        }

        public void OnGadgetTap()
        {
            StoreScreen module = Scene<MainScene>.Current.GetScreen<StoreScreen>();
            module.CurrentStoreType = StoreItemType.Gadgets;
            Scene<MainScene>.Current.SwitchScreen<StoreScreen>();
        }

        public void OnMenuTap()
        {
            MainScene.Current.ShowDialog<QuitStoryDialog>();
        }

        protected override void UpPress(InputAction.CallbackContext ctx = default)
        {
            _tricksSelected = true;
            SetState();
        }

        protected override void DownPress(InputAction.CallbackContext ctx = default)
        {
            _tricksSelected = false;
            SetState();
        }

        protected override void XPress(InputAction.CallbackContext ctx = default)
        {
            _tricksSelected = !_tricksSelected;
            SetState();
        }

        protected override void YPress(InputAction.CallbackContext ctx = default)
        {
            OnGadgetTap();
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnContinueTap();
        }

        void SetState()
        {
            if (_tricksSelected)
            {
                _scrollSnap.Enable();
                SetSelectedGameObject(CurrentItem.Button.gameObject);
            }

            else
            {
                _scrollSnap.Disable();
                SetSelectedGameObject(_firstObjectForSelection);
            }
        }
    }
}