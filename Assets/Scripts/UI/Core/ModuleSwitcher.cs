using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class ModuleSwitcher : MonoBehaviour
    {
        [SerializeField] private EventSystem _EventSystem;
        [SerializeField] private Image _Background;
        [SerializeField] private float _FadeTime = 1.5f, _PauseTime = 1f;
        private Sequence transition;
        private bool previousInput, ownsInput;

        public EventSystem EventSystem => _EventSystem;

        public void Switch(Action callback, bool needFadeOut = true) =>
            Switch(callback, null, _FadeTime, _PauseTime, needFadeOut);

        public void Switch(Action callback, Action after, bool needFadeOut = true) =>
            Switch(callback, after, _FadeTime, _PauseTime, needFadeOut);

        public void Switch(Action callback, float fade, float pause, bool needFadeOut = true) =>
            Switch(callback, null, fade, pause, needFadeOut);

        public void Switch(Action callback, Action after, float fade, float pause, bool needFadeOut = true)
        {
            Stop();
            if (_Background == null)
            {
                Debug.LogError("ModuleSwitcher needs its Background Image assigned.", this);
                return;
            }

            if (!Application.isPlaying) return;
            if (_EventSystem != null)
            {
                previousInput = _EventSystem.enabled;
                _EventSystem.enabled = false;
                ownsInput = true;
            }
            InputController.instance.Disable();
            var c = _Background.color;
            c.a = 0;
            _Background.color = c;
            _Background.raycastTarget = true;
            transform.SetAsLastSibling();
            transition = DOTween.Sequence().SetUpdate(true);
            transition.Append(_Background.DOFade(1f, Mathf.Max(0, fade))).AppendInterval(Mathf.Max(0, pause))
                .AppendCallback(() => callback?.Invoke());
            if (needFadeOut) transition.Append(_Background.DOFade(0f, Mathf.Max(0, fade)));
            transition.AppendCallback(() => after?.Invoke()).OnKill(() =>
            {
                transition = null;
                ReleaseInput();
                if (_Background != null) _Background.raycastTarget = false;
            });
        }

        public void Stop()
        {
            var owned = transition;
            transition = null;
            owned?.Kill();
            ReleaseInput();
            if (_Background != null)
            {
                var c = _Background.color;
                c.a = 0;
                _Background.color = c;
                _Background.raycastTarget = false;
            }
        }

        private void ReleaseInput()
        {
            if (ownsInput && _EventSystem != null) _EventSystem.enabled = previousInput;
            ownsInput = false;
            
            InputController.instance.Enable();
            
        }

        private void OnDisable() => Stop();
        private void OnDestroy() => Stop();
    }
}