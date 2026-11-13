#nullable enable
using System.Collections.Generic;
using System.Text;
using GameDiagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The in-game error log, drawn by the engine itself (IMGUI) so it works whatever failed to load: menus, a
/// level, a mod's files. Problems the game went past are shown by a small badge in the corner (F8 or a click
/// opens the log); errors that stopped something the player asked for (a level that could not be loaded, the
/// moves) open the log at once. The log lists every report of the session, newest first, filtered by kind,
/// with where each comes from (file, line, element, attribute, what uses it) and what the game did about it;
/// it can copy them and open the log folder. Closing it always goes back to the game: it never quits.
/// </summary>
public class ErrorDialog : MonoBehaviour
{
    // ================= REPORTING (kept for the game's callers) ================= //

    /// <summary>
    /// A content problem with no exception: logged as an error (<paramref name="fatal"/>) or a warning;
    /// <paramref name="show"/> opens the error log for it.
    /// </summary>
    public static void Report(string message, bool fatal = false, bool show = false)
    {
        var report = GameErrorReport.Warning("Game", message);
        if (fatal)
            report.Severity = GameErrorSeverity.Error;
        report.ShowNow = show;
        GameLog.Add(report);
    }

    /// <summary>Shows a message in the error log. The game is never quit for it.</summary>
    public static void Show(string message, bool quitOnClose = false)
    {
        Report(message, true, true);
    }

    /// <summary>Opens the error log (F8 does too), with <paramref name="select"/> selected.</summary>
    public static void Open(GameErrorReport? select = null, string? headline = null)
    {
        EnsureInstance();
        _instance!.OpenWindow(select, headline);
    }

    private static ErrorDialog? _instance;

    // Reports to show, raised from any thread: opened on the next frame.
    private static readonly object PendingLock = new object();
    private static GameErrorReport? _pending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null)
            return;
        _instance = new GameObject("[ErrorLog]").AddComponent<ErrorDialog>();
        DontDestroyOnLoad(_instance.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Hook()
    {
        _instance = null;
        _pending = null;
        GameLog.ShowRequested -= OnShowRequested;
        GameLog.ShowRequested += OnShowRequested;
    }

    private static void OnShowRequested(GameErrorReport report)
    {
        lock (PendingLock)
            _pending = report;
    }

    // ================= STATE ================= //

    private enum Filter { All, Errors, Warnings }

    private static readonly string[] FilterNames = { "All", "Errors", "Warnings" };

    private bool _open;
    private string? _headline;
    private Filter _filter;
    private GameErrorReport? _selected;
    private Vector2 _listScroll, _detailScroll, _crashScroll;
    private bool _showCrashLog;
    private string _crashLog = string.Empty;
    private string? _copied;
    private float _copiedUntil;

    private List<GameErrorReport> _shown = new List<GameErrorReport>();
    private int _shownVersion = -1;
    private Filter _shownFilter;

    private float _previousTimeScale = 1f;
    private EventSystem? _disabledEventSystem;

    private Texture2D? _white;
    private GUIStyle? _panel, _title, _text, _dim, _mono, _row, _rowSelected, _label, _badge, _badgeSerious, _button, _toolbar;
    private float _stylesScale = -1;

    private void Awake()
    {
        _white = new Texture2D(1, 1);
        _white.SetPixel(0, 0, Color.white);
        _white.Apply();
    }

    private void OnDestroy()
    {
        if (_white != null)
            Destroy(_white);
        if (_open)
            CloseWindow();
    }

    private void Update()
    {
        GameErrorReport? pending;
        lock (PendingLock)
        {
            pending = _pending;
            _pending = null;
        }
        if (pending != null)
            OpenWindow(pending, null);

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
        {
            if (_open) CloseWindow();
            else OpenWindow(null, null);
        }
        if (!_open)
            return;

        // Keyboard and gamepad: up / down choose a report, Esc / B / Start go back to the game.
        var gamepad = Gamepad.current;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            || (gamepad != null && (gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame)))
        {
            CloseWindow();
            return;
        }
        int step = 0;
        if ((keyboard != null && keyboard.downArrowKey.wasPressedThisFrame) || (gamepad != null && gamepad.dpad.down.wasPressedThisFrame))
            step = 1;
        if ((keyboard != null && keyboard.upArrowKey.wasPressedThisFrame) || (gamepad != null && gamepad.dpad.up.wasPressedThisFrame))
            step = -1;
        if (step != 0 && _shown.Count > 0)
        {
            int index = Mathf.Clamp(_shown.IndexOf(_selected!) + step, 0, _shown.Count - 1);
            Select(_shown[index]);
        }
        if (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame)
            _filter = (Filter)(((int)_filter + 1) % 3);
    }

    private void OpenWindow(GameErrorReport? select, string? headline)
    {
        RefreshList(force: true);
        if (select != null)
        {
            if (select.Severity == GameErrorSeverity.Warning && _filter == Filter.Errors)
                _filter = Filter.All;
            if (select.Severity != GameErrorSeverity.Warning && _filter == Filter.Warnings)
                _filter = Filter.All;
            RefreshList(force: true);
            Select(select);
            _headline = headline ?? (select.ShowNow ? select.Outcome ?? "Something could not be loaded." : null);
        }
        else
        {
            if (!_open)
                _headline = headline;
            if (_selected == null || !_shown.Contains(_selected))
                Select(_shown.Count > 0 ? _shown[0] : null);
        }
        _showCrashLog = false;
        if (_open)
            return;
        _open = true;
        // The game waits while the log is read; the menus under it do not take clicks meanwhile.
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        var eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.enabled)
        {
            eventSystem.enabled = false;
            _disabledEventSystem = eventSystem;
        }
        GameLog.MarkSeen();
    }

    private void CloseWindow()
    {
        if (!_open)
            return;
        _open = false;
        _headline = null;
        Time.timeScale = _previousTimeScale <= 0f ? 1f : _previousTimeScale;
        if (_disabledEventSystem != null)
            _disabledEventSystem.enabled = true;
        _disabledEventSystem = null;
        GameLog.MarkSeen();
    }

    private void Select(GameErrorReport? report)
    {
        if (_selected != report)
            _detailScroll = Vector2.zero;
        _selected = report;
    }

    private void RefreshList(bool force = false)
    {
        if (!force && _shownVersion == GameLog.Version && _shownFilter == _filter)
            return;
        _shownVersion = GameLog.Version;
        _shownFilter = _filter;
        var all = GameLog.Reports;
        _shown = new List<GameErrorReport>(all.Count);
        for (int i = all.Count - 1; i >= 0; i--) // newest first
        {
            var r = all[i];
            if (_filter == Filter.Errors && r.Severity == GameErrorSeverity.Warning) continue;
            if (_filter == Filter.Warnings && r.Severity != GameErrorSeverity.Warning) continue;
            _shown.Add(r);
        }
    }

    // ================= DRAWING ================= //

    private static readonly Color Back = new Color(0.08f, 0.08f, 0.1f, 0.96f);
    private static readonly Color PanelBack = new Color(0.13f, 0.13f, 0.16f, 1f);
    private static readonly Color RowSelected = new Color(0.25f, 0.32f, 0.45f, 1f);
    private static readonly Color WarningColor = new Color(1f, 0.78f, 0.3f);
    private static readonly Color ErrorColor = new Color(1f, 0.45f, 0.4f);
    private static readonly Color Dim = new Color(0.7f, 0.7f, 0.75f);

    private static Color ColorOf(GameErrorReport r) => r.Severity == GameErrorSeverity.Warning ? WarningColor : ErrorColor;

    private void OnGUI()
    {
        if (!_open && GameLog.UnseenCount == 0)
            return;
        GUI.depth = -100000;
        // Laid out for 1080 lines and scaled to the screen.
        float scale = Mathf.Max(0.5f, Screen.height / 1080f);
        BuildStyles(scale);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale, height = Screen.height / scale;

        if (_open)
            DrawWindow(width, height);
        else
            DrawBadge(height);
        GUI.matrix = Matrix4x4.identity;
    }

    private void DrawBadge(float height)
    {
        int count = GameLog.UnseenCount;
        bool serious = GameLog.UnseenSerious;
        string text = (serious ? "! " : "") + (count == 1 ? "1 problem" : count + " problems") + " — F8 to view";
        var style = serious ? _badgeSerious! : _badge!;
        var size = style.CalcSize(new GUIContent(text));
        var rect = new Rect(16, height - size.y - 16, size.x, size.y);
        if (GUI.Button(rect, text, style))
            OpenWindow(null, null);
    }

    private void DrawWindow(float width, float height)
    {
        RefreshList();
        Fill(new Rect(0, 0, width, height), new Color(0, 0, 0, 0.55f));
        const float margin = 40;
        var window = new Rect(margin, margin, width - margin * 2, height - margin * 2);
        Fill(window, Back);

        GUILayout.BeginArea(new Rect(window.x + 20, window.y + 16, window.width - 40, window.height - 32));

        // Title and counts.
        int errors = 0, warnings = 0;
        foreach (var r in GameLog.Reports)
        {
            if (r.Severity == GameErrorSeverity.Warning) warnings++;
            else errors++;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Error Log", _title);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{errors} error(s) · {warnings} warning(s)" + (GameLog.Dropped > 0 ? $" · {GameLog.Dropped} more not listed" : ""), _dim);
        GUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(_headline))
        {
            var previous = GUI.color;
            GUI.color = ErrorColor;
            GUILayout.Label(_headline, _text);
            GUI.color = previous;
        }
        GUILayout.Space(6);

        // Tabs.
        GUILayout.BeginHorizontal();
        bool crashTab = GUILayout.Toggle(_showCrashLog, "Crash Log", _toolbar, GUILayout.Width(140));
        if (crashTab != _showCrashLog)
        {
            _showCrashLog = crashTab;
            if (_showCrashLog)
                _crashLog = GameLog.ReadCrashLog().TrimEnd();
        }
        GUILayout.Space(12);
        if (!_showCrashLog)
        {
            var filter = (Filter)GUILayout.Toolbar((int)_filter, FilterNames, _toolbar, GUILayout.Width(360));
            if (filter != _filter)
            {
                _filter = filter;
                RefreshList(force: true);
                if (_selected == null || !_shown.Contains(_selected))
                    Select(_shown.Count > 0 ? _shown[0] : null);
            }
        }
        else if (GUILayout.Button("Back to the list", _toolbar, GUILayout.Width(180)))
        {
            _showCrashLog = false;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        float bodyHeight = window.height - 32 - 200;
        if (_showCrashLog)
            DrawCrashLog(bodyHeight);
        else
            DrawReports(window.width - 40, bodyHeight);

        GUILayout.FlexibleSpace();
        DrawButtons();
        GUILayout.EndArea();
    }

    private void DrawReports(float width, float height)
    {
        GUILayout.BeginHorizontal(GUILayout.Height(height));

        // The list, newest first.
        float listWidth = Mathf.Clamp(width * 0.38f, 320, 640);
        GUILayout.BeginVertical(_panel, GUILayout.Width(listWidth), GUILayout.Height(height));
        _listScroll = GUILayout.BeginScrollView(_listScroll);
        if (_shown.Count == 0)
            GUILayout.Label(_filter == Filter.All ? "No problems in this session." : "Nothing of this kind.", _dim);
        foreach (var r in _shown)
        {
            bool selected = ReferenceEquals(r, _selected);
            string count = r.Count > 1 ? $"  ×{r.Count}" : "";
            string head = $"<color=#{ColorUtility.ToHtmlStringRGB(ColorOf(r))}>{r.SeverityName}</color>  {r.Time:HH:mm:ss}{count}  <color=#B0B0B8>{r.Stage}</color>";
            if (GUILayout.Button(head + "\n" + Truncate(r.Summary, 180), selected ? _rowSelected : _row))
                Select(r);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // The selected report.
        GUILayout.BeginVertical(_panel, GUILayout.Height(height));
        _detailScroll = GUILayout.BeginScrollView(_detailScroll);
        if (_selected != null)
            DrawDetails(_selected);
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawDetails(GameErrorReport r)
    {
        var previous = GUI.color;
        GUI.color = ColorOf(r);
        GUILayout.Label(r.SeverityName + (r.Count > 1 ? $" (happened {r.Count} times)" : ""), _label);
        GUI.color = previous;

        Field("Problem", r.Problem);
        Field("What the game did", r.Outcome);
        Field("While", r.Stage);
        Field("File", r.ShortFile);
        Field("Position", r.Line > 0 ? $"Line {r.Line}, column {r.Column}" : null);
        Field("Element", r.ElementPath);
        Field("Used by", r.UsedBy);
        Field("Attribute", r.Attribute == null ? null : r.Value != null ? $"{r.Attribute} = \"{r.Value}\"" : r.Attribute);
        Field("Code", r.CodeLocation == null ? null : r.ExceptionType != null ? $"{r.CodeLocation} ({r.ExceptionType})" : r.CodeLocation);
        if (!string.IsNullOrEmpty(r.Details))
        {
            GUILayout.Space(8);
            GUILayout.Label("Technical details", _label);
            // A text area so the text can be selected and copied.
            GUILayout.TextArea(r.Details, _mono);
        }
    }

    private void Field(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, _dim, GUILayout.Width(190));
        GUILayout.Label(value, _text);
        GUILayout.EndHorizontal();
    }

    private void DrawCrashLog(float height)
    {
        GUILayout.BeginVertical(_panel, GUILayout.Height(height));
        _crashScroll = GUILayout.BeginScrollView(_crashScroll);
        if (_crashLog.Length == 0)
            GUILayout.Label("No crashes have been logged.", _dim);
        else
            GUILayout.TextArea(_crashLog, _mono);
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawButtons()
    {
        GUILayout.Label("Saved in " + GameLog.LogFolder, _dim);
        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (_selected != null && GUILayout.Button("Copy", _button, GUILayout.Width(150)))
            Copy(_selected.ToText(), "Copied.");
        if (GUILayout.Button("Copy All", _button, GUILayout.Width(150)))
        {
            var sb = new StringBuilder();
            foreach (var r in _shown)
                sb.AppendLine(r.ToText());
            Copy(sb.ToString(), "All copied.");
        }
        if (!Application.isMobilePlatform && GUILayout.Button("Open Log Folder", _button, GUILayout.Width(220)))
        {
            System.IO.Directory.CreateDirectory(GameLog.LogFolder);
            Application.OpenURL("file:///" + GameLog.LogFolder.Replace('\\', '/'));
        }
        if (GUILayout.Button("Clear", _button, GUILayout.Width(150)))
        {
            GameLog.Clear();
            RefreshList(force: true);
            Select(null);
        }
        if (_copied != null && Time.unscaledTime < _copiedUntil)
            GUILayout.Label(_copied, _dim);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Continue (Esc)", _button, GUILayout.Width(220)))
            CloseWindow();
        GUILayout.EndHorizontal();
    }

    private void Copy(string text, string message)
    {
        GUIUtility.systemCopyBuffer = text;
        _copied = message;
        _copiedUntil = Time.unscaledTime + 2f;
    }

    private static string Truncate(string text, int length) => text.Length <= length ? text : text.Substring(0, length - 1) + "…";

    private void Fill(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, _white);
        GUI.color = previous;
    }

    private Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    /// <summary>The corner badge: a solid, readable pill in the color of the worst unseen problem.</summary>
    private GUIStyle Badge(Color color)
    {
        var lighter = Color.Lerp(color, Color.white, 0.25f);
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(16, 16, 8, 8),
            normal = { textColor = Color.black, background = Solid(color) },
            hover = { textColor = Color.black, background = Solid(lighter) },
            active = { textColor = Color.black, background = Solid(lighter) },
        };
    }

    private void BuildStyles(float scale)
    {
        if (_panel != null && Mathf.Approximately(_stylesScale, scale))
            return;
        _stylesScale = scale;
        var skin = GUI.skin;
        _panel = new GUIStyle(skin.box) { normal = { background = Solid(PanelBack) }, padding = new RectOffset(10, 10, 10, 10) };
        _title = new GUIStyle(skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        _text = new GUIStyle(skin.label) { fontSize = 18, wordWrap = true, richText = false, normal = { textColor = Color.white } };
        _dim = new GUIStyle(_text) { normal = { textColor = Dim } };
        _label = new GUIStyle(_text) { fontStyle = FontStyle.Bold };
        _mono = new GUIStyle(skin.textArea)
        {
            fontSize = 14,
            wordWrap = true,
            normal = { textColor = new Color(0.85f, 0.85f, 0.9f), background = Solid(new Color(0.06f, 0.06f, 0.08f)) },
            focused = { textColor = new Color(0.9f, 0.9f, 0.95f), background = Solid(new Color(0.06f, 0.06f, 0.08f)) },
            padding = new RectOffset(8, 8, 6, 6),
        };
        var rowBack = Solid(new Color(0, 0, 0, 0));
        var rowHover = Solid(new Color(1, 1, 1, 0.06f));
        _row = new GUIStyle(skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            richText = true,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 0, 2),
            normal = { textColor = Color.white, background = rowBack },
            hover = { textColor = Color.white, background = rowHover },
            active = { textColor = Color.white, background = rowHover },
        };
        _rowSelected = new GUIStyle(_row) { normal = { textColor = Color.white, background = Solid(RowSelected) }, hover = { textColor = Color.white, background = Solid(RowSelected) } };
        _button = new GUIStyle(skin.button) { fontSize = 18, fixedHeight = 44 };
        _toolbar = new GUIStyle(skin.button) { fontSize = 16, fixedHeight = 36 };
        _badge = Badge(WarningColor);
        _badgeSerious = Badge(ErrorColor);
    }
}
