using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class ProgressBarSegmentUI : MonoBehaviour
    {
        [SerializeField] private Image _BackgroundLine,_ProgressLine,_AnimLine;
        [SerializeField] private LayoutElement _LayoutElement;
        [SerializeField] private ResolutionImage _BoostGlow;
        [SerializeField] private ProgressBarSegmentUIParameters _Parameters=new ProgressBarSegmentUIParameters();
        private int progress;
        private Sequence animation;
        public ProgressBarSegmentUIParameters Parameters { get=>_Parameters;set { _Parameters=value ?? new ProgressBarSegmentUIParameters();Refresh(); } }
        public bool ShowAnimation { get=>_Parameters.ShowAnimation;set { _Parameters.ShowAnimation=value;Refresh(); } }
        public int CurrentProgress { get=>progress;set { progress=Mathf.Clamp(value,0,Mathf.Max(0,_Parameters.MaxProgress));Refresh(); } }
        public void SetParametersAndProgress(ProgressBarSegmentUIParameters value,int current) { _Parameters=value ?? new ProgressBarSegmentUIParameters();CurrentProgress=current; }
        public void Refresh()
        {
            StopAnim();if(_Parameters==null) _Parameters=new ProgressBarSegmentUIParameters();
            bool boost=_Parameters.FillType==ProgressBarSegmentFillType.Boosted;
            float fraction=_Parameters.FillType==ProgressBarSegmentFillType.Filled || boost?1:(_Parameters.FillType==ProgressBarSegmentFillType.Empty?0:(_Parameters.MaxProgress>0?Mathf.Clamp01((float)progress/_Parameters.MaxProgress):0));
            Apply(_BackgroundLine,1,_Parameters.BackgroundColor,!boost);Apply(_ProgressLine,fraction,_Parameters.ProgressColor,!boost);
            Apply(_AnimLine,_Parameters.MaxProgress>0?Mathf.Clamp01((float)_Parameters.AnimatedProgress/_Parameters.MaxProgress):0,_Parameters.ProgressColor,ShowAnimation);
            if(_BoostGlow!=null) { _BoostGlow.enabled=boost;if(boost && !string.IsNullOrEmpty(_Parameters.BoostGlowSpriteName)) _BoostGlow.SpriteName=_Parameters.BoostGlowSpriteName; }
            if(_LayoutElement!=null) { _LayoutElement.minHeight=Mathf.Max(0,_Parameters.LineHeight);_LayoutElement.minWidth=Mathf.Max(0,_Parameters.LineWidth); }
            if(ShowAnimation && _AnimLine!=null && Application.isPlaying && isActiveAndEnabled) animation=DOTween.Sequence().SetUpdate(true).Append(_AnimLine.DOFade(0,.8f)).Append(_AnimLine.DOFade(1,.8f)).SetLoops(-1);
        }
        private void Apply(Image image,float fraction,Color color,bool enabled) { if(image==null) return;image.enabled=enabled;image.color=color;image.rectTransform.sizeDelta=new Vector2(Mathf.Max(0,_Parameters.LineWidth),Mathf.Max(0,_Parameters.LineHeight)*fraction); }
        public void StopAnim() { animation?.Kill();animation=null; }
        private void OnDisable()=>StopAnim();
        private void OnDestroy()=>StopAnim();
    }
}
