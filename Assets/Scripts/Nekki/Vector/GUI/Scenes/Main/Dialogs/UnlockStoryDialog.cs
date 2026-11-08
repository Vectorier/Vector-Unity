using System;
using UnityEngine;
using UnityEngine.UI;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class UnlockStoryDialog : BaseDialog
    {
        [SerializeField] private Text _starsText;
        
        [SerializeField] private Text _priceText;
        
        [SerializeField] private Text _storyNameText;
        
        [SerializeField]  private Text _captionText;

        Action _onDismiss;
        
        private int _price;

        private string _storyId;

        public void Show(int price, int stars, string caption, string storyName, string storyId, Action onDismiss)
        {
            base.Show();
            
            _onDismiss = onDismiss;
            
            _price = price;
            _storyId = storyId;
            
            _priceText.text = price.ToString();
            _starsText.text = stars.ToString();
            _captionText.text = caption;
            _storyNameText.text = storyName;
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
                UserDataManager.Instance.ShopData.Add(_storyId, 1, false);
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