using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Common
{
    public class TimerUI : MonoBehaviour
    {
        [SerializeField] private string _Id;
        [SerializeField] private TimerFormat _Format;
        [SerializeField] private Text _Label;
        [SerializeField] private UnityEvent _OnExpire=new UnityEvent();
        private bool manual,expired;
        private float deadline,nextUpdate;
        public string Id { get=>_Id;set { _Id=value;manual=false;expired=false;RefreshTimer(); } }
        public UnityEvent OnExpire=>_OnExpire;
        public TimerFormat Format { get=>_Format;set { _Format=value;RefreshTimer(); } }
        public void SetRemainingSeconds(float seconds)
        {
            if(float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!manual || seconds>0) expired=false;manual=true;deadline=Time.unscaledTime+Mathf.Max(0,seconds);RefreshTimer();
        }
        public void RefreshTimer()
        {
            float? remaining=manual ? (float?)Mathf.Max(0,deadline-Time.unscaledTime) : UIBindings.RemainingSeconds?.Invoke(_Id);
            if(!remaining.HasValue || float.IsNaN(remaining.Value) || float.IsInfinity(remaining.Value)) return;
            if(remaining.Value>0) expired=false;
            if(remaining.Value<=0) { if(_Label!=null) _Label.text=string.Empty;if(!expired) { expired=true;_OnExpire.Invoke(); }return; }
            if(_Label!=null) _Label.text=FormatTime(TimeSpan.FromSeconds(Math.Ceiling(remaining.Value)));
        }
        private string FormatTime(TimeSpan time)
        {
            if(_Format==TimerFormat.Full) return (time.Days>0?time.Days+"d ":string.Empty)+string.Format("{0:00}:{1:00}:{2:00}",time.Hours,time.Minutes,time.Seconds);
            if(time.Days>0) return string.Format("{0}d {1:00}h",time.Days,time.Hours);
            if(time.Hours>0) return string.Format("{0:00}h {1:00}m",time.Hours,time.Minutes);
            if(time.Minutes>0) return string.Format("{0:00}m {1:00}s",time.Minutes,time.Seconds);
            return string.Format("{0:00}s",time.Seconds);
        }
        private void OnEnable() { nextUpdate=0; }
        private void Update() { if(Time.unscaledTime>=nextUpdate) { nextUpdate=Time.unscaledTime+.25f;RefreshTimer(); } }
    }
}
