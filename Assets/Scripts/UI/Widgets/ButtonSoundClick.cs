using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class ButtonSoundClick : MonoBehaviour, ISubmitHandler, IPointerUpHandler
    {
        [SerializeField] private string _SoundAlias;
        
        public string SoundAlias
        {
            get => _SoundAlias;
            set => _SoundAlias = value;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(_SoundAlias)) SoundsManager.Instance.PlaySounds(_SoundAlias);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!string.IsNullOrEmpty(_SoundAlias)) SoundsManager.Instance.PlaySounds(_SoundAlias);
        }
    }
}