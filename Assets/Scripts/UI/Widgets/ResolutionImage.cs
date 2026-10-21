using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [AddComponentMenu("UI/Resolved Image")]
    public class ResolutionImage : Image
    {
        public const string LowQualitySuffix="_low";
        [SerializeField] private string _TexturePath=string.Empty;
        [SerializeField] private string _SpriteName=string.Empty;
        private bool selectedLowResolution;
        public string TexturePath { get=>_TexturePath; set { _TexturePath=value ?? string.Empty; RefreshSprite(); } }
        public string SpriteName { get=>_SpriteName; set { _SpriteName=value ?? string.Empty; RefreshSprite(); } }
        public float Alpha { get=>color.a; set { var c=color; c.a=value; color=c; } }
        protected override void Awake() { base.Awake(); UIBindings.Refreshed+=RefreshSprite; RefreshSprite(); }
        protected override void OnDestroy() { UIBindings.Refreshed-=RefreshSprite; base.OnDestroy(); }
        public void RefreshSprite()
        {
            if(string.IsNullOrEmpty(_SpriteName)) return;
            bool low=UIBindings.UseLowResolution?.Invoke()==true;
            Sprite found=null;
            if(low) found=UIBindings.ResolveSprite?.Invoke(_TexturePath+_SpriteName+LowQualitySuffix);
            selectedLowResolution=found!=null;
            if(found==null) found=UIBindings.ResolveSprite?.Invoke(_TexturePath+_SpriteName);
            if(found==null && _SpriteName.Contains("."))
            {
                string atlas=_SpriteName.Split('.')[0];
                if(low) found=AtlasCache.GetSpriteFromAtlas(_TexturePath+atlas+LowQualitySuffix,_SpriteName);
                selectedLowResolution=found!=null;
                if(found==null) found=AtlasCache.GetSpriteFromAtlas(_TexturePath+atlas,_SpriteName);
            }
            if(found!=null) sprite=found;
        }
        public override void SetNativeSize() { base.SetNativeSize(); if(selectedLowResolution) rectTransform.sizeDelta*=2f; }
    }
}
