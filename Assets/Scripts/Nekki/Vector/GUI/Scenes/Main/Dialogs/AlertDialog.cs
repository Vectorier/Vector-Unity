using System;
using UnityEngine;
using UnityEngine.UI;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class AlertDialog : BaseDialog
    {
        [SerializeField] Text  _title;
        [SerializeField] Text _message;
        [SerializeField] Text _buttonOkText;
        [SerializeField] Text _buttonCancelText;
        
        Action<bool> _result;
        
        public void Show(string title, string message, string buttonOkText, string buttonCancelText, Action<bool> result)
        {
            base.Show();
            _title.text = title;
            _message.text = message;
            _buttonOkText.text = buttonOkText;
            _buttonCancelText.text = buttonCancelText;
            
            _result = result;
        }

        public void OnOkTap()
        {
            _result?.Invoke(true);
            base.Dismiss();
        }

        public void OnCancelTap()
        {
            _result?.Invoke(false);
            base.Dismiss();
        }

        public override void Dismiss()
        {
            base.Dismiss();
            _result?.Invoke(false);
        }
    }
}