using System;
using UnityEngine;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class GDPRDialog : BaseDialog
    {
        private Action confirm;

        public void Show(Action onConfirm)
        {
            base.Show();
            confirm = onConfirm;
        }

        public void OnOkTap()
        {
            Dismiss();
        }

        public void OnTermsTap()
        {
            Application.OpenURL("https://nekki.com/en/legal/privacy/");
        }

        public override void Dismiss()
        {
            confirm();
            base.Dismiss();
        }
    }
}