using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using Nekki.Vector.GUI.Scenes.Main.Screens;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Main
{
    public class MainScene : Scene<MainScene>
    {
        [SerializeField] private ModuleSwitcher _moduleSwitcher;

        [SerializeField] private DialogCanvasController _dialogCanvas;

        [SerializeField] private LevelSceneController _RunController;
        
        public LevelSceneController RunController => _RunController;
        
        public ModuleSwitcher ModuleSwitcher => _moduleSwitcher;
        
        public EventSystem EventSystem => _moduleSwitcher.EventSystem;
        
        // Contains previous screens, not the currently active screen.
        private readonly Stack<UIModule> _screenHistory =
            new Stack<UIModule>();
        
        public UIModule CurrentScreen { get; private set; }

        public UIModule PrevScreen =>
            _screenHistory.Count > 0 ? _screenHistory.Peek() : null;

        public bool CanGoBack => _screenHistory.Count > 0;

        protected override void Awake()
        {
            if (!Game.Loaded)
            {
                SceneManager.LoadScene(0);
                return;
            }

            base.Awake();
        }

        protected override void Init()
        {
            base.Init();

            _screenHistory.Clear();
            CurrentScreen = GetModule<LobbyScreen>();
        }

        private void Start()
        {
            StartCoroutine(ShowVideo());
        }

        private IEnumerator ShowVideo()
        {
            CurrentScreen.Activate();
            
            yield return GetDialog<VideoDialog>().Show("intro.mp4");
            
            SoundsManager.Instance.PlayBackground(MusicType.menu);
        }

        public void SwitchScreen<T>(bool fade = true) where T : UIModule
        {
            var module = GetModule<T>();

            if (module == null)
            {
                Debug.LogError($"{typeof(T).Name} not found");
                return;
            }

            // Don't add duplicate entries by reopening the active screen.
            if (module == CurrentScreen)
            {
                return;
            }

            if (fade)
            {
                _moduleSwitcher.Switch(() => SwitchScreen(module));
            }
            else
            {
                SwitchScreen(module);
            }
        }

        void SwitchScreen(UIModule module)
        {
            // Recheck because the callback might execute later.
            if (module == null || module == CurrentScreen)
            {
                return;
            }

            var screenToLeave = CurrentScreen;

            // Forward navigation adds the old screen to history.
            if (screenToLeave != null)
            {
                _screenHistory.Push(screenToLeave);
            }

            CurrentScreen = module;

            if (screenToLeave != null)
            {
                screenToLeave.DeActivate();
            }

            module.Activate();
        }

        public void GoToPreviousScreen(bool fade = true)
        {
            if (!CanGoBack)
            {
                return;
            }

            if (fade)
            {
                _moduleSwitcher.Switch(GoToPrevious);
            }
            else
            {
                GoToPrevious();
            }
        }

        void GoToPrevious()
        {
            if (!CanGoBack)
            {
                return;
            }

            var screenToLeave = CurrentScreen;

            var screenToRestore = _screenHistory.Pop();
            CurrentScreen = screenToRestore;

            if (screenToLeave != null)
            {
                screenToLeave.DeActivate();
            }

            screenToRestore.Activate();
        }

        public T GetScreen<T>() where T : UIModule
        {
            return GetModule<T>();
        }

        public T GetDialog<T>() where T : BaseDialog
        {
            return _dialogCanvas.GetOrCreateDialog<T>();
        }

        public void ShowDialog<T>(bool modal = true) where T : BaseDialog
        {
            var dialog = _dialogCanvas.GetOrCreateDialog<T>();
            dialog.Modal = modal;
            dialog.Show();
        }

        public void ClearHistoryUntil<T>() where T : UIModule
        {
            if (!_screenHistory.Any(screen => screen.GetType() == typeof(T)))
                return;

            while (_screenHistory.Peek().GetType() != typeof(T))
            {
                _screenHistory.Pop();
            }
            
        }
        
    }
}