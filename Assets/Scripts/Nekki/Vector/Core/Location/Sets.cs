using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Core._Common;
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
            Debug.LogError("Object not found: " + name);
            return null;
        }

        private void LoadLevel()
        {
            var root = _document["Root"];
            if (root["Sets"] != null)
            {
                foreach (XmlNode set in root["Sets"])
                {
                    var fileName = set.Attributes["FileName"].Value;
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
                    Debug.LogError("Empty node from file \"" + file + "\"!");
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
                        _filesObjects.Add(set.Attributes["FileName"].Value);
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
                    Debug.LogError("Empty node from file \"" + file + "\"!");
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
                    _objectsNodes[build.Attributes["Name"].Value] = build;
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
                    _choices[node.Attributes["Name"].Value] = node.Attributes["Variant"].Value;
                }
            }
            _choices["AITriggers"] = LevelMainController.IsHunterMode ? "HunterMode" : "CommonMode";
        }

        private void ParseLevel()
        {
            var root = _document["Root"];
            TotalCoins = root["Coins"].Attributes["Value"].ParseInt();
            ParseModels(root.ChildNodes.Cast<XmlNode>().Where(node => node.Name == "Models"));
            Music = root["Music"].Attributes["Name"].Value;
            CreateObjects();
            CollectElements(_objects);

            foreach (var trigger in Triggers) trigger.Init();
            foreach (var particle in Particles) particle.Init();
            foreach (var animation in Animations) animation.Init();
            foreach (var primitive in Primitives) primitive.Load();
            foreach (var visual in Visuals) visual.Init();
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
                _userData.Add(Nekki.Vector.Core.User.UserData.FromXml(node));
            }
        }

        private void CreateObjects()
        {
            XmlNode track = _document["Root"]["Track"];
            var factors = new List<float>();
            var levelObject = new GameObject("Level_root_object");
            LevelMainController.current.LevelObject = levelObject;
            foreach (XmlNode xmlNode in track)
            {
                if (xmlNode.Attributes["Factor"] != null)
                {
                    factors.Add(float.Parse(xmlNode.Attributes["Factor"].Value, CultureInfo.InvariantCulture));
                }
            }
            factors.Sort();

            uint index = 0;
            foreach (XmlNode childNode in track)
            {
                var objectRunner = new ObjectRunner(index, null);
                index += objectRunner.Parse(childNode, null, _choices);
                objectRunner.Init();
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
