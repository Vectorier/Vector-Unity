using System;
using System.Collections;
using System.Collections.Generic;
using Nekki.Vector.GUI.Scenes.Main;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UI.Dialogs;
using Object = System.Object;

namespace UI
{
    public class UIModule : MonoBehaviour
    {
        private static readonly List<UIModule> modules = new List<UIModule>();
        [SerializeField] private int _Order;
        [SerializeField] protected GameObject _SelectedObjectOnActivation;
        private Transform sceneParent;
        protected bool _IsInited, _IsReleased;
        public bool IsActive => gameObject.activeSelf;

        public GameObject SelectedObjectOnActivation => _SelectedObjectOnActivation;
        public static event Action<UIModule> OnModuleActivated, OnModuleDeactivated;

        private void Awake()
        {
            if (!modules.Contains(this)) modules.Add(this);
            sceneParent = transform.parent;
        }

        public static UIModule MountModule(UIModule template, Transform parent, bool active)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            var module = Instantiate(template, parent, false);
            module.name = template.name;
            module.sceneParent = parent;
            module.gameObject.SetActive(false);
            if (active) module.Activate();
            return module;
        }

        public static T GetModule<T>() where T : UIModule
        {
            modules.RemoveAll(m => m == null);
            foreach (var module in modules)
                if (module is T typed)
                    return typed;
            return null;
        }

        public static UIModule GetModuleByName(string name)
        {
            modules.RemoveAll(m => m == null);
            return modules.Find(m => m.name == name);
        }

        public void Activate()
        {
            gameObject.SetActive(true);
            transform.SetSiblingIndex(_Order);
            if (!_IsInited) Init();
            OnActivated();
            Send(OnModuleActivated);
        }

        public void DeActivate()
        {
            gameObject.SetActive(false);
            OnDeactivated();
            Send(OnModuleDeactivated);
        }

        private void Send(Action<UIModule> callback)
        {
            if (!Application.isPlaying) callback?.Invoke(this);
            else UICoroutineRunner.Run(Notify(callback));
        }

        private IEnumerator Notify(Action<UIModule> callback)
        {
            yield return new WaitForEndOfFrame();
            if (this != null) callback?.Invoke(this);
        }

        public void MoveToDialogsCanvas()
        {
            var canvas = DialogCanvasController.DialogsCanvas;
            if (canvas != null) transform.SetParent(canvas.transform, false);
        }

        public void MoveToSceneCanvas()
        {
            if (sceneParent != null) transform.SetParent(sceneParent, false);
            Activate();
        }

        protected virtual void Init()
        {
            _IsInited = true;
        }

        protected virtual void Free()
        {
            _IsReleased = true;
        }

        protected virtual void OnActivated()
        {
            SetInputActions(InputController.instance.InputActions);
            SetSelectedGameObject(_SelectedObjectOnActivation);
        }

        protected virtual void OnDeactivated()
        {
            var input = InputController.instance;
            if (input != null) DisableInputActions(input.InputActions);
        }

        protected virtual void SetInputActions(PlayerInputActions actions)
        {
            actions.UI.Back.performed += BackPress;
            actions.UI.Up.performed += UpPress;
            actions.UI.Down.performed += DownPress;
            actions.UI.Left.performed += LeftPress;
            actions.UI.Right.performed += RightPress;
            actions.UI.XButton.performed += XPress;
            actions.UI.YButton.performed += YPress;
        }

        protected virtual void DisableInputActions(PlayerInputActions actions)
        {
            actions.UI.Back.performed -= BackPress;
            actions.UI.Up.performed -= UpPress;
            actions.UI.Down.performed -= DownPress;
            actions.UI.Left.performed -= LeftPress;
            actions.UI.Right.performed -= RightPress;
            actions.UI.XButton.performed -= XPress;
            actions.UI.YButton.performed -= YPress;
        }

        protected virtual void BackPress(InputAction.CallbackContext ctx = default)
        {
        }

        protected virtual void XPress(InputAction.CallbackContext ctx = default)
        {
        }

        protected virtual void YPress(InputAction.CallbackContext ctx = default)
        {
        }

        protected virtual void UpPress(InputAction.CallbackContext ctx = default)
        {
        }

        protected virtual void DownPress(InputAction.CallbackContext ctx = default)
        {
        }

        protected virtual void LeftPress(InputAction.CallbackContext ctx = default)
        {
        }

        protected virtual void RightPress(InputAction.CallbackContext ctx = default)
        {
        }

        public List<Button> GetButtonsByName(string name)
        {
            var result = new List<Button>();
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (name == null || button.name == name)
                    result.Add(button);
            return result;
        }

        private void OnDestroy()
        {
            if (!_IsReleased) Free();
            modules.Remove(this);
        }

        protected void SetSelectedGameObject(GameObject go)
        {
            MainScene.Current.EventSystem.SetSelectedGameObject(go);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            modules.Clear();
            OnModuleActivated = null;
            OnModuleDeactivated = null;
        }
    }
}