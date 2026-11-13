using System;
using System.IO;
using System.Xml;
using GameDiagnostics;
using UnityEngine;

public static class XmlUtils
{
    public enum OpenXmlType
    {
        Normal = 0,
        ForcedResourced = 1,
        ForcedExternal = 2
    }

    public const string Comment = "#comment";

    /// <summary>Returns the element's Properties node when it holds Dynamic (transformation) data, otherwise null.</summary>
    public static XmlNode GetDynamicProperties(XmlNode node)
    {
        var properties = node?["Properties"];
        return properties?["Dynamic"] != null ? properties : null;
    }

    public static int ParseInt(this XmlAttribute attr, int defVal = 0)
    {
        if (attr == null || !int.TryParse(attr.Value, out int result))
        {
            return defVal;
        }
        return result;
    }

    public static float ParseFloat(this XmlAttribute attr, float defVal = 0f)
    {
        if (attr == null || !float.TryParse(attr.Value, out float result))
        {
            return defVal;
        }
        return result;
    }

    public static double ParseDouble(this XmlAttribute attr, double defVal = 0.0)
    {
        if (attr == null || !double.TryParse(attr.Value, out double result))
        {
            return defVal;
        }
        return result;
    }

    public static bool ParseBool(this XmlAttribute attr, bool defVal = false)
    {
        if (attr == null || !int.TryParse(attr.Value, out int result))
        {
            return defVal;
        }
        return result > 0;
    }

    public static string ParseString(this XmlAttribute attr, string defVal = null)
    {
        if (attr == null)
        {
            return defVal;
        }
        return attr.Value;
    }

    /// <summary>
    /// The file's XML, or null when it is missing or is not valid XML: the game goes on without it and the problem
    /// is logged with the file, line and column (<paramref name="dialog"/> false: a missing file is not logged,
    /// the caller expects it may not be there).
    /// </summary>
    public static XmlDocument OpenXMLDocument(string path, string file = "", OpenXmlType openType = OpenXmlType.Normal, bool pIgnoreComments = true, bool dialog = true)
    {
        try
        {
            return ReadXmlDocument(path, file, openType, pIgnoreComments);
        }
        catch (GameDataException ex)
        {
            if (dialog || ex.Line > 0)
                GameLog.Add(GameErrorReport.From(ex, "Reading XML", GameErrorSeverity.Warning, "The file was not used."));
            return null;
        }
    }

    /// <summary>
    /// The file's XML. A <see cref="GameDataException"/> when the file does not exist or is not valid XML, naming
    /// the file and the line and column where the XML breaks.
    /// </summary>
    public static XmlDocument ReadXmlDocument(string path, string file = "", OpenXmlType openType = OpenXmlType.Normal, bool pIgnoreComments = true)
    {
        string fullPath = Path.Combine(path, file);
        string text;
        try
        {
            switch (openType)
            {
                case OpenXmlType.Normal:
                    if (path.StartsWith(Application.streamingAssetsPath))
                    {
                        if (!File.Exists(fullPath))
                            throw new GameDataException("The file does not exist.", fullPath);
                        text = File.ReadAllText(fullPath);
                    }
                    else
                    {
                        text = ResourceManager.GetTextFromResources(fullPath);
                    }
                    break;
                case OpenXmlType.ForcedResourced:
                    text = ResourceManager.GetTextFromResources(fullPath);
                    break;
                default:
                    if (!File.Exists(fullPath))
                        throw new GameDataException("The file does not exist.", fullPath);
                    text = File.ReadAllText(fullPath);
                    break;
            }
        }
        catch (IOException ex)
        {
            throw new GameDataException("The file could not be read: " + ex.Message, fullPath, inner: ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new GameDataException("The file could not be read: " + ex.Message, fullPath, inner: ex);
        }
        if (string.IsNullOrEmpty(text))
            throw new GameDataException("The file does not exist.", fullPath);

        var document = new XmlDocument();
        try
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { IgnoreComments = pIgnoreComments }))
                document.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new GameDataException("The file is not valid XML: " + ex.Message, fullPath, line: ex.LineNumber, column: ex.LinePosition, inner: ex);
        }
        XmlSource.Register(document, fullPath, text);
        return document;
    }
}
