using System;
using UnityEngine;

public class InputController : MonoBehaviour
{
    static InputController _instance;
    public PlayerInputActions InputActions { get; private set; }

    public static InputController instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("InputController").AddComponent<InputController>();
            }

            return _instance;
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(this);
        InputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        InputActions.Enable();
    }

    private void OnDisable()
    {
        InputActions.Disable();
    }

    public void Enable()
    {
        InputActions.Enable();
    }

    public void Disable()
    {
        InputActions.Disable();
    }
}