using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    public class VideoDialog : BaseDialog
    {
        [SerializeField] VideoPlayer _videoPlayer;
        [SerializeField] RenderTexture _renderTexture;

        private bool stopped = false;

        public IEnumerator Show(string videoName)
        {
            stopped = false;
            base.Show();
            SoundsManager.Instance.AudioSourceBackground.Stop();
            _videoPlayer.gameObject.SetActive(false);
#if PLATFORM_ANDROID
			VideoPlayer.source = VideoSource.VideoClip;
			VideoPlayer.clip = Resources.Load<VideoClip>(payload.Video);
#else
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = "file:///" + Application.streamingAssetsPath + "/" + videoName;
#endif
            _videoPlayer.gameObject.SetActive(true);
            _videoPlayer.Prepare();
            _videoPlayer.prepareCompleted += vp =>
            {
                vp.Play();
            };
            _videoPlayer.loopPointReached += (vp) => Dismiss();
            
            yield return new WaitUntil(() => stopped);
        }
        
        public override void Dismiss()
        {
            StopVideo();
            base.Dismiss();
        }
        
        private void StopVideo()
        {
            _videoPlayer.Stop();
            _videoPlayer.gameObject.SetActive(false);
            _renderTexture.Release();
            SoundsManager.Instance.AudioSourceBackground.Play();
            
            stopped = true;
        }
    }
}