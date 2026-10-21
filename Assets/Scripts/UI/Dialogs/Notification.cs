using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Dialogs
{
    public abstract class Notification : BaseDialog
    {
        public enum Orientation { Top,Bottom,Right,Left,LeftBottom }
        public enum HideBy { Click,TimeOrClick,TimeBlockClicks,TimeDontBlockClicks }
        [Serializable] public class Parameters
        {
            public Orientation Orientation;
            public HideBy HideBy;
            public string Image,Text=string.Empty;
            public GameObject Content;
            public DialogQueueType QueueType=DialogQueueType.Notification;
            public Action Callback;
        }
        [SerializeField] protected RectTransform _Root,_TweenRoot;
        protected Parameters _Parameters;
        protected Vector3 _ShowDeltaPosition;
        private Sequence movement;
        private bool closing;
        protected void SetRootPosition()
        {
            if(_Root==null) _Root=GetComponent<RectTransform>();if(_TweenRoot==null) _TweenRoot=_Root;if(_Root==null) return;
            var pivot=Vector2.zero;
            switch(_Parameters.Orientation)
            {
                case Orientation.Top:pivot=new Vector2(.5f,1);_ShowDeltaPosition=new Vector3(0,_Root.sizeDelta.y);break;
                case Orientation.Bottom:pivot=new Vector2(.5f,0);_ShowDeltaPosition=new Vector3(0,-_Root.sizeDelta.y);break;
                case Orientation.Right:pivot=new Vector2(1,.5f);_ShowDeltaPosition=new Vector3(_Root.sizeDelta.x,0);break;
                case Orientation.Left:pivot=new Vector2(0,.5f);_ShowDeltaPosition=new Vector3(-_Root.sizeDelta.x,0);break;
                default:pivot=new Vector2(0,.1f);_ShowDeltaPosition=new Vector3(-_Root.sizeDelta.x,0);break;
            }
            _Root.pivot=_Root.anchorMin=_Root.anchorMax=pivot;
        }
        public override void Show()
        {
            if(_Parameters==null) _Parameters=new Parameters();
            Modal=_Parameters.HideBy!=HideBy.TimeDontBlockClicks; closing=false;SetRootPosition();base.Show();UIBindings.PlaySound?.Invoke("notification");PlayTweenForward();
        }
        public void PlayTweenForward()
        {
            movement?.Kill();if(_TweenRoot==null || !Application.isPlaying) return;
            _TweenRoot.anchoredPosition=_ShowDeltaPosition;
            movement=DOTween.Sequence().SetUpdate(true).Append(_TweenRoot.DOAnchorPos(Vector2.zero,.7f));
            if(_Parameters.HideBy!=HideBy.Click) movement.AppendInterval(2.5f).OnComplete(PlayTweenBackward);
        }
        public void PlayTweenBackward()
        {
            if(closing) return;closing=true;movement?.Kill();
            if(_TweenRoot==null || !Application.isPlaying) { Dismiss();return; }
            movement=DOTween.Sequence().SetUpdate(true).Append(_TweenRoot.DOAnchorPos(_ShowDeltaPosition,.7f)).OnComplete(Dismiss);
        }
        public override void Dismiss()
        {
            bool shown=gameObject.activeSelf;movement?.Kill();movement=null;DialogCoordinator.Dismiss(this);gameObject.SetActive(false);
            if(!shown) return;_Parameters?.Callback?.Invoke();
        }
        public void OnTap() { if(_Parameters!=null && (_Parameters.HideBy==HideBy.Click || _Parameters.HideBy==HideBy.TimeOrClick)) PlayTweenBackward(); }
        protected override void OnDisable() { movement?.Kill();movement=null;base.OnDisable(); }
        protected override void OnDestroy() { movement?.Kill();movement=null;base.OnDestroy(); }
    }
}
