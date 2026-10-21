using System;
using DG.Tweening;
using Nekki.Vector.Core.Controllers;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIController : MonoBehaviour
{
    
    [SerializeField] TutorialKeyboard _keyboard;
    
    [SerializeField] TutorialGamepad _gamepad;

    [SerializeField] private GameObject _GB;

    [SerializeField] private GameObject _root;

    private Vector2 _defaultHandPosition;

    private Sequence _sequence;

    private void Awake()
    {
        // GamepadController.Instance.OnGamepadConnected += () =>
        // {
        //     _keyboard.gameObject.SetActive(false);
        // };
        // GamepadController.Instance.OnGamepadDisconnected += () =>
        // {
        //     _keyboard.gameObject.SetActive(true);
        // };
    }

    public void SetVisible(bool value)
    {
        if (_root != null)
        {
            _root.SetActive(value);
        }

        if (_GB != null)
        {
            _GB.SetActive(value);
        }
    }

    public void ShowKey(KeyVariables key, string description)
    {
        if (!GamepadController.Instance.IsGamepadConnected)
        {
            _gamepad.gameObject.SetActive(false);
            _keyboard.gameObject.SetActive(true);
            _keyboard.Show(key.Key, GetDescription(description));
        }
        else
        {
            _keyboard.gameObject.SetActive(false);
            _gamepad.gameObject.SetActive(true);
            _gamepad.Show(key.Key, GetDescription(description));
        }
    }

    private string GetDescription(string description)
    {
        return LocalizationManager.Instance.GetTranslation(description);
    }

    public void Reset()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }
    }
}