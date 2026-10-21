using UnityEngine;
using UI.InputControllers;

namespace UI
{
    /// <summary>Derive your own screen host from Scene&lt;YourHost&gt;. No game startup is performed.</summary>
    public abstract class Scene<T> : ModuleHolder where T : Scene<T>
    {
        protected static T _Current;
        protected bool _IsInited, _IsReleased;
        public static T Current => _Current;
        public Canvas Canvas { get; private set; }
        public KeyboardController KeyboardController { get; private set; }

        protected virtual void Init()
        {
            _IsInited = true;
        }

        protected virtual void Free()
        {
            _IsReleased = true;
        }

        protected override void Awake()
        {
            _Current = (T)this;
            Canvas = GetComponent<Canvas>();
            KeyboardController = GetComponent<KeyboardController>();
            base.Awake();
            Init();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (!_IsReleased) Free();
            if (_Current == this) _Current = null;
        }
    }
}