using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class CountdownDialog : BaseDialog
    {
        [SerializeField] private Text _number;

        private Sequence _countdownSequence;

        Action _onComplete;

        public void Show(float time, int countFrom, Action onComplete)
        {
            _onComplete = onComplete;
            _countdownSequence?.Kill();
            base.Show();

            if (countFrom <= 0 || time <= 0f)
            {
                Dismiss();
                onComplete?.Invoke();
                return;
            }

            float interval = time / countFrom;

            _number.text = countFrom.ToString();

            _countdownSequence = DOTween.Sequence();

            for (int i = countFrom - 1; i > 0; i--)
            {
                int number = i;

                _countdownSequence
                    .AppendInterval(interval)
                    .AppendCallback(() => _number.text = number.ToString());
            }

            _countdownSequence
                .AppendInterval(interval)
                .OnComplete(() =>
                {
                    _countdownSequence = null;
                    Dismiss();
                    _onComplete?.Invoke();
                });
        }

        public override void Dismiss()
        {
            _countdownSequence?.Kill();
            _countdownSequence = null;
            
            _onComplete?.Invoke();
            _onComplete = null;
            base.Dismiss();
        }
    }
}