using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public static class AtlasCache
    {
        private static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        public static Sprite[] GetAtlas(string path)
        {
            if(string.IsNullOrEmpty(path)) return new Sprite[0];
            if(!cache.TryGetValue(path,out var sprites)) { sprites=UIBindings.ResolveAtlas?.Invoke(path) ?? new Sprite[0]; cache[path]=sprites; }
            return sprites;
        }
        public static Sprite GetSpriteFromAtlas(string path,string name) { foreach(var sprite in GetAtlas(path)) if(sprite!=null && sprite.name==name) return sprite; return null; }
        public static void Clear() { cache.Clear(); }
    }
}
