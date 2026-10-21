using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Scripts
{
    public class Fader : MonoBehaviour
    {
        [SerializeField] private Image _FadeImage;
        private Tween tween;
        public static Fader Instance { get; private set; }
        public bool IsFading=>tween!=null && tween.IsActive();
        public bool IsPaused=>IsFading && !tween.IsPlaying();
        private void Awake() { Instance=this;if(_FadeImage==null) _FadeImage=GetComponent<Image>(); }
        public static void RegisterInstance(Fader fader) { Instance=fader; }
        public void FadeIn(float duration)=>FadeIn(duration,Color.black,null);
        public void FadeIn(float duration,Color color)=>FadeIn(duration,color,null);
        public void FadeIn(float duration,Action callback)=>FadeIn(duration,Color.black,callback);
        public void FadeIn(float duration,Color color,Action callback)
        {
            Stop();if(_FadeImage==null) { Debug.LogError("Fader needs its Fade Image assigned.",this);return; }
            float alpha=color.a;color.a=0;_FadeImage.color=color;Fade(duration,alpha,callback);
        }
        public void FadeOut(float duration,Action callback=null) { Stop();Fade(duration,0,callback); }
        private void Fade(float duration,float alpha,Action callback)
        {
            if(_FadeImage==null) { Debug.LogError("Fader needs its Fade Image assigned.",this);return; }
            _FadeImage.raycastTarget=true;
            if(!Application.isPlaying || duration<=0) { var c=_FadeImage.color;c.a=alpha;_FadeImage.color=c;_FadeImage.raycastTarget=alpha>0;callback?.Invoke();return; }
            tween=_FadeImage.DOFade(alpha,duration).SetUpdate(true).OnComplete(()=>{tween=null;_FadeImage.raycastTarget=alpha>0;callback?.Invoke();});
        }
        public void Pause()=>tween?.Pause();
        public void Resume()=>tween?.Play();
        public void Stop() { tween?.Kill();tween=null;if(_FadeImage!=null) _FadeImage.raycastTarget=false; }
        private void OnDisable()=>Stop();
        private void OnDestroy() { Stop();if(Instance==this) Instance=null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void ResetSession() { Instance=null; }
    }
}
