using System.Collections.Generic;
using Nekki.Vector.GUI.Scenes.Main;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UI.Dialogs
{
    public class BaseDialog : MonoBehaviour
    {
        [SerializeField] private bool _Modal = true;

        [SerializeField] private GameObject _SelectedGameObjectOnShow;


        public GameObject SelectedGameObjectOnShow => _SelectedGameObjectOnShow;

        public bool Modal
        {
            get => _Modal;
            set => _Modal = value;
        }

        public static int OpenedDialogsCount => DialogCoordinator.OpenDialogCount;

        public void Show(bool moveToFront)
        {
            if (moveToFront) transform.SetAsLastSibling();
            Show();
        }

        public virtual void Show()
        {
            EventSystem.current.GetComponent<InputSystemUIInputModule>().cancel.action.performed += BackPress;
            DialogCoordinator.Show(this, _Modal);
            if (MainScene.Current != null)
            {
                MainScene.Current.EventSystem.SetSelectedGameObject(SelectedGameObjectOnShow);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(SelectedGameObjectOnShow);
            }
        }

        public virtual void Dismiss()
        {
            EventSystem.current.GetComponent<InputSystemUIInputModule>().cancel.action.performed -= BackPress;
            DialogCoordinator.Dismiss(this);
            gameObject.SetActive(false);

            if (Scene<MainScene>.Current != null)
            {
                MainScene.Current.EventSystem.SetSelectedGameObject(Scene<MainScene>.Current.CurrentScreen
                    .SelectedObjectOnActivation);
            }
        }

        protected virtual void OnDisable()
        {
            DialogCoordinator.Dismiss(this);
        }

        protected virtual void OnDestroy()
        {
            DialogCoordinator.Dismiss(this);
            DialogCoordinator.Forget(this);
        }

        protected virtual void BackPress(InputAction.CallbackContext ctx = default)
        {
            Dismiss();
        }

        public void SetButtonsEnabled(bool enabled)
        {
            foreach (var image in GetComponentsInChildren<Image>(true)) image.raycastTarget = enabled;
        }

        public List<Button> GetButtonsByName(string name)
        {
            var result = new List<Button>();
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (name == null || button.name == name)
                    result.Add(button);
            return result;
        }
    }
}