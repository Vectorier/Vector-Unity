using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    /// <summary>Full-screen cutscene player for videos in StreamingAssets (intro, level start/end cutscenes).</summary>
    public class VideoDialog : BaseDialog
    {
        [SerializeField] VideoPlayer _videoPlayer;
        [SerializeField] RenderTexture _renderTexture;

        private bool _stopped = true;

        private void Awake()
        {
            _videoPlayer.prepareCompleted += vp => vp.Play();
            _videoPlayer.loopPointReached += _ => Dismiss();
            _videoPlayer.errorReceived += (_, message) =>
            {
                Debug.LogWarning("Video could not be played: " + message);
                Dismiss();
            };
        }

        /// <summary>Plays <paramref name="videoName"/> and completes when it ends, fails or is skipped.</summary>
        public IEnumerator Show(string videoName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, videoName);
            if (!File.Exists(path) && !Application.isMobilePlatform)
            {
                Debug.LogWarning("Video not found: " + path);
                yield break;
            }

            _stopped = false;
            base.Show();
            SoundsManager.Instance.AudioSourceBackground.Stop();
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = path;
            _videoPlayer.gameObject.SetActive(true);
            _videoPlayer.Prepare();

            yield return new WaitUntil(() => _stopped);
        }

        public override void Dismiss()
        {
            if (_stopped)
            {
                return;
            }
            StopVideo();
            base.Dismiss();
        }

        private void StopVideo()
        {
            _videoPlayer.Stop();
            _videoPlayer.gameObject.SetActive(false);
            _renderTexture.Release();
            SoundsManager.Instance.AudioSourceBackground.Play();
            _stopped = true;
        }
    }
}
