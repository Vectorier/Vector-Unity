using System;
using System.IO;
using System.Xml;
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

    public static XmlDocument OpenXMLDocument(string path, string file = "", OpenXmlType openType = OpenXmlType.Normal, bool pIgnoreComments = true, bool dialog = true)
    {
        string fullPath = Path.Combine(path, file);
        var xmlReaderSettings = new XmlReaderSettings();
        xmlReaderSettings.IgnoreComments = pIgnoreComments;
        XmlDocument document = null;
        switch (openType)
        {
            case OpenXmlType.Normal:
                if (path.StartsWith(Application.streamingAssetsPath))
                {
                    if (!File.Exists(fullPath))
                    {
                        if (dialog)
                            ErrorDialog.Report("FILE DOESNT EXIST: " + fullPath, true, true);

                        return null;
                    }
                    var text0 = File.ReadAllText(fullPath);
                    document = OpenXMLDocFromString(text0, xmlReaderSettings);
                    break;
                }
                var text = ResourceManager.GetTextFromResources(fullPath);
                document = OpenXMLDocFromString(text, xmlReaderSettings);
                break;
            case OpenXmlType.ForcedResourced:
                var text1 = ResourceManager.GetTextFromResources(fullPath);
                document = OpenXMLDocFromString(text1, xmlReaderSettings);
                break;
            case OpenXmlType.ForcedExternal:
                var text2 = File.ReadAllText(fullPath);
                document = OpenXMLDocFromString(text2, xmlReaderSettings);
                break;
            default:
                var input = File.Open(fullPath, FileMode.Open);
                var reader = XmlReader.Create(input);
                document = OpenXMLDocument(reader);
                break;
        }
        return document;
    }

    private static XmlDocument OpenXMLDocFromString(string pData, XmlReaderSettings pSettings)
    {
        using (XmlReader p_reader = XmlReader.Create(new StringReader(pData), pSettings))
        {
            XmlDocument xmlDocument = OpenXMLDocument(p_reader);
            if (xmlDocument == null)
            {
                ErrorDialog.Report("Error read XML", true);
            }
            return xmlDocument;
        }
    }

    private static XmlDocument OpenXMLDocument(XmlReader pReader)
    {
        try
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(pReader);
            return xmlDocument;
        }
        catch
        {
            return null;
        }
    }

}
