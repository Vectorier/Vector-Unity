using System;
using System.Collections.Generic;
using System.Xml;
using Nekki.Vector.Core.Transformation;
using UnityEngine;

namespace Nekki.Vector.Core.Location
{
    /// <summary>
    /// An "Object" node of a level: a positioned group of runners and child objects with its own
    /// transformations. Object references are resolved against the level's object/building libraries.
    /// </summary>
    public class ObjectRunner
    {
        private readonly ObjectRunner _parent;

        private readonly List<ObjectRunner> _childs = new List<ObjectRunner>();

        private readonly Dictionary<string, TransformSystem> _transformationDictionary = new Dictionary<string, TransformSystem>();

        private readonly List<TransformSystem> _transformationData = new List<TransformSystem>();

        private Point _position = new Point();

        private GameObject _unityObject;

        private Transform _cachedTransform;

        private string _name;

        private Elements _element;

        private bool _isEnabled = true;

        /// <summary>Child objects and runners in XML order, used to parent them under this object's GameObject.</summary>
        public readonly List<object> LayerOrder = new List<object>();

        public List<ObjectRunner> Childs => _childs;

        public Point Position => _position;

        public GameObject UnityObject => _unityObject;

        public Transform CachedTransform => _cachedTransform;

        public List<TransformSystem> TransformationData => _transformationData;

        public float Factor { get; private set; }

        public Elements Element => _element;

        public bool IsInitialized { get; private set; }

        public GameObject Layer { get; private set; }

        public uint Index { get; private set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                foreach (var runner in _element.Runners)
                {
                    runner.IsEnabled = value;
                }
            }
        }

        public ObjectRunner(uint index, ObjectRunner parent)
        {
            Index = index;
            _parent = parent;
        }

        public uint Parse(XmlNode node, XmlNode data, Dictionary<string, string> choices)
        {
            _name = node.Attributes["Name"].ParseString("");
            _position = new Point(node.Attributes["X"].ParseFloat(), node.Attributes["Y"].ParseFloat());
            Factor = node.Attributes["Factor"].ParseFloat();
            if (_parent != null)
            {
                _position.Add(_parent.Position);
            }
            ParseTransformationData(XmlUtils.GetDynamicProperties(node));
            Index++;
            var num = CreateElement(node["Content"], Index, choices);
            if (data == null)
            {
                return num;
            }
            ParseTransformationData(XmlUtils.GetDynamicProperties(data));
            return CreateElement(data["Content"], Index, choices);
        }

        private uint CreateElement(XmlNode node, uint index, Dictionary<string, string> choices)
        {
            var element = new Elements(this, index);
            uint num = element.Parse(node, choices);
            if (_element == null)
            {
                _element = element;
            }
            else
            {
                _element.CopyFrom(element);
            }
            return num;
        }

        public uint CreateChild(XmlNode mainNode, uint index, Dictionary<string, string> choices)
        {
            var selection = mainNode["Properties"]?["Static"]?["Selection"];
            if (selection != null &&
                (!choices.TryGetValue(selection.Attributes["Choice"].Value, out var variant) ||
                 variant != selection.Attributes["Variant"].Value))
            {
                return index;
            }

            string name = mainNode.Attributes["Name"].ParseString(string.Empty);
            XmlNode data = null;
            if (!string.IsNullOrEmpty(name))
            {
                // An object placed inside itself (directly or deeper) would nest until the game runs out of stack:
                // its definition is not followed again.
                bool nested = false;
                for (var p = this; p != null; p = p._parent)
                {
                    if (p._name == name)
                    {
                        nested = true;
                        break;
                    }
                }
                if (nested)
                {
                    GameDiagnostics.GameLog.Warning("Loading the level", $"The object \"{name}\" contains itself.",
                        "The inner copy was skipped.", node: mainNode, attribute: "Name");
                }
                else
                {
                    data = Sets.ObjectNode(name);
                    // Named without content of its own: a reference to an object that is not there.
                    if (data == null && mainNode["Content"] == null)
                        GameDiagnostics.GameLog.Warning("Loading the level",
                            $"The object \"{name}\" is not defined in the level or in its object sets.", "It was left out.", node: mainNode, attribute: "Name");
                }
            }

            var child = new ObjectRunner(index, this);
            uint num = child.Parse(mainNode, data, choices);
            child.Init();
            LayerOrder.Add(child);
            _childs.Add(child);
            return num;
        }

        public void Init()
        {
            if (_unityObject == null)
            {
                _unityObject = new GameObject(string.IsNullOrEmpty(_name) ? "[Object Holder]" : _name);
                _cachedTransform = _unityObject.transform;
                _cachedTransform.position = Vector3.zero;
            }
            foreach (var runner in _element.Runners)
            {
                runner.InitRunner(Position);
                BuildTransformationTable(runner.TransformationData);
            }
            foreach (var child in _childs)
            {
                BuildTransformationTable(child.TransformationData);
            }
            BuildTransformationTable(_transformationData);
            IsInitialized = true;
        }

        public void SetLayer(GameObject parent)
        {
            Layer = parent;
            _cachedTransform.parent = Layer.transform;
            _cachedTransform.localPosition = Vector3.zero;
            foreach (var obj in LayerOrder)
            {
                if (obj is ObjectRunner child)
                {
                    child.SetLayer(_unityObject);
                }
                else if (obj is Runner runner)
                {
                    runner.Layer = _unityObject;
                }
            }
            foreach (var child in _childs)
            {
                child.SetLayer(_unityObject);
            }
            foreach (var runner in _element.Runners)
            {
                runner.Layer = _unityObject;
            }
        }

        private void ParseTransformationData(XmlNode node)
        {
            if (node == null || node["Dynamic"] == null)
            {
                return;
            }
            foreach (XmlNode transformation in node["Dynamic"])
            {
                if (transformation.Name == "Transformation")
                {
                    var system = TransformSystem.Create(transformation);
                    system.MainParent = this;
                    _transformationData.Add(system);
                }
            }
        }

        private void BuildTransformationTable(List<TransformSystem> systems)
        {
            foreach (TransformSystem system in systems)
            {
                _transformationDictionary[system.Name] = system;
            }
        }

        public int RunTranformation(string name)
        {
            if (_transformationDictionary.TryGetValue(name, out var system))
            {
                system.Reset();
                return system.Run();
            }
            int frames = 0;
            foreach (var child in _childs)
            {
                frames = Math.Max(child.RunTranformation(name), frames);
            }
            return frames;
        }

        public int GetTransformationFrame(string name)
        {
            if (_transformationDictionary.TryGetValue(name, out var system))
            {
                return system.FramesCount;
            }
            int frames = 0;
            foreach (var child in _childs)
            {
                frames = Math.Max(child.GetTransformationFrame(name), frames);
            }
            return frames;
        }
    }
}
