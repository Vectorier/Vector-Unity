using System;
using DG.Tweening;
using UnityEngine;
using UI.Common;
using UI.Localization;

namespace UI.Dialogs
{
    public class Tooltip : BaseDialog
    {
        public enum AnimationType { Instant,Fade }
        public enum Pivot { TopLeft,TopRight,BottomLeft,BottomRight }
        public enum State { Inactive,Opening,Active,Closing }
        public class UISettings
        {
            public string Text,Title;
            public RectTransform Parent;
            public Vector2 Shift;
            public Pivot Pivot;
            public AnimationType OpenAnimation,CloseAnimation;
            public bool BlockTouches;
            public DialogQueueType QueueType=DialogQueueType.Notification;
            public Action OnDismiss;
            public DialogButtonData ButtonData;
        }
        [SerializeField] private RectTransform _ContentRoot;
        [SerializeField] private CanvasGroup _CanvasGroup;
        [SerializeField] private LabelAlias _Text,_Title;
        [SerializeField] private ButtonUI _Button;
        private UISettings settings;
        private Tween fade;
        public void Init(UISettings value)
        {
            settings=value ?? new UISettings();Modal=settings.BlockTouches;
            if(_Text!=null) _Text.SetAlias(settings.Text);
            if(_Title!=null) { _Title.gameObject.SetActive(!string.IsNullOrEmpty(settings.Title));_Title.SetAlias(settings.Title); }
            if(_Button!=null) { _Button.gameObject.SetActive(settings.ButtonData!=null);settings.ButtonData?.InitButton(_Button); }
            if(_ContentRoot!=null && settings.Parent!=null)
            {
                _ContentRoot.position=settings.Parent.position;_ContentRoot.anchoredPosition+=settings.Shift;
                switch(settings.Pivot) { case Pivot.TopLeft:_ContentRoot.pivot=new Vector2(0,1);break;case Pivot.TopRight:_ContentRoot.pivot=Vector2.one;break;case Pivot.BottomLeft:_ContentRoot.pivot=Vector2.zero;break;default:_ContentRoot.pivot=new Vector2(1,0);break; }
            }
        }
        public override void Show()
        {
            base.Show();fade?.Kill();if(_CanvasGroup==null) return;
            if(settings!=null && settings.OpenAnimation==AnimationType.Fade && Application.isPlaying) { _CanvasGroup.alpha=0;fade=_CanvasGroup.DOFade(1,.3f).SetUpdate(true); }else _CanvasGroup.alpha=1;
        }
        public override void Dismiss()
        {
            if(!gameObject.activeSelf) return;fade?.Kill();
            if(settings!=null && settings.CloseAnimation==AnimationType.Fade && _CanvasGroup!=null && Application.isPlaying) fade=_CanvasGroup.DOFade(0,.3f).SetUpdate(true).OnComplete(FinishDismiss);else FinishDismiss();
        }
        private void FinishDismiss() { DialogCoordinator.Dismiss(this);gameObject.SetActive(false);settings?.OnDismiss?.Invoke(); }
        public void OnButtonTap()=>settings?.ButtonData?.Activate(this);
        protected override void OnDisable() { fade?.Kill();fade=null;base.OnDisable(); }
        protected override void OnDestroy() { fade?.Kill();fade=null;base.OnDestroy(); }
    }
}
