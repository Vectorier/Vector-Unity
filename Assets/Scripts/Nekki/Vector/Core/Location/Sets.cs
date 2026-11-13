using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Core._Common;
using GameDiagnostics;
using Nekki.Vector.Core.Location.Animation;
using Nekki.Vector.Core.User;
using Nekki.Vector.Core.Visual;
using UnityEngine;

namespace Nekki.Vector.Core.Location
{
    /// <summary>
    /// Everything a level XML builds: its object tree, runners grouped by type, layer containers,
    /// choices, models and music. Built once per level from the XML document.
    /// </summary>
    public class Sets
    {
        private readonly Dictionary<string, string> _choices = new Dictionary<string, string>();

        private readonly Dictionary<float, VisualContainer> _containers = new Dictionary<float, VisualContainer>();

        private readonly List<Runner> _allRunners = new List<Runner>();

        private readonly List<UserData> _userData = new List<UserData>();

        private readonly List<ObjectRunner> _objects = new List<ObjectRunner>();

        private readonly List<XmlNode> _xmlObjects = new List<XmlNode>();

        private readonly List<XmlNode> _xmlBuilds = new List<XmlNode>();

        private readonly HashSet<string> _filesBuilds = new HashSet<string>();

        private readonly HashSet<string> _filesObjects = new HashSet<string>();

        private readonly XmlDocument _document;

        private Dictionary<string, XmlNode> _objectsNodes = new Dictionary<string, XmlNode>();

        public readonly List<VisualRunner> Visuals = new List<VisualRunner>();

        public readonly List<PlatformRunner> Platforms = new List<PlatformRunner>();

        public readonly List<TriggerRunner> Triggers = new List<TriggerRunner>();

        public readonly List<AreaRunner> Areas = new List<AreaRunner>();

        public readonly List<AnimationRunner> Animations = new List<AnimationRunner>();

        public readonly List<ParticleRunner> Particles = new List<ParticleRunner>();

        public readonly List<ItemRunner> Items = new List<ItemRunner>();

        public readonly List<TrapezoidRunner> Trapezoids = new List<TrapezoidRunner>();

        public readonly List<CameraRunner> Cameras = new List<CameraRunner>();

        public readonly List<SpawnRunner> Spawns = new List<SpawnRunner>();

        /// <summary>Solid geometry (platforms and trapezoids).</summary>
        public readonly List<QuadRunner> Quads = new List<QuadRunner>();

        /// <summary>Solid geometry plus triggers and areas, used for the spatial grid.</summary>
        public readonly List<QuadRunner> QuadsAll = new List<QuadRunner>();

        public readonly List<PrimitiveRunner> Primitives = new List<PrimitiveRunner>();

        public static Sets Current { get; private set; }

        public Dictionary<float, VisualContainer> Containers => _containers;

        public List<Runner> AllRunners => _allRunners;

        public int TotalCoins { get; private set; }

        public List<UserData> UserData => _userData;

        public string Music { get; private set; }

        public int totalPoints
        {
            get
            {
                int points = 0;
                foreach (var area in Areas)
                {
                    if (area is TrickAreaRunner trick)
                    {
                        points += trick.score;
                    }
                }
                foreach (var item in Items)
                {
                    if (item is ItemScoreRunner scoreRunner)
                    {
                        points += scoreRunner.score;
                    }
                }
                return points;
            }
        }

        public int totalTricks => Areas.Count(area => area is TrickAreaRunner);

        public int totalBonus => Items.Count(item => item.Type == 0);

        public Sets(XmlDocument document)
        {
            Current = this;
            _document = document;
            LoadLevel();
        }

        public static XmlNode ObjectNode(string name)
        {
            if (Current != null && Current._objectsNodes != null && Current._objectsNodes.TryGetValue(name, out var node))
            {
                return node;
            }
            return null;
        }

        private void LoadLevel()
        {
            var root = XmlSource.RequiredChild(_document, "Root");
            if (root["Sets"] != null)
            {
                foreach (XmlNode set in root["Sets"])
                {
                    var fileName = set.Attributes?["FileName"]?.Value;
                    if (string.IsNullOrEmpty(fileName))
                    {
                        GameLog.Warning(Stage, "The set has no FileName attribute.", "The set was skipped.", node: set, attribute: "FileName");
                        continue;
                    }
                    if (set.Name == "City")
                    {
                        _filesBuilds.Add(fileName);
                    }
                    else
                    {
                        _filesObjects.Add(fileName);
                    }
                }
            }
            LoadBuilds();
            LoadObjects();
            Parse();
        }

        private void LoadBuilds()
        {
            foreach (var file in _filesBuilds)
            {
                var root = XmlUtils.OpenXMLDocument(VectorPaths.XmlRoot, file)?["Root"];
                if (root == null)
                {
                    // A file missing or not XML was logged when it was read.
                    continue;
                }
                if (root["Objects"] != null)
                {
                    _xmlBuilds.Add(root["Objects"]);
                }
                if (root["Sets"] != null)
                {
                    foreach (XmlNode set in root["Sets"])
                    {
                        var fileName = set.Attributes?["FileName"]?.Value;
                        if (string.IsNullOrEmpty(fileName))
                            GameLog.Warning(Stage, "The set has no FileName attribute.", "The set was skipped.", node: set, attribute: "FileName");
                        else
                            _filesObjects.Add(fileName);
                    }
                }
            }
        }

        private void LoadObjects()
        {
            foreach (var file in _filesObjects)
            {
                var root = XmlUtils.OpenXMLDocument(VectorPaths.XmlRoot, file)?["Root"];
                if (root == null)
                {
                    continue;
                }
                if (root["Objects"] == null)
                {
                    GameLog.Warning(Stage, "The object set has no <Objects> element.", "Its objects are not available.", node: root);
                    continue;
                }
                _xmlObjects.Add(root["Objects"]);
            }
        }

        private void Parse()
        {
            ParseChoices(_document["Root"]["Choises"]);
            foreach (XmlNode node in _xmlObjects)
            {
                foreach (XmlNode obj in node)
                {
                    if (obj.Attributes["Name"] != null)
                        _objectsNodes[obj.Attributes["Name"].Value] = obj;
                }
            }
            foreach (XmlNode node in _xmlBuilds)
            {
                foreach (XmlNode build in node)
                {
                    if (build.Attributes?["Name"]?.Value is string buildName)
                        _objectsNodes[buildName] = build;
                    else
                        GameLog.Warning(Stage, "The object has no Name attribute.", "It cannot be placed by name.", node: build, attribute: "Name");
                }
            }
            ParseLevel();
            _objectsNodes = null;
        }

        private void ParseChoices(XmlNode nodes)
        {
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    string name = node.Attributes?["Name"]?.Value, variant = node.Attributes?["Variant"]?.Value;
                    if (name == null || variant == null)
                        GameLog.Warning(Stage, "The choice needs both a Name and a Variant attribute.", "The choice was skipped.", node: node,
                            attribute: name == null ? "Name" : "Variant");
                    else
                        _choices[name] = variant;
                }
            }
            _choices["AITriggers"] = LevelMainController.IsHunterMode ? "HunterMode" : "CommonMode";
        }

        private void ParseLevel()
        {
            var root = _document["Root"];
            TotalCoins = root["Coins"]?.Attributes["Value"].ParseInt() ?? 0;
            ParseModels(root.ChildNodes.Cast<XmlNode>().Where(node => node.Name == "Models"));
            Music = root["Music"]?.Attributes["Name"]?.Value;
            if (Music == null)
                GameLog.Warning(Stage, "The level has no <Music Name=\"...\"> element.", "The level plays without music.", node: root);
            CreateObjects();
            CollectElements(_objects);

            // Each one that cannot start is logged and left out; the level plays without it.
            foreach (var trigger in Triggers.ToList())
            {
                try
                {
                    trigger.Init();
                }
                catch (System.Exception ex)
                {
                    GameLog.Recovered(ex, Stage, $"The trigger \"{trigger.Name}\" was turned off.");
                    trigger.IsEnabled = false;
                    Triggers.Remove(trigger);
                    QuadsAll.Remove(trigger);
                    _allRunners.Remove(trigger);
                }
            }
            InitEach(Particles, particle => particle.Init(), "particle effect");
            InitEach(Animations, animation => animation.Init(), "animation");
            InitEach(Primitives, primitive => primitive.Load(), "model");
            InitEach(Visuals, visual => visual.Init(), "image");
        }

        private const string Stage = "Loading the level";

        private static void InitEach<T>(List<T> runners, System.Action<T> init, string kind) where T : Runner
        {
            foreach (var runner in runners)
            {
                try
                {
                    init(runner);
                }
                catch (System.Exception ex)
                {
                    GameLog.Recovered(ex, Stage, $"The {kind} \"{runner.Name}\" was not started.");
                }
            }
        }

        private void ParseModels(IEnumerable<XmlNode> nodes)
        {
            foreach (XmlNode node in nodes)
            {
                // Matches the original game: an unconditional Models group ends the search.
                if (node.Attributes["Choice"] == null)
                {
                    AddModels(node);
                    return;
                }
                if (_choices.TryGetValue(node.Attributes["Choice"].Value, out var variant) &&
                    variant == node.Attributes["Variant"].Value)
                {
                    AddModels(node);
                }
            }
        }

        private void AddModels(XmlNode models)
        {
            foreach (XmlNode node in models.ChildNodes)
            {
                try
                {
                    _userData.Add(Nekki.Vector.Core.User.UserData.FromXml(node));
                }
                catch (System.Exception ex)
                {
                    GameLog.Recovered(ex, Stage, "The character was left out of the level.", node);
                }
            }
        }

        private void CreateObjects()
        {
            XmlNode track = XmlSource.RequiredChild(_document["Root"], "Track");
            var factors = new List<float>();
            var levelObject = new GameObject("Level_root_object");
            LevelMainController.current.LevelObject = levelObject;
            foreach (XmlNode xmlNode in track)
            {
                if (xmlNode.Attributes["Factor"] != null)
                {
                    if (float.TryParse(xmlNode.Attributes["Factor"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
                        factors.Add(factor);
                    else
                        GameLog.Warning(Stage, "The Factor is not a number.", "The object uses factor 0.", node: xmlNode, attribute: "Factor");
                }
            }
            factors.Sort();

            uint index = 0;
            foreach (XmlNode childNode in track)
            {
                var objectRunner = new ObjectRunner(index, null);
                try
                {
                    index += objectRunner.Parse(childNode, null, _choices);
                    objectRunner.Init();
                }
                catch (System.Exception ex)
                {
                    GameLog.Recovered(ex, Stage, "The object was left out of the level.", childNode);
                    continue;
                }
                _objects.Add(objectRunner);

                if (!_containers.TryGetValue(objectRunner.Factor, out var visualContainer))
                {
                    var factorIndex = factors.IndexOf(objectRunner.Factor);
                    visualContainer = new VisualContainer(objectRunner.Factor, (factors.Count - factorIndex) * 10, factorIndex);
                    visualContainer.Object.transform.SetParent(levelObject.transform);
                    _containers[objectRunner.Factor] = visualContainer;
                }
                objectRunner.SetLayer(visualContainer.Object);
            }
        }

        private void CollectElements(List<ObjectRunner> objectRunners)
        {
            foreach (var objectRunner in objectRunners)
            {
                var elements = objectRunner.Element;
                _allRunners.AddRange(elements.Runners);
                Visuals.AddRange(elements.Visuals);
                Platforms.AddRange(elements.Platforms);
                Triggers.AddRange(elements.Triggers);
                Areas.AddRange(elements.Areas);
                Spawns.AddRange(elements.Spawns);
                Cameras.AddRange(elements.Cameras);
                Trapezoids.AddRange(elements.Trapezoids);
                Primitives.AddRange(elements.Primitives);
                Animations.AddRange(elements.Animations);
                Particles.AddRange(elements.Particles);
                Items.AddRange(elements.Items);

                Quads.AddRange(elements.Platforms);
                Quads.AddRange(elements.Trapezoids);

                QuadsAll.AddRange(elements.Platforms);
                QuadsAll.AddRange(elements.Triggers);
                QuadsAll.AddRange(elements.Areas);
                QuadsAll.AddRange(elements.Trapezoids);

                CollectElements(objectRunner.Childs);
            }
        }
    }
}
