#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace GameDiagnostics
{
    /// <summary>
    /// The game's error log. Every problem met while loading or playing is reported here, traced to its source
    /// (<see cref="GameErrorReport"/>): warnings for what the game left out and went on without, errors for what
    /// could not be loaded at all, and crashes for exceptions nothing expected (also those that only reach Unity's
    /// log). The same problem again is counted, not listed again. Reports are kept for the in-game error log
    /// window (<see cref="ErrorDialog"/>) and appended to two files: logs/game-errors.log (warnings and errors)
    /// and logs/game-crash.log (crashes), next to the game's saves folder.
    /// </summary>
    public static class GameLog
    {
        private const int MaxReports = 1000;
        private const long MaxFileLength = 2 * 1024 * 1024;

        public const string ErrorFileName = "game-errors.log";
        public const string CrashFileName = "game-crash.log";

        private static readonly object Lock = new object();
        private static readonly List<GameErrorReport> _reports = new List<GameErrorReport>();
        private static readonly Dictionary<string, GameErrorReport> _byKey = new Dictionary<string, GameErrorReport>();

        [ThreadStatic] private static bool _writingToUnityLog;
        private static bool _sessionHeaderWritten;
        private static int _dropped;

        /// <summary>A report was added or counted again, or the list cleared (from any thread).</summary>
        public static event Action? Changed;

        /// <summary>A report that should be shown at once (<see cref="GameErrorReport.ShowNow"/>) was added (from any thread).</summary>
        public static event Action<GameErrorReport>? ShowRequested;

        /// <summary>The folder of the log files: next to the saves folder on desktop, the app's data folder on mobile.</summary>
        public static string LogFolder { get; } = Application.isMobilePlatform
            ? Path.Combine(Application.persistentDataPath, "logs")
            : Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "logs");

        /// <summary>This session's reports, oldest first.</summary>
        public static List<GameErrorReport> Reports
        {
            get
            {
                lock (Lock)
                    return new List<GameErrorReport>(_reports);
            }
        }

        public static int Version { get; private set; }

        /// <summary>Reports not looked at yet in the error log window.</summary>
        public static int UnseenCount { get; private set; }
        public static bool UnseenSerious { get; private set; }

        public static void MarkSeen()
        {
            lock (Lock)
            {
                UnseenCount = 0;
                UnseenSerious = false;
            }
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            Application.logMessageReceivedThreaded -= OnUnityLog;
            Application.logMessageReceivedThreaded += OnUnityLog;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandled;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
        }

        // ================= REPORTING ================= //

        /// <summary>Something was wrong; the game left it out and went on.</summary>
        public static GameErrorReport Warning(string stage, string problem, string? outcome = null, string? file = null,
            XmlNode? node = null, string? attribute = null) =>
            Add(GameErrorReport.Warning(stage, problem, outcome, file, node, attribute));

        /// <summary>
        /// An exception the game recovered from (it skipped what failed): a warning traced to its source.
        /// <paramref name="node"/> is the element skipped.
        /// </summary>
        public static GameErrorReport Recovered(Exception exception, string stage, string outcome, XmlNode? node = null) =>
            Add(GameErrorReport.From(exception, stage, GameErrorSeverity.Warning, outcome, node));

        /// <summary>Something could not be loaded at all; <paramref name="show"/> opens the error log window for it.</summary>
        public static GameErrorReport Error(Exception exception, string stage, string? outcome = null, XmlNode? node = null, bool show = true)
        {
            var report = GameErrorReport.From(exception, stage, GameErrorSeverity.Error, outcome, node);
            report.ShowNow = show;
            return Add(report);
        }

        public static GameErrorReport Add(GameErrorReport report)
        {
            bool isNew;
            lock (Lock)
            {
                string key = report.Key;
                if (_byKey.TryGetValue(key, out var existing))
                {
                    existing.Count++;
                    report = existing;
                    isNew = false;
                }
                else if (_reports.Count >= MaxReports)
                {
                    _dropped++;
                    return report;
                }
                else
                {
                    _byKey[key] = report;
                    _reports.Add(report);
                    isNew = true;
                    UnseenCount++;
                    if (report.Severity != GameErrorSeverity.Warning)
                        UnseenSerious = true;
                }
                Version++;
            }

            if (isNew)
            {
                WriteToUnityLog(report);
                WriteToFile(report);
                if (report.ShowNow)
                    ShowRequested?.Invoke(report);
            }
            Changed?.Invoke();
            return report;
        }

        /// <summary>The error log window's Clear: this session's list is emptied (the files keep everything).</summary>
        public static void Clear()
        {
            lock (Lock)
            {
                _reports.Clear();
                _byKey.Clear();
                UnseenCount = 0;
                UnseenSerious = false;
                _dropped = 0;
                Version++;
            }
            Changed?.Invoke();
        }

        /// <summary>Reports left out because the list is full.</summary>
        public static int Dropped
        {
            get
            {
                lock (Lock)
                    return _dropped;
            }
        }

        // ================= UNITY'S LOG ================= //

        private static void WriteToUnityLog(GameErrorReport report)
        {
            _writingToUnityLog = true;
            try
            {
                string text = report.ToText(withDetails: report.Severity != GameErrorSeverity.Warning);
                if (report.Severity == GameErrorSeverity.Warning)
                    Debug.LogWarning(text);
                else
                    Debug.LogError(text);
            }
            finally
            {
                _writingToUnityLog = false;
            }
        }

        /// <summary>Errors that reach Unity's log without passing here (an exception nothing caught, Debug.LogError).</summary>
        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (_writingToUnityLog || type == LogType.Log || type == LogType.Warning)
                return;
            // In the Editor, errors of the Editor's own tools (packages, windows) are not the game's.
            if (Application.isEditor && type != LogType.Exception && (stackTrace == null || !stackTrace.Contains("Assets/")))
                return;
            try
            {
                Add(GameErrorReport.FromLog(condition, stackTrace, type == LogType.Exception));
            }
            catch
            {
                // The log must never fail the game.
            }
        }

        private static void OnUnhandled(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
                Add(GameErrorReport.From(exception, "The game stopped (unhandled error)", GameErrorSeverity.Crash));
        }

        // ================= FILES ================= //

        public static string PathFor(GameErrorSeverity severity) =>
            Path.Combine(LogFolder, severity == GameErrorSeverity.Crash ? CrashFileName : ErrorFileName);

        private static void WriteToFile(GameErrorReport report)
        {
            try
            {
                lock (Lock)
                {
                    Directory.CreateDirectory(LogFolder);
                    if (!_sessionHeaderWritten)
                    {
                        _sessionHeaderWritten = true;
                        string header = $"===== Session started {DateTime.Now:yyyy-MM-dd HH:mm:ss} — {Application.productName} {Application.version}, {Application.platform} ====={Environment.NewLine}";
                        foreach (var file in new[] { PathFor(GameErrorSeverity.Warning), PathFor(GameErrorSeverity.Crash) })
                        {
                            Trim(file);
                            File.AppendAllText(file, Environment.NewLine + header, Encoding.UTF8);
                        }
                    }
                    File.AppendAllText(PathFor(report.Severity), report.ToText() + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _writingToUnityLog = true;
                try
                {
                    Debug.LogWarning("The game's error log could not be written: " + ex.Message);
                }
                finally
                {
                    _writingToUnityLog = false;
                }
            }
        }

        /// <summary>A log past its size limit keeps its newer half.</summary>
        private static void Trim(string file)
        {
            var info = new FileInfo(file);
            if (!info.Exists || info.Length <= MaxFileLength)
                return;
            string all = File.ReadAllText(file);
            int cut = all.IndexOf('\n', all.Length / 2);
            File.WriteAllText(file, "(older entries removed)" + Environment.NewLine + all.Substring(cut + 1), Encoding.UTF8);
        }

        /// <summary>The crash log file's text (empty when there is none).</summary>
        public static string ReadCrashLog()
        {
            try
            {
                lock (Lock)
                {
                    string file = PathFor(GameErrorSeverity.Crash);
                    return File.Exists(file) ? File.ReadAllText(file) : string.Empty;
                }
            }
            catch (Exception ex)
            {
                return "The crash log could not be read: " + ex.Message;
            }
        }
    }
}
