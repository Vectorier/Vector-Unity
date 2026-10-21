using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Localization
{
    [AddComponentMenu("UI/Localized Label")]
    public class LabelAlias : Text
    {
        public enum Type { Content, Title, Button }
        public bool UseLocalizationFont=true;
        public Type FontType;
        [TextArea(4,10)] public string Alias=string.Empty;
        [SerializeField] private bool _ToUpperCase;
        private TextAnimator animator;
        public bool ToUpperCase { get=>_ToUpperCase; set { _ToUpperCase=value; RefreshLocalization(); } }
        public string Text { get=>text; set { text=value ?? string.Empty; RefreshFont(); } }
        public float Alpha { get=>color.a; set { var c=color; c.a=value; color=c; } }
        protected override void Awake() { base.Awake(); animator=GetComponent<TextAnimator>(); if(animator!=null) animator.Target=this; UIBindings.Refreshed+=RefreshLocalization; RefreshLocalization(); }
        protected override void OnDestroy() { UIBindings.Refreshed-=RefreshLocalization; base.OnDestroy(); }
        public void SetAlias(string value) { Alias=value ?? string.Empty;if(Alias.Length==0) { animator?.Stop();text=string.Empty;RefreshFont(); }else RefreshLocalization(); }
        public void RefreshLocalization()
        {
            if(!string.IsNullOrEmpty(Alias))
            {
                string value=Regex.Replace(Alias,@"\^([^\^]+)\^",m=>UIBindings.Phrase(m.Groups[1].Value)).TrimStart(' ');
                if(_ToUpperCase) value=value.ToUpperInvariant();
                if(animator!=null && Application.isPlaying) animator.Play(value); else text=value;
            }
            RefreshFont();
        }
        private void RefreshFont() { if(UseLocalizationFont) { var resolved=UIBindings.ResolveFont?.Invoke(FontType.ToString()); if(resolved!=null) font=resolved; } }
    }
}
