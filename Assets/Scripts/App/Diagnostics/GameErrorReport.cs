#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace GameDiagnostics
{
    public enum GameErrorSeverity
    {
        /// <summary>Something was wrong, the game left it out and went on (a move, a trigger, an image).</summary>
        Warning,
        /// <summary>Something could not be loaded at all (a level, moves.xml).</summary>
        Error,
        /// <summary>An error nothing expected (an exception that reached Unity's log).</summary>
        Crash,
    }

    /// <summary>
    /// One problem of the game, traced to its source as far as it can be: what the game was doing, what is wrong,
    /// the file, the element with its line, the attribute, what uses that element, and what the game did about it;
    /// the exception and its stack for the developer.
    /// </summary>
    public sealed class GameErrorReport
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public GameErrorSeverity Severity { get; set; } = GameErrorSeverity.Error;

        /// <summary>What the game was doing: "Loading the moves", "Loading the level ...".</summary>
        public string Stage { get; set; } = string.Empty;

        /// <summary>What is wrong, in a sentence.</summary>
        public string Problem { get; set; } = string.Empty;

        /// <summary>What the game did about it: "The move was skipped.", "The level was not started.".</summary>
        public string? Outcome { get; set; }

        public string? File { get; set; }

        /// <summary>The element, e.g. &lt;MonkeyVaultStart FileName="monkey_vault.bin"&gt;.</summary>
        public string? Element { get; set; }

        /// <summary>Where the element is: root › Moves › &lt;Run&gt; › Interval #2.</summary>
        public string? ElementPath { get; set; }

        /// <summary>What uses the element, when it is a shared definition (an event group, a set's object).</summary>
        public string? UsedBy { get; set; }

        public string? Attribute { get; set; }

        /// <summary>The attribute's value as written.</summary>
        public string? Value { get; set; }

        public int Line { get; set; }
        public int Column { get; set; }

        /// <summary>The game's code that met the error (Type.Method).</summary>
        public string? CodeLocation { get; set; }

        public string? ExceptionType { get; set; }

        /// <summary>The exception in full (message, inner exceptions, stack).</summary>
        public string? Details { get; set; }

        /// <summary>The error log window opens for it at once (the game could not go on as asked: a level, moves.xml).</summary>
        public bool ShowNow { get; set; }

        /// <summary>How many times the same problem happened (it is listed once).</summary>
        public int Count { get; set; } = 1;

        /// <summary>One line: the problem and where it is.</summary>
        public string Summary
        {
            get
            {
                var where = new List<string>();
                if (File != null)
                    where.Add(Line > 0 ? $"{ShortFile}:{Line}" : ShortFile!);
                if (Element != null)
                    where.Add(Element);
                if (Attribute != null)
                    where.Add("attribute \"" + Attribute + "\"");
                return where.Count > 0 ? $"{Problem} ({string.Join(", ", where)})" : Problem;
            }
        }

        /// <summary>The file from the game's content folder on (StreamingAssets/...), not the whole path.</summary>
        public string? ShortFile
        {
            get
            {
                if (File == null)
                    return null;
                string path = File.Replace('\\', '/');
                int at = path.IndexOf("StreamingAssets/", StringComparison.OrdinalIgnoreCase);
                return at >= 0 ? path.Substring(at + "StreamingAssets/".Length) : path;
            }
        }

        /// <summary>What identifies the same problem happening again (it is then counted, not listed again).</summary>
        public string Key => FromUnityLog && ExceptionType != null
            // The same exception thrown by the same code (e.g. once per texture) is one entry, counted.
            ? Severity + "|" + Stage + "|" + ExceptionType + "|" + CodeLocation
            : Severity + "|" + Stage + "|" + Summary + "|" + CodeLocation;

        /// <summary>The report came from Unity's log (an exception nothing caught), not from the game's own checks.</summary>
        public bool FromUnityLog { get; set; }

        // ================= BUILDING ================= //

        /// <summary>A problem found without an exception.</summary>
        public static GameErrorReport Warning(string stage, string problem, string? outcome = null, string? file = null,
            XmlNode? node = null, string? attribute = null)
        {
            var (line, column) = XmlSource.Locate(node);
            return new GameErrorReport
            {
                Severity = GameErrorSeverity.Warning,
                Stage = stage,
                Problem = problem,
                Outcome = outcome,
                File = file ?? XmlSource.FileOf(node),
                Element = node != null ? XmlSource.Describe(node) : null,
                ElementPath = node != null ? XmlSource.PathOf(node) : null,
                Attribute = attribute,
                Value = attribute != null ? node?.Attributes?[attribute]?.Value : null,
                Line = line,
                Column = column,
            };
        }

        /// <summary>
        /// Traces <paramref name="exception"/> to the game data it came from. <paramref name="node"/> is the element
        /// being read when it was caught, used when no inner element was noted.
        /// </summary>
        public static GameErrorReport From(Exception exception, string stage, GameErrorSeverity severity = GameErrorSeverity.Error,
            string? outcome = null, XmlNode? node = null)
        {
            var root = Unwrap(exception);
            var data = root as GameDataException ?? Find<GameDataException>(exception);
            var xml = root as XmlException ?? Find<XmlException>(exception);

            XmlNode? element = data?.Node ?? XmlSource.NodeOf(root) ?? NotedNode(exception) ?? node;
            string? file = data?.File ?? XmlSource.FileOf(element);
            string? attribute = data?.Attribute ?? GuessAttribute(root, element);

            XmlNode? user = element != null ? XmlSource.UserOf(root, element) : null;
            string? usedBy = null;
            if (user != null)
            {
                string? userFile = XmlSource.FileOf(user);
                usedBy = userFile != null && userFile != file ? Shorten(userFile) + ": " + XmlSource.PathOf(user) : XmlSource.PathOf(user);
            }

            int line = data?.Line ?? 0, column = data?.Column ?? 0;
            if (line == 0 && xml != null)
            {
                line = xml.LineNumber;
                column = xml.LinePosition;
            }
            if (line == 0 && element != null)
                (line, column) = XmlSource.Locate(element);

            return new GameErrorReport
            {
                Severity = severity,
                Stage = stage,
                Problem = data?.Problem ?? Explain(root, xml),
                Outcome = outcome,
                File = file,
                Element = element != null ? XmlSource.Describe(element) : null,
                ElementPath = element != null ? XmlSource.PathOf(element) : null,
                UsedBy = usedBy,
                Attribute = attribute,
                Value = attribute != null ? (element as XmlElement)?.Attributes[attribute]?.Value : null,
                Line = line,
                Column = column,
                CodeLocation = CodeLocationOf(root),
                ExceptionType = root.GetType().FullName,
                Details = exception.ToString(),
            };
        }

        /// <summary>An error that only reached Unity's log (its text and stack, no exception object).</summary>
        public static GameErrorReport FromLog(string condition, string stackTrace, bool isException)
        {
            string problem = condition;
            string? type = null;
            // "NullReferenceException: Object reference not set..." → the type and the message.
            int colon = condition.IndexOf(": ", StringComparison.Ordinal);
            if (isException && colon > 0 && condition.Substring(0, colon).EndsWith("Exception", StringComparison.Ordinal))
            {
                type = condition.Substring(0, colon);
                problem = condition.Substring(colon + 2);
                if (type.EndsWith("NullReferenceException", StringComparison.Ordinal))
                    problem = "The game used something that is not there (a missing object, model or value): " + problem;
            }
            return new GameErrorReport
            {
                FromUnityLog = true,
                Severity = isException ? GameErrorSeverity.Crash : GameErrorSeverity.Error,
                Stage = isException ? "Running the game (unhandled error)" : "Game log",
                Problem = problem,
                Outcome = isException ? "The game went on; what this code was doing this frame did not finish." : null,
                ExceptionType = type,
                CodeLocation = CodeLocationOf(stackTrace),
                Details = string.IsNullOrEmpty(stackTrace) ? condition : condition + "\n" + stackTrace.TrimEnd(),
            };
        }

        private static string Shorten(string path)
        {
            path = path.Replace('\\', '/');
            int at = path.IndexOf("StreamingAssets/", StringComparison.OrdinalIgnoreCase);
            return at >= 0 ? path.Substring(at + "StreamingAssets/".Length) : path;
        }

        // ================= TRACING ================= //

        private static Exception Unwrap(Exception exception)
        {
            while (true)
            {
                switch (exception)
                {
                    case AggregateException aggregate when aggregate.InnerExceptions.Count > 0:
                        exception = aggregate.InnerExceptions[0];
                        continue;
                    case System.Reflection.TargetInvocationException { InnerException: { } inner }:
                        exception = inner;
                        continue;
                    case TypeInitializationException { InnerException: { } inner }:
                        exception = inner;
                        continue;
                    default:
                        return exception;
                }
            }
        }

        private static T? Find<T>(Exception exception) where T : Exception
        {
            for (Exception? e = exception; e != null; e = e.InnerException)
                if (e is T found)
                    return found;
            return null;
        }

        private static XmlNode? NotedNode(Exception exception)
        {
            for (Exception? e = exception; e != null; e = e.InnerException)
                if (XmlSource.NodeOf(e) is { } node)
                    return node;
            return null;
        }

        private static readonly Regex QuotedValue = new Regex("'([^']*)'", RegexOptions.Compiled);

        /// <summary>
        /// A value that could not be read: the exception may quote it ("Input string 'abc' was not in a correct
        /// format", "The given key 'x' was not present"); the element's attribute holding it is the one at fault.
        /// </summary>
        private static string? GuessAttribute(Exception exception, XmlNode? node)
        {
            if (node?.Attributes == null || node.Attributes.Count == 0)
                return null;
            if (!(exception is FormatException || exception is OverflowException || exception is KeyNotFoundException || exception is ArgumentException))
                return null;
            var match = QuotedValue.Match(exception.Message);
            if (!match.Success)
                return exception is FormatException ? OnlyNonNumber(node) : null;
            string value = match.Groups[1].Value;
            XmlAttribute? contains = null;
            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.Value == value)
                    return attribute.Name;
                if (value.Length > 0 && contains == null && attribute.Value.Contains(value))
                    contains = attribute;
            }
            return contains?.Name;
        }

        // Attributes that hold names or text, never numbers.
        private static readonly HashSet<string> TextAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "FileName", "Groups", "TriggerName", "ItemName", "Key", "Template", "Choice", "Variant", "Prefix",
            "ClassName", "Type", "Value", "Text", "Music", "Skin", "Skins", "Spawn", "BirthSpawn", "PivotNode", "Node",
            "Nodes", "Statistic", "Texture", "AI", "Icon", "Description", "Animation", "Sound",
        };

        /// <summary>
        /// A number that could not be read, with no value in the message (Unity's runtime does not quote it): the one
        /// attribute of the element whose value is not made of numbers is the likely cause.
        /// </summary>
        private static string? OnlyNonNumber(XmlNode node)
        {
            string? found = null;
            foreach (XmlAttribute attribute in node.Attributes!)
            {
                if (TextAttributes.Contains(attribute.Name))
                    continue;
                foreach (var part in attribute.Value.Split('|', ',', ';'))
                {
                    string p = part.Trim();
                    if (p.Length == 0 || double.TryParse(p, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                        continue;
                    if (found != null)
                        return null; // more than one: not sure which
                    found = attribute.Name;
                    break;
                }
            }
            return found;
        }

        private static string Explain(Exception exception, XmlException? xml)
        {
            if (xml != null)
                return "The file is not valid XML: " + xml.Message;
            switch (exception)
            {
                case NullReferenceException _:
                    return "A required element or attribute is missing (the game read a value that is not there).";
                case FormatException _:
                    return "A value is not written in the expected format: " + exception.Message;
                case OverflowException _:
                    return "A number is too large or too small: " + exception.Message;
                case KeyNotFoundException _:
                    return "Something is referred to by a name that does not exist: " + exception.Message;
                case IndexOutOfRangeException _:
                case ArgumentOutOfRangeException _:
                    return "A list has fewer entries than the data needs (a value with too few parts, a move's frames...): " + exception.Message;
                case System.IO.EndOfStreamException _:
                    return "A binary file ends too early (it is cut short or not the expected format).";
                case InvalidCastException _:
                    return "A value has the wrong type: " + exception.Message;
                default:
                    return exception.Message;
            }
        }

        private static string? CodeLocationOf(Exception exception)
        {
            try
            {
                foreach (var frame in new StackTrace(exception, false).GetFrames() ?? Array.Empty<StackFrame>())
                {
                    var method = frame.GetMethod();
                    var type = method?.DeclaringType;
                    while (type?.DeclaringType != null && type.Name.StartsWith("<", StringComparison.Ordinal))
                        type = type.DeclaringType;
                    if (type == null || method == null || IsInfrastructure(type))
                        continue;
                    string name = method.Name;
                    int open = name.IndexOf('<'), close = name.IndexOf('>');
                    if (open >= 0 && close > open + 1)
                        name = name.Substring(open + 1, close - open - 1);
                    return type.Name + "." + name;
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>System, Unity and this error log's own frames are not where the game's error is.</summary>
        private static bool IsInfrastructure(Type type)
        {
            string ns = type.Namespace ?? string.Empty;
            return ns == "GameDiagnostics" || ns.StartsWith("System", StringComparison.Ordinal)
                || ns.StartsWith("UnityEngine", StringComparison.Ordinal) || ns.StartsWith("Mono", StringComparison.Ordinal)
                || type == typeof(XmlUtils) || type == typeof(ErrorDialog);
        }

        /// <summary>The first frame of a Unity stack text in the game's own code ("Type:Method (...)").</summary>
        private static string? CodeLocationOf(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return null;
            foreach (var raw in stackTrace.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("UnityEngine.", StringComparison.Ordinal) || line.StartsWith("System.", StringComparison.Ordinal)
                    || line.StartsWith("GameDiagnostics.", StringComparison.Ordinal) || line.StartsWith("ErrorDialog", StringComparison.Ordinal))
                    continue;
                int paren = line.IndexOf(" (", StringComparison.Ordinal);
                string call = paren > 0 ? line.Substring(0, paren) : line;
                int at = line.IndexOf("(at ", StringComparison.Ordinal);
                if (at > 0)
                {
                    string source = line.Substring(at + 4).TrimEnd(')');
                    int slash = source.LastIndexOf('/');
                    call += " — " + (slash >= 0 ? source.Substring(slash + 1) : source);
                }
                return call;
            }
            return null;
        }

        // ================= TEXT ================= //

        public string SeverityName => Severity == GameErrorSeverity.Warning ? "Warning" : Severity == GameErrorSeverity.Crash ? "Crash" : "Error";

        /// <summary>The report as the log files and Copy write it.</summary>
        public string ToText(bool withDetails = true)
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(Time.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ").Append(SeverityName.ToUpperInvariant());
            if (Stage.Length > 0)
                sb.Append(" — ").Append(Stage);
            if (Count > 1)
                sb.Append(" (×").Append(Count).Append(')');
            sb.AppendLine();
            sb.Append("  Problem:   ").AppendLine(Problem);
            if (Outcome != null)
                sb.Append("  Outcome:   ").AppendLine(Outcome);
            if (File != null)
                sb.Append("  File:      ").AppendLine(Line > 0 ? $"{ShortFile} (line {Line}, column {Column})" : ShortFile);
            if (ElementPath != null)
                sb.Append("  Element:   ").AppendLine(ElementPath);
            if (UsedBy != null)
                sb.Append("  Used by:   ").AppendLine(UsedBy);
            if (Attribute != null)
                sb.Append("  Attribute: ").AppendLine(Value != null ? $"{Attribute} = \"{Value}\"" : Attribute);
            if (CodeLocation != null)
                sb.Append("  Code:      ").AppendLine(ExceptionType != null ? $"{CodeLocation} ({ExceptionType})" : CodeLocation);
            if (withDetails && Details != null)
            {
                sb.AppendLine("  Details:");
                foreach (var line in Details.Split('\n'))
                    sb.Append("    ").AppendLine(line.TrimEnd('\r'));
            }
            return sb.ToString();
        }

        public override string ToString() => Summary;
    }
}
