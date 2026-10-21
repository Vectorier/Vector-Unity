using System;
using UnityEngine;

namespace UI
{
    /// <summary>Optional host services. Assign callbacks before opening your UI, then call Refresh.</summary>
    public static class UIBindings
    {
        public static Func<string,string> Localize;
        public static Func<string,Font> ResolveFont;
        public static Func<string,Sprite> ResolveSprite;
        public static Func<string,Sprite[]> ResolveAtlas;
        public static Func<string,float?> RemainingSeconds;
        public static Func<bool> UseLowResolution;
        public static Action<string> PlaySound;
        public static Action<string> StopSound;
        public static Action<bool> InputBlockingChanged;
        public static Action<bool> BackgroundEffectChanged;
        public static event Action Refreshed;

        public static string Phrase(string key) { return Localize?.Invoke(key) ?? key ?? string.Empty; }
        public static void Refresh() { AtlasCache.Clear(); Refreshed?.Invoke(); }
        public static void Reset()
        {
            Localize=null; ResolveFont=null; ResolveSprite=null; ResolveAtlas=null;
            RemainingSeconds=null; UseLowResolution=null; PlaySound=null; StopSound=null;
            InputBlockingChanged=null; BackgroundEffectChanged=null;
            // Keep subscriptions of live views; each view unsubscribes on destruction.
            Refresh();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void NewSession() { Refreshed=null; Reset(); }
    }
}
