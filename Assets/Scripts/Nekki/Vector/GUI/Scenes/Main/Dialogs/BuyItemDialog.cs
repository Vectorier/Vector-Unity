using System;
using Nekki.Vector.GUI.Scenes.Main.Screens;
using UnityEngine;
using UnityEngine.UI;
using UI;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class BuyItemDialog : BaseDialog
    {
        public Text Caption;

        public Image Icon;

        public Text Price;

        public Text Description;

        public GameObject CounterHolder;

        public Text Counter;
        
        Action OnDismiss;
        
        Item _item;
        
        bool _multiBuy = false;
        
        public void Show(Sprite sprite, Item item, bool canEquip, bool canMultiBuy, Action onDismiss = null, string description = "")
        {
            base.Show();
            
            OnDismiss = onDismiss;
            _item = item;
            _multiBuy = canMultiBuy;
            
            Icon.sprite = sprite;
            Caption.text = LocalizationManager.Instance.GetTranslationByID(item.Id);
            Price.text = item.Price.ToString();
            
            Description.gameObject.SetActive(!string.IsNullOrEmpty(description));
            Description.text = description;

            if (canMultiBuy)
            {
                CounterHolder.SetActive(true);
                Counter.text = UserDataManager.Instance.ShopData.GetCount(item.Id).ToString();
            }
            else
            {
                CounterHolder.SetActive(false);
            }
        }

        public void OnBuyTap()
        {
            if (_item == null)
                return;
            
            var playerData = UserDataManager.Instance;
            
            if (playerData.MainData.GetCoins() < _item.Price)
            {
                Dismiss();
                Scene<MainScene>.Current.GetDialog<BuyCoinsDialog>().Show(_item.Price);
                return;
            }
            
            playerData.MainData.AddCoins(-_item.Price);
            playerData.ShopData.Add(_item.Id, 1, true, false);
            playerData.SaveUserDate();
            SoundsManager.Instance.PlaySounds(SoundType.cash_register);
            if (_multiBuy)
            {
                Counter.text = UserDataManager.Instance.ShopData.GetCount(_item.Id).ToString();
            }
            else
            {
                Dismiss();
            }
        }

        public void OnCancelTap()
        {
            Dismiss();
        }

        public override void Dismiss()
        {
            OnDismiss?.Invoke();
            base.Dismiss();
        }
    }
}