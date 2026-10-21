using System.Collections;
using UnityEngine;

namespace UI
{
    public sealed class UICoroutineRunner : MonoBehaviour
    {
        private static UICoroutineRunner current;
        public static Coroutine Run(IEnumerator routine)
        {
            if (!Application.isPlaying || routine == null) return null;
            if (current == null) { var go=new GameObject("UI Coroutine Runner"); current=go.AddComponent<UICoroutineRunner>(); DontDestroyOnLoad(go); }
            return current.StartCoroutine(routine);
        }
        public static void Stop(Coroutine routine) { if(current != null && routine != null) current.StopCoroutine(routine); }
        private void OnDestroy() { if(current==this) current=null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void ResetSession() { current=null; }
    }
}
