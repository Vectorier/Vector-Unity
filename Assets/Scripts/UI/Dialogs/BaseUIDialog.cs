using System.Collections.Generic;
using UI.Common;
using UnityEngine;

namespace UI.Dialogs
{
    public class BaseUIDialog : BaseDialog
    {
        [SerializeField] private RectTransform _TopLineRect,_ButtonPanelRect;
        [SerializeField] private List<ButtonUI> _Buttons=new List<ButtonUI>();
        [SerializeField] private List<Transform> _ButtonsPlaceholders=new List<Transform>();
        [SerializeField] private DialogContent _Content;
        public DialogContent Content=>_Content;
        public T Init<T>(GameObject template) where T:DialogContent
        {
            if(_Content==null && template!=null) _Content=Instantiate(template,transform,false).GetComponent<T>();
            if(_Content==null) { Debug.LogError("BaseUIDialog requires assigned Content or a content template.",this);return null; }
            _Content.Parent=this;UpdateDialogSize();return _Content as T;
        }
        public void RegisterContent(DialogContent content) { _Content=content;if(content!=null) content.Parent=this; }
        public void UpdateDialogSize()
        {
            if(_Content==null) return;var rect=GetComponent<RectTransform>();if(rect==null) return;
            float top=_TopLineRect==null?0:_TopLineRect.sizeDelta.y;float bottom=_ButtonPanelRect==null?0:_ButtonPanelRect.sizeDelta.y;
            _Content.DeltaY=(bottom-top)/2;rect.sizeDelta=new Vector2(rect.sizeDelta.x,_Content.Height+top+bottom);
        }
        public void InitButtons(List<DialogButtonData> buttons)
        {
            if(buttons==null) buttons=new List<DialogButtonData>();
            if(buttons.Count>_Buttons.Count) { Debug.LogError("BaseUIDialog needs enough ButtonUI slots for "+buttons.Count+" buttons.",this);return; }
            for(int i=0;i<_Buttons.Count;i++) if(_Buttons[i]!=null) { _Buttons[i].gameObject.SetActive(i<buttons.Count);if(i<buttons.Count) buttons[i]?.InitButton(_Buttons[i]); }
            RefreshButtonPosition();
        }
        public void RefreshButtonPosition() { int slot=0;foreach(var button in _Buttons) if(button!=null && button.gameObject.activeSelf) { if(slot<_ButtonsPlaceholders.Count && _ButtonsPlaceholders[slot]!=null) button.transform.SetParent(_ButtonsPlaceholders[slot],false);slot++; } }
        public void OnButton1Tap()=>_Content?.OnButton1Tap();
        public void OnButton2Tap()=>_Content?.OnButton2Tap();
        public void OnButton3Tap()=>_Content?.OnButton3Tap();
        public void OnButton4Tap()=>_Content?.OnButton4Tap();
        public void HideButtons() { foreach(var button in _Buttons) if(button!=null) button.gameObject.SetActive(false); }
        public void ShowButtons() { foreach(var button in _Buttons) if(button!=null) button.gameObject.SetActive(true); }
    }
}
