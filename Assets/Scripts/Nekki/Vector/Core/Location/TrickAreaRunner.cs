using System.Xml;
using Nekki.Vector.Core.Models;
using UnityEngine;

namespace Nekki.Vector.Core.Location
{
    public class TrickAreaRunner : AreaRunner
    {
        private const float ExpansionWidthRatio = 1f;

        private readonly XmlNode _node;

        public static TrickAreaRunner Current;

        private bool _isActive;

        private string _itemName;

        private int _score;

        private TrickGO _content;

        public bool isActive => _isActive;

        public string itemName => _itemName;

        public int score => _score;

        public TrickAreaRunner(float x, float y, float width, float height, string typeName, string name, string itemName, int score)
            : base(AreaType.Trick, x, y, width, height, typeName, name)
        {
            _itemName = itemName;
            _score = score;
            _isActive = Game.IsTrickBought(_itemName);
        }

        protected override void GenerateObject()
        {
            var obj = Resources.Load<GameObject>("Prefabs/Gameplay/Trick");
            obj = Object.Instantiate(obj);
            _UnityObject = obj;
            _CachedTransform = obj.transform;
            var vector = new Vector3();
            vector.x = _DefautPosition.X;
            vector.y = _DefautPosition.Y;
            _CachedTransform.localPosition = vector;
            if (_Layer != null)
            {
                _CachedTransform.SetParent(_Layer.transform, false);
            }
            _content = _UnityObject.GetComponent<TrickGO>();
            _content.Init(_itemName, _isActive, _W, _H);
        }

        public override void InitRunner(Point point)
        {
            base.InitRunner(point);
            UpdateUnityObjectPosition(Position);

            _DefautPosition.Set(Position);
        }

        public override void Activate(ModelHuman model)
        {
            if (model.UserData.IsTrick)
            {
                Current = this;
            }
        }

        public override void Deactivate(ModelHuman model)
        {
            if (model.UserData.IsTrick)
            {
                Current = null;
            }
        }

        protected override void UpdateUnityObjectPosition(Vector3 position)
        {
            position.z = 0;
            _CachedTransform.localPosition = position;
        }

        public static void TrickGetByModel(ModelHuman model)
        {
            if (Current == null || !model.UserData.IsSelf)
            {
                return;
            }
            SoundsManager.Instance.PlaySounds(SoundType.trick_activate);
            model.CollectTrick(Current);
            Current._content.RunActivate();
            LevelMainController.current.levelSceneController.ShowActivatedTrick(Current);
        }

        public override void Reset()
        {
            base.Reset();
            UpdateUnityObjectPosition(Position);
            Current = null;
            _isActive = Game.IsTrickBought(_itemName);
            _content.Init(_itemName, _isActive, _W, _H);
        }

        public void Refresh()
        {
            if (!_isActive)
            {
                _isActive = Game.IsTrickBought(_itemName);
                _content.Init(_itemName, _isActive, _W, _H);
            }
        }

    }
}
