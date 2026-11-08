using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Dialogs
{
    [RequireComponent(typeof(Canvas))]
    public class DialogCanvasController : MonoBehaviour
    {
        [SerializeField] private List<BaseDialog> _RegisteredDialogs = new List<BaseDialog>();
        private readonly Dictionary<Type, BaseDialog> instances = new Dictionary<Type, BaseDialog>();
        private readonly Dictionary<Type, Func<BaseDialog>> factories = new Dictionary<Type, Func<BaseDialog>>();
        public static DialogCanvasController Current { get; private set; }
        public static Canvas DialogsCanvas => Current == null ? null : Current.GetComponent<Canvas>();

        private void Awake()
        {
            Current = this;
            foreach (var dialog in _RegisteredDialogs)
                if (dialog != null)
                    instances[dialog.GetType()] = dialog;
        }

        public void Register<T>(T instance) where T : BaseDialog
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            instances[typeof(T)] = instance;
        }

        public void RegisterFactory<T>(Func<T> factory) where T : BaseDialog
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            factories[typeof(T)] = () => factory();
        }

        public T GetOrCreateDialog<T>() where T : BaseDialog
        {
            var type = typeof(T);
            if (instances.TryGetValue(type, out var dialog) && dialog != null) return (T)dialog;
            if (!factories.TryGetValue(type, out var factory))
                throw new InvalidOperationException("Register an existing " + type.Name +
                                                    " or a factory before requesting it.");
            dialog = factory();
            if (dialog == null) throw new InvalidOperationException("Dialog factory returned null for " + type.Name);
            instances[type] = dialog;
            return (T)dialog;
        }

        public void CloseDialog<T>() where T : BaseDialog
        {
            if (instances.TryGetValue(typeof(T), out var dialog) && dialog != null) dialog.Dismiss();
        }

        protected virtual void OnDestroy()
        {
            if (Current == this) Current = null;
            instances.Clear();
            factories.Clear();
        }
    }
}