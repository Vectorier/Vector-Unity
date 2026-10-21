using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	public class UIParticleSettings : MonoBehaviour
	{
		private Mesh ownedMesh;
		[SerializeField]
		private MeshFilter _MeshFilter;

		[SerializeField]
		private ParticleSystem _ParticleSystem;

		[SerializeField]
		[HideInInspector]
		private ParticleShape _Shape;

		[HideInInspector]
		[SerializeField]
		private float _CircleRadius;

		[SerializeField]
		[HideInInspector]
		private float _RectWidth;

		[HideInInspector]
		[SerializeField]
		private float _RectHeight;

		[SerializeField]
		[HideInInspector]
		private bool _FromEdge;

		public ParticleShape Shape
		{
			get
			{
				return _Shape;
			}
			set
			{
				_Shape = value;
			}
		}

		public float CircleRadius
		{
			get
			{
				return _CircleRadius;
			}
			set
			{
				_CircleRadius = value;
			}
		}

		public float RectWidth
		{
			get
			{
				return _RectWidth;
			}
			set
			{
				_RectWidth = value;
			}
		}

		public float RectHeight
		{
			get
			{
				return _RectHeight;
			}
			set
			{
				_RectHeight = value;
			}
		}

		public bool FromEdge
		{
			get
			{
				return _FromEdge;
			}
			set
			{
				_FromEdge = value;
			}
		}

		private void ResetMesh()
		{
			if (ownedMesh != null)
			{
				if (_MeshFilter != null && _MeshFilter.sharedMesh == ownedMesh) _MeshFilter.sharedMesh = null;
				if (_ParticleSystem != null) { var shape = _ParticleSystem.shape;if (shape.mesh == ownedMesh) shape.mesh = null; }
				if (Application.isPlaying) Object.Destroy(ownedMesh);else Object.DestroyImmediate(ownedMesh);
				ownedMesh = null;
			}
		}

		private void GenerateMesh()
		{
			Mesh mesh = new Mesh();
			ownedMesh = mesh;
			Vector2 zero = Vector2.zero;
			List<Vector3> list = new List<Vector3>();
			list.Add(new Vector3(zero.x - _RectWidth * 0.5f, zero.y - _RectHeight * 0.5f, 0f));
			list.Add(new Vector3(zero.x + _RectWidth * 0.5f, zero.y + _RectHeight * 0.5f, 0f));
			list.Add(new Vector3(zero.x + _RectWidth * 0.5f, zero.y - _RectHeight * 0.5f, 0f));
			list.Add(new Vector3(zero.x - _RectWidth * 0.5f, zero.y + _RectHeight * 0.5f, 0f));
			mesh.vertices = list.ToArray();
			List<int> list2 = new List<int>();
			list2.Add(0);
			list2.Add(1);
			list2.Add(2);
			list2.Add(1);
			list2.Add(0);
			list2.Add(3);
			mesh.triangles = list2.ToArray();
			List<Vector3> list3 = new List<Vector3>();
			list3.Add(new Vector3(-1f, -1f, 0f));
			list3.Add(new Vector3(1f, 1f, 0f));
			list3.Add(new Vector3(1f, -1f, 0f));
			list3.Add(new Vector3(-1f, 1f, 0f));
			mesh.normals = list3.ToArray();
			mesh.RecalculateBounds();
			_MeshFilter.sharedMesh = mesh;
		}

		[ContextMenu("Force Refresh")]
		public void Refresh()
		{
			if (_ParticleSystem == null) return;
			ResetMesh();
			if (_Shape == ParticleShape.Circle)
			{
				SetupShapeToCircle();
			}
			else if (_Shape == ParticleShape.Rect)
			{
				SetupShapeToRect();
			}
		}

		public void SetupShapeToCircle()
		{
			if (_ParticleSystem == null) return;
			ParticleSystem.ShapeModule shape = _ParticleSystem.shape;
			shape.radius = _CircleRadius;
			shape.shapeType = ParticleSystemShapeType.Circle;
			shape.radiusThickness = _FromEdge ? 0f : 1f;
		}

		public void SetupShapeToRect()
		{
			if (_ParticleSystem == null || _MeshFilter == null) return;
			ResetMesh();
			GenerateMesh();
			ParticleSystem.ShapeModule shape = _ParticleSystem.shape;
			shape.mesh = _MeshFilter.sharedMesh;
			shape.shapeType = ParticleSystemShapeType.Mesh;
			shape.meshShapeType = ParticleSystemMeshShapeType.Edge;
		}
		private void OnDestroy() { ResetMesh(); }
	}
}
