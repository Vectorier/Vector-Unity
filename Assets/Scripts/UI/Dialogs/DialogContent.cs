using System.Collections.Generic;
using UnityEngine;

namespace UI.Dialogs
{
    public class DialogContent : MonoBehaviour
    {
        protected List<DialogButtonData> _Buttons = new List<DialogButtonData>();
        public BaseUIDialog Parent { get; set; }
        public float Height => GetComponent<RectTransform>()?.sizeDelta.y ?? 0;

        public float DeltaY
        {
            set
            {
                var rect = GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, value);
            }
        }

        protected void Init(List<DialogButtonData> buttons)
        {
            _Buttons = buttons ?? new List<DialogButtonData>();
            if (Parent == null) Parent = GetComponentInParent<BaseUIDialog>();
            if (Parent != null) Parent.InitButtons(_Buttons);
            else Debug.LogError("DialogContent requires a parent BaseUIDialog.", this);
        }

        private void Tap(int index)
        {
            if (index < _Buttons.Count) _Buttons[index]?.Activate(Parent);
        }

        public virtual void OnButton1Tap() => Tap(0);
        public virtual void OnButton2Tap() => Tap(1);
        public virtual void OnButton3Tap() => Tap(2);
        public virtual void OnButton4Tap() => Tap(3);
    }
}