using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class ModuleHolder : MonoBehaviour
    {
        [SerializeField] private List<UIModule> _MountOnStart = new List<UIModule>();
        [SerializeField] private List<UIModule> _MountOnLater = new List<UIModule>();
        [SerializeField] private List<UIModule> _MountByRequest = new List<UIModule>();
        [SerializeField] private List<UIModule> _SceneModules = new List<UIModule>();
        private readonly List<UIModule> live = new List<UIModule>();
        public List<UIModule> MountOnStart => _MountOnStart;
        public List<UIModule> MountOnLater => _MountOnLater;

        protected virtual void BeforeMountModules()
        {
        }

        protected virtual bool InitCondition() => true;

        protected virtual void Awake()
        {
            if (!InitCondition()) return;
            BeforeMountModules();
            foreach (var module in _SceneModules) RegisterModule(module);
            foreach (var template in _MountOnStart)
                if (template != null)
                    RegisterModule(UIModule.MountModule(template, transform, true));
            foreach (var template in _MountOnLater)
                if (template != null)
                    RegisterModule(UIModule.MountModule(template, transform, false));
        }

        public void RegisterModule(UIModule module)
        {
            if (module != null && !live.Contains(module)) live.Add(module);
        }

        protected T GetModule<T>() where T : UIModule
        {
            live.RemoveAll(m => m == null);
            foreach (var module in live)
                if (module is T typed)
                    return typed;
            return null;
        }

        public T GetOrMountModule<T>() where T : UIModule
        {
            var module = GetModule<T>();
            if (module != null) return module;
            foreach (var template in _MountByRequest)
                if (template is T)
                {
                    module = (T)UIModule.MountModule(template, transform, true);
                    RegisterModule(module);
                    return module;
                }

            return null;
        }

        public UIModule GetModuleByName(string name)
        {
            live.RemoveAll(m => m == null);
            return live.Find(m => m.name == name);
        }

        public List<UIModule> ActiveModule()
        {
            live.RemoveAll(m => m == null);
            return live.FindAll(m => m.IsActive);
        }

        protected virtual void OnDestroy()
        {
            live.Clear();
        }
    }
}