using UnityEngine;
using UnityEngine.UI;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class BuyCoinsDialog : BaseDialog
    {
        [SerializeField] private Text _price;
        
        public void Show(int price)
        {
            base.Show();
            _price.text = price.ToString();
        }

        public void OnCancelTap()
        {
            Dismiss();
        }
    }
}