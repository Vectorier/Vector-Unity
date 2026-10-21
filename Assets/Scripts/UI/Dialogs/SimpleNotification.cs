using UnityEngine;
using UI.Localization;

namespace UI.Dialogs
{
    public class SimpleNotification : Notification
    {
        [SerializeField] private LabelAlias _Text;
        [SerializeField] private ResolutionImage _Image;
        [SerializeField] private RectTransform _ContentAnchor;
        public void Init(Parameters parameters)
        {
            _Parameters=parameters ?? new Parameters();
            if(_Text!=null) _Text.SetAlias(_Parameters.Text);
            if(_Image!=null) { _Image.gameObject.SetActive(!string.IsNullOrEmpty(_Parameters.Image));_Image.SpriteName=_Parameters.Image; }
            if(_Parameters.Content!=null && _ContentAnchor!=null) _Parameters.Content.transform.SetParent(_ContentAnchor,false);
            SetRootPosition();
        }
    }
}
