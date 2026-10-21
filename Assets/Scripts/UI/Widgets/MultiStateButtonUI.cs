using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class MultiStateButtonUI : MonoBehaviour
    {
        [SerializeField] private Button _Button;
        [SerializeField] private ResolutionImage _Image;
        [SerializeField] private MultiStateButtonUIParameters _Parameters=new MultiStateButtonUIParameters();
        private List<ButtonData> states=new List<ButtonData>();
        private int index;
        private Sequence sequence;
        public Button Button=>_Button;
        public ResolutionImage Image=>_Image;
        public MultiStateButtonUIParameters Parameters=>_Parameters;
        public void Init(MultiStateButtonUIParameters parameters,List<ButtonData> values) { sequence?.Kill();_Parameters=parameters ?? new MultiStateButtonUIParameters();states=values ?? new List<ButtonData>();index=0;ShowState();Schedule(); }
        private void ShowState() { if(states.Count>0 && _Image!=null) _Image.SpriteName=states[index]?.ImageAlias; }
        private void Schedule() { if(!Application.isPlaying || !isActiveAndEnabled || states.Count<2) return;sequence=DOTween.Sequence().SetUpdate(true).AppendInterval(Mathf.Max(.01f,_Parameters.ShowTime)).AppendCallback(ChangeState); }
        public void ChangeState()
        {
            sequence?.Kill();sequence=null;if(states.Count==0) return;
            index=(index+1)%states.Count;
            if(_Image==null || !Application.isPlaying || !isActiveAndEnabled) { ShowState();return; }
            sequence=DOTween.Sequence().SetUpdate(true).Append(_Image.DOFade(0,Mathf.Max(0,_Parameters.ChangeStateAnimationTime)/2)).AppendCallback(ShowState).Append(_Image.DOFade(1,Mathf.Max(0,_Parameters.ChangeStateAnimationTime)/2)).AppendInterval(Mathf.Max(.01f,_Parameters.ShowTime)).AppendCallback(ChangeState);
        }
        public void OnTap() { if(states.Count>0) states[index]?.Callback?.Invoke(); }
        private void OnEnable()=>Schedule();
        private void OnDisable() { sequence?.Kill();sequence=null; }
        private void OnDestroy() { sequence?.Kill();sequence=null; }
    }
}
