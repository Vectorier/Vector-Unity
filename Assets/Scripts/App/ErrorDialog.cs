using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Last-resort, engine-drawn message box for content errors (missing or malformed level/mod files)
/// that would otherwise fail silently. Every report is logged; only reports with
/// <c>show</c> set are displayed to the player.
/// </summary>
public class ErrorDialog : MonoBehaviour
{
    private static readonly List<string> _messages = new List<string>();

    private static ErrorDialog _instance;

    private static bool _quitOnClose;

    private Texture2D _background;

    public static void Report(string message, bool fatal = false, bool show = false)
    {
        if (fatal)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.LogWarning(message);
        }
        if (show)
        {
            Show(message, fatal);
        }
    }

    public static void Show(string message, bool quitOnClose)
    {
        if (!_messages.Contains(message))
        {
            _messages.Add(message);
        }
        _quitOnClose |= quitOnClose;
        if (_instance == null)
        {
            _instance = new GameObject("[ErrorDialog]").AddComponent<ErrorDialog>();
            DontDestroyOnLoad(_instance.gameObject);
        }
        Time.timeScale = 0f;
    }

    private void Awake()
    {
        _background = new Texture2D(1, 1);
        _background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.8f));
        _background.Apply();
    }

    private void OnDestroy()
    {
        Destroy(_background);
    }

    private void Close()
    {
        if (_quitOnClose)
        {
            Application.Quit();
            return;
        }
        _messages.Clear();
        Time.timeScale = 1f;
        Destroy(gameObject);
        _instance = null;
    }

    private void OnGUI()
    {
        GUI.depth = -100000;
        float width = Screen.width;
        float height = Screen.height;
        int fontSize = Mathf.RoundToInt(Mathf.Max(width, height) / 40f);

        GUI.DrawTexture(new Rect(0f, 0f, width, height), _background);

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = fontSize,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(width / 10f, height / 8f, width * 0.8f, height * 0.6f), string.Join("\n\n", _messages), labelStyle);

        var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
        if (GUI.Button(new Rect(width / 10f, height * 0.75f, width * 0.8f, height / 8f), "OK", buttonStyle))
        {
            Close();
        }
    }
}
