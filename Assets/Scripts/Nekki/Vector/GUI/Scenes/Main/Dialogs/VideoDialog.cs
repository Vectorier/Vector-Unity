using System.Collections;
using System.IO;
using Core._Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main.Dialogs
{
    /// <summary>
    /// Full-screen cutscene player for the videos in StreamingAssets/videos (intro, level start/end cutscenes).
    /// Fades in from black when playback starts; finishing or skipping fades to black and then out to the screen behind.
    /// </summary>
    public class VideoDialog : BaseDialog
    {
        private const float FadeInTime = 0.5f;
        private const float FadeToBlackTime = 0.4f;
        private const float FadeOutTime = 0.5f;

        [SerializeField] VideoPlayer _videoPlayer;
        [SerializeField] RenderTexture _renderTexture;

        private bool _stopped = true;
        private bool _closing;
        private bool _pausedMusic;
        private Image _blackOverlay;
        private CanvasGroup _canvasGroup;
        private Coroutine _fade;

        private void Awake()
        {
            _videoPlayer.prepareCompleted += OnPrepared;
            _videoPlayer.loopPointReached += _ => Dismiss();
            _videoPlayer.errorReceived += (_, message) =>
            {
                Debug.LogWarning("Video could not be played: " + message);
                Dismiss();
            };

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            var overlay = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)overlay.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _blackOverlay = overlay.GetComponent<Image>();
            _blackOverlay.color = Color.black;
            _blackOverlay.raycastTarget = false;
        }

        /// <summary>Plays <paramref name="videoName"/> and completes when it ends, fails or is skipped.</summary>
        public IEnumerator Show(string videoName)
        {
            var path = ResolvePath(videoName);
            if (path == null)
            {
                Debug.LogWarning("Video not found: " + videoName);
                yield break;
            }

            _stopped = false;
            _closing = false;
            base.Show();
            transform.SetAsLastSibling();
            _blackOverlay.transform.SetAsLastSibling();
            SetOverlay(1f);
            _canvasGroup.alpha = 1f;
            _pausedMusic = SoundsManager.Instance.PauseMusic();

            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = path;
            _videoPlayer.gameObject.SetActive(true);
            _videoPlayer.Prepare();

            yield return new WaitUntil(() => _stopped);
        }

        private static string ResolvePath(string videoName)
        {
            // Videos live in StreamingAssets/videos; the root is still accepted for older mods.
            var path = Path.Combine(VectorPaths.Videos, videoName);
            if (Application.isMobilePlatform || File.Exists(path))
            {
                return path;
            }
            var legacyPath = Path.Combine(Application.streamingAssetsPath, videoName);
            return File.Exists(legacyPath) ? legacyPath : null;
        }

        private void OnPrepared(VideoPlayer player)
        {
            player.Play();
            if (!_closing)
            {
                StartFade(FadeVideoIn());
            }
        }

        /// <summary>Skips or ends the video with a fade to black and back out.</summary>
        public override void Dismiss()
        {
            if (_stopped || _closing)
            {
                return;
            }
            _closing = true;
            if (isActiveAndEnabled)
            {
                StartFade(FadeVideoOut());
            }
            else
            {
                Close();
            }
        }

        private IEnumerator FadeVideoIn()
        {
            yield return Fade(a => SetOverlay(a), _blackOverlay.color.a, 0f, FadeInTime);
        }

        private IEnumerator FadeVideoOut()
        {
            yield return Fade(a => SetOverlay(a), _blackOverlay.color.a, 1f, FadeToBlackTime);
            StopVideo();
            yield return Fade(a => _canvasGroup.alpha = a, 1f, 0f, FadeOutTime);
            Close();
        }

        private static IEnumerator Fade(System.Action<float> apply, float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                apply(Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            apply(to);
        }

        private void StartFade(IEnumerator routine)
        {
            if (_fade != null)
            {
                StopCoroutine(_fade);
            }
            _fade = StartCoroutine(routine);
        }

        private void SetOverlay(float alpha)
        {
            var c = _blackOverlay.color;
            c.a = alpha;
            _blackOverlay.color = c;
        }

        private void Close()
        {
            _fade = null;
            StopVideo();
            _canvasGroup.alpha = 1f;
            _stopped = true;
            _closing = false;
            base.Dismiss();
        }

        private void StopVideo()
        {
            _videoPlayer.Stop();
            _videoPlayer.gameObject.SetActive(false);
            _renderTexture.Release();
            if (_pausedMusic)
            {
                _pausedMusic = false;
                SoundsManager.Instance.ResumeMusic();
            }
        }
    }
}
