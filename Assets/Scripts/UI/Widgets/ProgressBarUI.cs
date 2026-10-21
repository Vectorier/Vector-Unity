using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class ProgressBarUI : MonoBehaviour
    {
        [SerializeField] private ProgressBarSegmentUI _SegmentPrefab,_CardProgress;
        [SerializeField] private RectTransform _SegmentsAnchor;
        [SerializeField] private Text _ValueNumbers;
        [SerializeField] private VerticalLayoutGroup _LayoutGroup;
        [SerializeField] private GameObject _BoostTimerRoot;
        [SerializeField] private ResolutionImage _BoostTimerCircle;
        [SerializeField] private ProgressBarUIParameters _Parameters=new ProgressBarUIParameters();
        private readonly List<ProgressBarSegmentUI> segments=new List<ProgressBarSegmentUI>();
        private int Limit { get { if(_Parameters.LevelRequirements!=null && _Parameters.CurrentLevel>=0 && _Parameters.CurrentLevel<_Parameters.LevelRequirements.Length) return Mathf.Max(0,_Parameters.LevelRequirements[_Parameters.CurrentLevel]);return Mathf.Max(0,_Parameters.MaxProgress); } }
        public float NormalizedProgress=>Limit>0?Mathf.Clamp01((float)_Parameters.CurrentProgress/Limit):0;
        public ProgressBarUIParameters Parameters { get=>_Parameters;set=>SetParameters(value); }
        public bool ShowAnimation { get=>_Parameters.ShowAnimation;set { _Parameters.ShowAnimation=value;Refresh(); } }
        public void SetParameters(ProgressBarUIParameters value) { _Parameters=value ?? new ProgressBarUIParameters();Refresh(); }
        public void Refresh()
        {
            if(_Parameters==null) _Parameters=new ProgressBarUIParameters();
            if(_ValueNumbers!=null) { _ValueNumbers.enabled=_Parameters.ShowNumbers;_ValueNumbers.text=Mathf.Max(0,_Parameters.CurrentProgress)+"/"+Limit; }
            if(_LayoutGroup!=null) _LayoutGroup.spacing=8;
            if(_CardProgress!=null)
            {
                _CardProgress.gameObject.SetActive(_Parameters.ShowProgress);
                _CardProgress.SetParametersAndProgress(new ProgressBarSegmentUIParameters { BackgroundColor=_Parameters.BackgroundColor,ProgressColor=_Parameters.ProgressColor,LineWidth=_Parameters.ProgressWidth,MaxProgress=Limit,AnimatedProgress=Mathf.Max(0,_Parameters.CurrentProgress+_Parameters.AnimationCardCount),ShowAnimation=_Parameters.ShowAnimation },_Parameters.CurrentProgress);
            }
            foreach(var segment in segments) if(segment!=null) { if(Application.isPlaying) Destroy(segment.gameObject);else DestroyImmediate(segment.gameObject); }segments.Clear();
            if(_SegmentPrefab!=null && _SegmentsAnchor!=null)
            {
                int count=Mathf.Clamp(_Parameters.CurrentLevel,0,Mathf.Clamp(_Parameters.MaxLevel,0,1000));
                for(int i=0;i<count;i++) { var segment=Instantiate(_SegmentPrefab,_SegmentsAnchor,false);segments.Add(segment);segment.SetParametersAndProgress(new ProgressBarSegmentUIParameters { BackgroundColor=Color.clear,ProgressColor=_Parameters.ProgressColor,LineHeight=10,LineWidth=_Parameters.LevelWidth,MaxProgress=1,FillType=i<_Parameters.BoostedLevel?ProgressBarSegmentFillType.Boosted:ProgressBarSegmentFillType.Filled },1); }
            }
            RefreshBoostTimer();
        }
        public void RefreshBoostTimer()
        {
            if(_BoostTimerRoot!=null) _BoostTimerRoot.SetActive(_Parameters.ShowNumbers && _Parameters.BoostedLevel>0);
            var time=string.IsNullOrEmpty(_Parameters.BoostTimerId)?null:UIBindings.RemainingSeconds?.Invoke(_Parameters.BoostTimerId);
            if(_BoostTimerCircle!=null && time.HasValue && _Parameters.BoostDuration>0) _BoostTimerCircle.fillAmount=Mathf.Clamp01(time.Value/_Parameters.BoostDuration);
        }
        public void SetActive(bool active) { gameObject.SetActive(active); }
        private void OnDisable() { _CardProgress?.StopAnim();foreach(var segment in segments) if(segment!=null) segment.StopAnim(); }
    }
}
