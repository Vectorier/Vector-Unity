using DG.Tweening;
using Nekki.Vector.Core.Node;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekki.Vector.Core.Scripts.Geometry
{
    public class Capsule : MonoBehaviour
    {
        private Color _Color = Color.black;

        private double _Stroke = 1;

        protected ModelLine _Base;

        private static Material _SharedMaterial;

        private LineRenderer _LineRenderer;

        public Color Color
        {
            get => _Color;
            set => _Color = value;
        }

        private static Material SharedMaterial
        {
            get
            {
                if (_SharedMaterial == null)
                {
                    _SharedMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                return _SharedMaterial;
            }
        }

        public Vector2 middleRect => transform.TransformPoint(Vector3f.Middle(_LineRenderer.GetPosition(0), _LineRenderer.GetPosition(1)));

        public string Name => _Base.Name;

        public void Init(ModelLine modelLine)
        {
            _Base = modelLine;
            _Stroke = modelLine.Stroke;

            _Stroke = _Base.Stroke;
            _LineRenderer = gameObject.AddComponent<LineRenderer>();
            _LineRenderer.numCapVertices = 9;
            _LineRenderer.widthMultiplier = (float)(_Stroke * 2);
            _LineRenderer.useWorldSpace = false;
            _LineRenderer.sharedMaterial = SharedMaterial;
            _LineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _LineRenderer.receiveShadows = false;
            _LineRenderer.alignment = LineAlignment.TransformZ;
            _LineRenderer.allowOcclusionWhenDynamic = false;
            _LineRenderer.sortingLayerName = "Model";
            _LineRenderer.startColor = _Color;
            _LineRenderer.endColor = _Color;
        }

        // Gameplay advances in Update; sample its completed presentation state.
        public void LateUpdate()
        {
            if (_Base == null || _LineRenderer == null)
                return;

            if (_Stroke != _Base.Stroke)
            {
                _Stroke = _Base.Stroke;
                _LineRenderer.widthMultiplier = (float)(_Stroke * 2);
            }

            if (_Base.Start?.End == null || _Base.End?.End == null)
                return;

            Vector3 start = FixedRenderInterpolation.Position(_Base.Start);
            Vector3 end = FixedRenderInterpolation.Position(_Base.End);

            Vector3 pos1 = Vector3.LerpUnclamped(start, end, (float)_Base.Margin1);
            Vector3 pos2 = Vector3.LerpUnclamped(end, start, (float)_Base.Margin2);

            pos1.z = 0f;
            pos2.z = 0f;

            _LineRenderer.SetPosition(0, pos1);
            _LineRenderer.SetPosition(1, pos2);
        }
    }
}
