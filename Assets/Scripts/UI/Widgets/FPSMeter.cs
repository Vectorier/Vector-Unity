using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class FPSMeter : UIModule
    {
        [SerializeField] private float _UpdateInterval=.2f;
        [SerializeField] private Text _Label;
        public bool ShowFPS=true;
        public static float FPS { get; private set; }
        private float lastTime;
        private int lastFrame;
        protected override void Init() { base.Init();lastTime=Time.realtimeSinceStartup;lastFrame=Time.frameCount; }
        private void Update() { float delta=Time.realtimeSinceStartup-lastTime;if(delta<Mathf.Max(.01f,_UpdateInterval)) return;FPS=(Time.frameCount-lastFrame)/delta;lastTime=Time.realtimeSinceStartup;lastFrame=Time.frameCount;if(_Label!=null) _Label.text=$"FPS: {FPS:F1}";UpdateVisiblity(); }
        public void UpdateVisiblity() { if(_Label!=null) _Label.enabled=ShowFPS; }
    }
}
