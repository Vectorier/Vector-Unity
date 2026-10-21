using DG.Tweening;
using UI.Localization;
using UnityEngine;

namespace UI
{
    public class TextAnimator : MonoBehaviour
    {
        [SerializeField] private float _Delay;
        [SerializeField] private float _OneCharSpeed=0.035f;
        [SerializeField] private bool _AlwaysUse=true;
        [SerializeField] private bool _IgnoreSource=true;
        [SerializeField] private ScrambleMode _ScrambleMode;
        private LabelAlias target;
        private Tween tween;
        private bool force;
        public LabelAlias Target { set=>target=value; }
        public void Arm() { force=true; }
        public void Play(string value)
        {
            Stop(); if(target==null) target=GetComponent<LabelAlias>(); if(target==null) return;
            value=value ?? string.Empty;
            if(!Application.isPlaying || !isActiveAndEnabled || (!force && !_AlwaysUse)) { target.text=value; return; }
            if(_IgnoreSource) target.text=string.Empty;
            force=false; UIBindings.PlaySound?.Invoke("type_loop");
            tween=target.DOText(value,Mathf.Max(0f,_OneCharSpeed)*value.Length,true,_ScrambleMode).SetDelay(Mathf.Max(0f,_Delay)).SetUpdate(true).OnComplete(()=>UIBindings.StopSound?.Invoke("type_loop"));
        }
        public void Stop() { tween?.Kill(); tween=null; UIBindings.StopSound?.Invoke("type_loop"); }
        private void OnDisable() { Stop(); }
        private void OnDestroy() { Stop(); }
    }
}
