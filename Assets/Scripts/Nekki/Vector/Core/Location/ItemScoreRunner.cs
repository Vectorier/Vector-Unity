using Nekki.Vector.Core.Models;
using UnityEngine;

namespace Nekki.Vector.Core.Location
{
    public class ItemScoreRunner : ItemRunner
    {
        protected int _score;

        protected string _prefabName;

        protected ItemGO _content;

        protected float _x;

        protected float _y;

        protected int _type;

        public int score
        {
            get => _score;
            set => _score = value;
        }

        public ItemScoreRunner(int type, string prefabName, int score, float x, float y)
            : base(x, y, type)
        {
            _type = type;
            _score = score;
            _x = x;
            _y = y;
            _prefabName = prefabName;
        }

        protected override void GenerateObject()
        {
            _UnityObject = Object.Instantiate(Resources.Load<GameObject>("Prefabs/Gameplay/" + _prefabName));
            _CachedTransform = _UnityObject.transform;
            _CachedTransform.localPosition = new Vector3(_DefautPosition.X, _DefautPosition.Y, 0);
            if (_Layer != null)
            {
                _UnityObject.transform.parent = _Layer.transform;
            }
        }

        public override void Init()
        {
        }

        public override void InitRunner(Point point)
        {
            base.InitRunner(point);
            _content = _UnityObject.GetComponent<ItemGO>();
            _content.Init();
            UpdateUnityObjectPosition(Position);
        }

        public override void AddBonus(ModelHuman modelHuman)
        {
            modelHuman.CollectBonus(this);
            SoundsManager.Instance.PlaySounds(SoundType.bonus_pickup);
        }

        public override void Play()
        {
            _content.PlayEnd(_score);
        }

        public override void Move(Point shift)
        {
            base.Move(shift);
        }

        protected override void UpdateUnityObjectPosition(Vector3 position)
        {
            _UnityObject.transform.localPosition = new Vector3(position.x, position.y, 0);
        }

        public override void Reset()
        {
            base.Reset();
            _inclusion = false;
            _content.Reset();
        }
    }
}
