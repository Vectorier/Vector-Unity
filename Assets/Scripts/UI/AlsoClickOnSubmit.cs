using Nekki.Vector.GUI.Scenes.Main;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UI
{
    public class AlsoClickOnSubmit : MonoBehaviour, ISubmitHandler
    {
        [SerializeField]
        private UnityEngine.UI.Button _button;
        
        [SerializeField]
        private Toggle _toggle;
        
        public void OnSubmit(BaseEventData eventData)
        {
            AlsoClickButton();
        }

        void AlsoClickButton()
        {
            if (MainScene.Current.EventSystem.currentSelectedGameObject == gameObject)
            {
                if (_button != null)
                    _button.onClick?.Invoke();
                
                if (_toggle != null)
                    _toggle.isOn = !_toggle.isOn;
            }
        }
    }
}
