using System;
using System.Collections.Generic;
using System.Xml;
using Core._Common;
using GameDiagnostics;
using Nekki.Vector.Core.Camera;
using Nekki.Vector.Core.Controllers;

namespace Nekki.Vector.Core.Animation
{
    public class AnimationLoader
    {
        public string _xmlPath;

        public static AnimationLoader Current
        {
            get;
        }

        public AnimationInfo Info
        {
            get;
        }

        static AnimationLoader()
        {
            Current = new AnimationLoader();
        }

        public void Init()
        {
            _xmlPath = VectorPaths.Animations;
            ParseAnimations();
        }

        public void ReloadAnimations()
        {
            AnimationBinaryParser.ClearCachedBinary();
            AnimationGroup.ClearGroups();
            Animations.Animation.Clear();
            AnimationReaction.Reactions.Clear();
            AnimationTrickInfo.TricksLoaded.Clear();
            ParseAnimations();
        }

        private const string Stage = "Loading the moves (moves.xml)";

        /// <summary>
        /// Reads moves.xml. A move, interval, event group or reaction group that cannot be read is left out (and
        /// logged with where it is); the others load. Only a moves.xml that is missing, is not XML or has none of
        /// its sections stops the loading (a <see cref="GameDataException"/>).
        /// </summary>
        public void ParseAnimations()
        {
            Dictionary<string, XmlNode> dictionary = new Dictionary<string, XmlNode>();
            XmlDocument document = XmlUtils.ReadXmlDocument(_xmlPath, "moves.xml");
            XmlNode xmlNode = XmlSource.RequiredChild(document, "root");
            if (xmlNode["Config"] is XmlNode config)
                ParseConfigs(config);
            else
                GameLog.Warning(Stage, "moves.xml has no <Config> element.", "The camera zoom and taser use their defaults.", node: xmlNode);
            foreach (XmlNode childNode in XmlSource.RequiredChild(xmlNode, "EventGroups").ChildNodes)
            {
                try
                {
                    dictionary[XmlSource.Required(childNode, "Name")] = childNode;
                }
                catch (Exception ex)
                {
                    GameLog.Recovered(ex, Stage, "The event group was skipped.", childNode);
                }
            }
            ParseGroups(XmlSource.RequiredChild(xmlNode, "ReactionGroups"));
            XmlNode xmlNode3 = XmlSource.RequiredChild(xmlNode, "Moves");
            foreach (XmlNode childNode2 in xmlNode3.ChildNodes)
            {
                AnimationInfo animationInfo;
                try
                {
                    animationInfo = childNode2.Attributes["Trick"] != null && childNode2.Attributes["Trick"].ParseInt() != 0 ? new AnimationTrickInfo(childNode2) : new AnimationInfo(childNode2);
                }
                catch (Exception ex)
                {
                    GameLog.Recovered(ex, Stage, $"The move \"{childNode2.Name}\" was skipped.", childNode2);
                    continue;
                }
                foreach (XmlNode childNode3 in childNode2.ChildNodes)
                {
                    if (childNode3.Name != "Interval")
                    {
                        continue;
                    }
                    try
                    {
                        AnimationInterval animationInterval = null;
                        if (childNode3.Attributes["Groups"] == null)
                        {
                            animationInterval = new AnimationInterval(childNode3);
                        }
                        else
                        {
                            List<XmlNode> list = new List<XmlNode>();
                            string[] array = childNode3.Attributes["Groups"].Value.Split('|');
                            foreach (string key in array)
                            {
                                if (!dictionary.ContainsKey(key))
                                {
                                    GameLog.Warning(Stage, $"The event group \"{key}\" does not exist in <EventGroups>.",
                                        "The interval goes on without it.", node: childNode3, attribute: "Groups");
                                    continue;
                                }
                                list.Add(dictionary[key]);
                            }
                            animationInterval = new AnimationInterval(childNode3, list);
                        }
                        animationInfo.Intervals.Add(animationInterval);
                    }
                    catch (Exception ex)
                    {
                        GameLog.Recovered(ex, Stage, $"This interval of \"{childNode2.Name}\" was skipped (the move has no reactions there).", childNode3);
                    }
                }
                Animations.Animation[animationInfo.Name] = animationInfo;
            }
            AnimationBinaryParser.ClearCachedBinary();
        }

        public static void ParseGroups(XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                string name;
                try
                {
                    name = XmlSource.Required(childNode, "Name");
                }
                catch (Exception ex)
                {
                    GameLog.Recovered(ex, Stage, "The reaction group was skipped.", childNode);
                    continue;
                }
                List<AnimationReaction> list = new List<AnimationReaction>();
                foreach (XmlNode childNode2 in childNode.ChildNodes)
                {
                    try
                    {
                        list.Add(new AnimationReaction(childNode2));
                    }
                    catch (Exception ex)
                    {
                        GameLog.Recovered(ex, Stage, $"The reaction \"{childNode2.Name}\" was left out of the group \"{name}\".", childNode2);
                    }
                }
                AnimationGroup animationGroup = new AnimationGroup();
                animationGroup.Reactions = list;
                animationGroup.Name = name;
                AnimationGroup.Add(animationGroup);
            }
        }

        public static AnimationReaction ParseReaction(string[] pArray)
        {
            if (pArray.Length < 1 && int.Parse(pArray[1]) == 0)
            {
                return null;
            }
            string name = pArray[0];
            int frame = !string.IsNullOrEmpty(pArray[1]) ? int.Parse(pArray[1]) : 0;
            AnimationReaction animationReaction = new AnimationReaction(name, frame);
            if (pArray.Length == 2)
            {
                animationReaction.Reverse = false;
            }
            else
            {
                animationReaction.Reverse = int.Parse(pArray[2]) > 0;
            }
            return animationReaction;
        }

        private void ParseConfigs(XmlNode xmlNode)
        {
            XmlNode cameraNode = xmlNode["Camera"];
            if (cameraNode == null)
            {
                GameLog.Warning(Stage, "<Config> has no <Camera> element.", "The camera zoom uses its defaults.", node: xmlNode);
            }
            else
            {
                LocationCamera.MinZoom = cameraNode.Attributes["MinZoom"].ParseFloat();
                LocationCamera.MaxZoom = cameraNode.Attributes["MaxZoom"].ParseFloat();
                LocationCamera.CurrentZoom = cameraNode.Attributes["CurrZoom"].ParseFloat();
            }

            XmlNode taserNode = xmlNode["Taser"];
            if (taserNode == null)
            {
                GameLog.Warning(Stage, "<Config> has no <Taser> element.", "The taser uses its defaults.", node: xmlNode);
                return;
            }
            ControllerCatching.DistanceFactor = taserNode.Attributes["Distance"].ParseFloat();
            ControllerCatching.Timeout = taserNode.Attributes["Time"].ParseFloat();
            ControllerCatching.HeightFactor = taserNode.Attributes["HeightFactor"].ParseFloat();
        }
    }
}
