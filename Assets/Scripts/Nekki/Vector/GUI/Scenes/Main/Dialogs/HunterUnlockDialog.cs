using UI.Dialogs;
using UnityEngine;
using UnityEngine.UI;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class HunterUnlockDialog : BaseDialog
    {
        [SerializeField] private Text _stars;

        public void Show(int stars)
        {
            base.Show();
            _stars.text = stars.ToString();
        }
        
        public void OnOkTap()
        {
            Dismiss();
        }
    }
}