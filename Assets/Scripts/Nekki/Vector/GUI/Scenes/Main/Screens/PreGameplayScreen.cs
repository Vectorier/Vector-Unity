using System.Collections.Generic;
using System.Linq;
using Nekki.Vector.Core;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utils;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class PreGameplayScreen : UIModule
    {
        [SerializeField] private Text _title;

        [SerializeField] private GameObject _gadgetIcon;

        [SerializeField] ScrollSnap _scrollSnap;

        [SerializeField] HolderItem _itemPrefab;

        List<HolderItem> _items = new List<HolderItem>();
        
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
            _scrollSnap.SnapEvent += i =>
            {
                var go = CurrentItem.Button.gameObject;;
                SetSelectedGameObject(go);
                _SelectedObjectOnActivation = go;
            };
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            InitItems();
            
            _scrollSnap.Recalculate();
            _scrollSnap.Snap(0, true);
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
                    holder = Instantiate(_itemPrefab, _scrollSnap._content);
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
                        OnPlayTap();
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
                Scene<MainScene>.Current.GetDialog<BuyCoinsDialog>().Show(item.Price);
                return;
            }

            UserDataManager.RuntimeInfo.CurentItemId = item.Id;
            UserDataManager.RuntimeInfo.CurentItemType = StoreItemType.Tricks;
            string desc = string.Empty;
            if (item.ItemType == StoreItemType.Gadgets)
            {
                desc = LocalizationManager.Instance.GetTranslation("store_blaster_des");
            }
            
            Scene<MainScene>.Current.GetDialog<BuyItemDialog>().Show(sprite, item, false, false, InitItems, desc);
        }

        public void OnGadgetTap()
        {
            StoreScreen module = Scene<MainScene>.Current.GetScreen<StoreScreen>();
            module.CurrentStoreType = StoreItemType.Gadgets;
            Scene<MainScene>.Current.SwitchScreen<StoreScreen>();
        }

        public void OnPlayTap()
        {
            MainScene.Current.GetScreen<Wallet>().DeActivate();
            MainScene.Current.SwitchScreen<LoadingScreen>(false);
        }

        public void OnBackTap()
        {
            MainScene.Current.GoToPreviousScreen();
        }

        protected override void YPress(InputAction.CallbackContext ctx = default)
        {
            OnGadgetTap();
        }

        protected override void XPress(InputAction.CallbackContext ctx = default)
        {
            OnPlayTap();
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }
    }
}