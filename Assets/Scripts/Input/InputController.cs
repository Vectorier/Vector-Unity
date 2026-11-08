using System;
using UnityEngine;

public class InputController : MonoBehaviour
{
    static InputController _instance;
    static bool _quitting;
    public PlayerInputActions InputActions { get; private set; }

    /// <summary>
    /// The shared input actions holder, created on first use. Returns null while the game is quitting
    /// (or Play mode is exiting) so teardown code never spawns a new object into the closing scene.
    /// </summary>
    public static InputController instance
    {
        get
        {
            if (_instance == null)
            {
                if (_quitting) return null;
                _instance = new GameObject("InputController").AddComponent<InputController>();
            }

            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        _instance = null;
        _quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    private static void OnQuitting() => _quitting = true;

    private void OnDestroy()
    {
        // Destroyed outside of a quit (should not happen: it is DontDestroyOnLoad) also stops re-creation mid-teardown.
        if (_instance == this) _quitting = true;
        InputActions?.Dispose();
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