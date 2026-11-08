using System;
using UnityEngine;
using UnityEngine.UI;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class BuyLocationDialog : BaseDialog
    {
        [SerializeField] private Text _captionText;

        [SerializeField] private Text _priceText;

        private Action _onDismiss;
        string _locationId;
        private int _price;

        public void Show(string locationId, int price, string captionText, Action onDismiss)
        {
            base.Show();
            
            _onDismiss = onDismiss;
            
            _locationId = locationId;
            _price = price;
            
            _captionText.text = captionText;
            _priceText.text = price.ToString();
        }

        public void OnBuyTap()
        {
            if (UserDataManager.Instance.MainData.GetCoins() < _price)
            {
                Dismiss();
                MainScene.Current.GetDialog<NotEnoughCoinsDialog>().Show(_price);
            }
            else
            {
                UserDataManager.Instance.MainData.AddCoins(-_price);
                UserDataManager.Instance.ShopData.Add(_locationId, 1, false);
                UserDataManager.Instance.SaveUserDate();
                Dismiss();
            }
        }

        public void OnCancelTap()
        {
            Dismiss();
        }

        public override void Dismiss()
        {
            base.Dismiss();
            _onDismiss();
        }
    }
}