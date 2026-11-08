using System.Collections.Generic;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class StoreScreen : UIModule
    {
        [SerializeField] ScrollSnap _scroll;

        [SerializeField] LocalizationText _title;

        [SerializeField] HolderItem _itemPrefab;

        [SerializeField] private GameObject _itemDummyPrefab;

        List<HolderItem> _items = new List<HolderItem>();

        private int _dummyCount = 10;

        string Title
        {
            get
            {
                switch (CurrentStoreType)
                {
                    case StoreItemType.Gadgets:
                        return "%store_gadgets_but%";
                    case StoreItemType.Gear:
                        return "%store_gear%";
                    case StoreItemType.Tricks:
                        return "%store_tricks%";
                }

                return "Store";
            }
        }

        private StoreItemType _CurrentStoreType = StoreItemType.Gear;

        public StoreItemType CurrentStoreType
        {
            get { return _CurrentStoreType; }
            set
            {
                _CurrentStoreType = value;
                if (_IsInited)
                {
                    InitItems();
                }
            }
        }

        public bool CanEquip => CurrentStoreType == StoreItemType.Gadgets || CurrentStoreType == StoreItemType.Gear;

        public bool CanMultiBuy => CurrentStoreType == StoreItemType.Gadgets;

        HolderItem CurrentItem
        {
            get { return _scroll.CurrentObject.GetComponent<HolderItem>(); }
        }

        protected override void Init()
        {
            base.Init();
            InsertDummies(_dummyCount, _dummyCount);
            InitItems();

            _scroll.SnapEvent += i =>
            {
                var go = CurrentItem.Button.gameObject;
                ;
                SetSelectedGameObject(go);
                _SelectedObjectOnActivation = go;

                switch (CurrentStoreType)
                {
                    case StoreItemType.Tricks:
                        UserDataManager.RuntimeInfo.LastSelectedTrick = i;
                        break;
                    case StoreItemType.Gear:
                        UserDataManager.RuntimeInfo.LastSelectedGear = i;
                        break;
                }
            };
        }

        public void InitItems()
        {
            _title.SetText(Title);
            var items = StoreManager.Instance.GetItems(CurrentStoreType);
            int index = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                index = i;

                var count = UserDataManager.Instance.ShopData.GetCount(item.Id);
                var isBought = UserDataManager.Instance.ShopData.IsBought(item.Id);

                HolderItem holder = null;

                if (_items.Count <= i)
                {
                    holder = Instantiate(_itemPrefab, _scroll._content);
                    holder.transform.SetSiblingIndex(_dummyCount + _items.Count);
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
                        _scroll.Snap(moveto, false);
                        return;
                    }

                    if (!CanMultiBuy && isBought)
                    {
                        return;
                    }

                    Buy(item, holder.Icon.sprite);
                });

                holder.Set(LocalizationManager.Instance.GetTranslationByID(item.Id), false, count > 0, isBought,
                    item.Price, item.IconId);
                var equipText = UserDataManager.Instance.ShopData.IsEquipped(item.Id)
                    ? "store_unequip_but"
                    : "store_equip_but";
                holder.EquipButtonText.text = LocalizationManager.Instance.GetTranslation(equipText);
                if (isBought)
                    holder.EquipButton.gameObject.SetActive(CanEquip);
                else
                {
                    holder.EquipButton.gameObject.SetActive(false);
                }

                holder.ItemShopView.IconCheck.SetActive(UserDataManager.Instance.ShopData.IsEquipped(item.Id));
                holder.EquipButton.onClick.RemoveAllListeners();

                if (CanEquip)
                {
                    holder.EquipButton.onClick.AddListener(() =>
                    {
                        var shopdata = UserDataManager.Instance.ShopData;
                        var equipped = shopdata.IsEquipped(item.Id);
                        if (equipped)
                        {
                            shopdata.Unequip(item.Id);
                        }
                        else
                        {
                            shopdata.Equip(item.Id);
                        }

                        equipped = shopdata.IsEquipped(item.Id);

                        var equipText1 = equipped ? "store_unequip_but" : "store_equip_but";
                        holder.EquipButtonText.text = LocalizationManager.Instance.GetTranslation(equipText1);

                        holder.ItemShopView.IconCheck.SetActive(equipped);
                        UserDataManager.Instance.SaveUserDate();

                    });
                }

                if (CanMultiBuy && isBought)
                {
                    holder.SetCount((uint)count);
                }
                else
                {
                    holder.SetCount(0);
                }
            }

            if (_items.Count >= index)
            {
                for (int i = index + 1; i < _items.Count; i++)
                {
                    _items[i].gameObject.SetActive(false);
                }
            }
        }

        void InsertDummies(int left, int right)
        {
            for (int i = 0; i < left; i++)
            {
                var item = Instantiate(_itemDummyPrefab, _scroll._content);
            }
            for (int i = 0; i < right; i++)
            {
                var item = Instantiate(_itemDummyPrefab, _scroll._content);
            }
        }

        public void OnBuyTap()
        {
            var item = CurrentItem;
            if (item == null)
                return;

            item.Button.onClick.Invoke();
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


            Scene<MainScene>.Current.GetDialog<BuyItemDialog>()
                .Show(sprite, item, CanEquip, CanMultiBuy, InitItems, desc);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            _scroll.Recalculate();

            int index = CurrentStoreType switch
            {
                StoreItemType.Tricks => UserDataManager.RuntimeInfo.LastSelectedTrick,
                StoreItemType.Gear => UserDataManager.RuntimeInfo.LastSelectedGear,
                _ => 0
            };

            _scroll.Snap(index, true);
        }

        public void OnBackTap()
        {
            Scene<MainScene>.Current.GoToPreviousScreen();
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }

        protected override void XPress(InputAction.CallbackContext ctx = default)
        {
            var item = CurrentItem;
            if (item == null)
                return;

            item.EquipButton.onClick.Invoke();
        }
    }
}