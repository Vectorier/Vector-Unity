using System.Collections.Generic;
using UI.Graphics;
using UI.Graphics.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
public class UIPoligon : UIFigure
{
	[SerializeField]
	private List<Vector2> _Points = new List<Vector2>();

	[SerializeField]
	private bool _UseTriangleFan = true;

	public List<Vector2> Points
	{
		get
		{
			return _Points;
		}
	}

	protected override void OnPopulateMesh(VertexHelper p_vertexHelper)
	{
		base.OnPopulateMesh(p_vertexHelper);
		p_vertexHelper.Clear();
		if (_Points == null || _Points.Count < 3) return;
		List<UIVertex> list = new List<UIVertex>(_Points.Count);
		for (int i = 0; i < _Points.Count; i++)
		{
			list.Add(AddVertex(_Points[i]));
		}
		p_vertexHelper.Clear();
		p_vertexHelper.AddUIVertexStream(list, (!_UseTriangleFan) ? FigureTopology.TringleStripIndices(list.Count - 2) : FigureTopology.TringleFanIndices(list.Count - 2));
	}

	private UIVertex AddVertex(Vector3 p_position)
	{
		UIVertex simpleVert = UIVertex.simpleVert;
		simpleVert.position = new Vector2(p_position.x, p_position.y);
		simpleVert.uv0 = new Vector2(0f, 0f);
		simpleVert.color = color;
		return simpleVert;
	}
}

}
