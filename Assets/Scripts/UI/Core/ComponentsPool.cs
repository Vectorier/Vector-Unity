using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class ComponentsPool<T> where T:MonoBehaviour
    {
        private readonly Transform parent;
        private readonly Stack<T> free=new Stack<T>();
        public GameObject Prefab { get; }
        public List<T> ObjectsInUse { get; }=new List<T>();
        public ComponentsPool(Transform parent,GameObject template,int initialCacheSize=0)
        {
            this.parent=parent;Prefab=template ?? throw new ArgumentNullException(nameof(template));
            if(Prefab.GetComponent<T>()==null) throw new ArgumentException("Pool template needs a "+typeof(T).Name+" component.",nameof(template));
            for(int i=0;i<initialCacheSize;i++) Register(UnityEngine.Object.Instantiate(Prefab,parent,false).GetComponent<T>(),false);
        }
        public void Register(T item,bool inUse=true)
        {
            if(item==null || ObjectsInUse.Contains(item) || free.Contains(item)) return;
            if(inUse) ObjectsInUse.Add(item);else { item.transform.SetParent(parent,false);item.gameObject.SetActive(false);free.Push(item); }
        }
        public T Get()
        {
            T item=null;while(free.Count>0 && item==null) item=free.Pop();
            if(item==null) item=UnityEngine.Object.Instantiate(Prefab,parent,false).GetComponent<T>();
            ObjectsInUse.Add(item);item.gameObject.SetActive(true);return item;
        }
        public void Return(T item) { if(item==null || !ObjectsInUse.Remove(item)) return;item.transform.SetParent(parent,false);item.gameObject.SetActive(false);free.Push(item); }
        public void ReturnAll() { foreach(var item in ObjectsInUse.ToArray()) Return(item); }
        public void Clear() { foreach(var item in ObjectsInUse) Destroy(item);ObjectsInUse.Clear();while(free.Count>0) Destroy(free.Pop()); }
        private static void Destroy(T item) { if(item==null) return;if(Application.isPlaying) UnityEngine.Object.Destroy(item.gameObject);else UnityEngine.Object.DestroyImmediate(item.gameObject); }
    }
}
