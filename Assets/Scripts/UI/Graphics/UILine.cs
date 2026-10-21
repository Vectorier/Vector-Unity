using System.Collections.Generic;
using UI.Graphics.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Graphics
{
	public class UILine : UIFigure
	{
		[SerializeField]
		private bool _SingleTexturCord;

		[SerializeField]
		private List<Vector2> _Points;

		[SerializeField]
		private float _Width = 10f;

		public List<Vector2> Points
		{
			get
			{
				return _Points;
			}
		}

		public float Width
		{
			get
			{
				return _Width;
			}
			set
			{
				_Width = value;
			}
		}

		protected override void OnPopulateMesh(VertexHelper p_vertexHelper)
		{
			base.OnPopulateMesh(p_vertexHelper);
			DrawFunctions.DrawLine(p_vertexHelper, _Points, _Width, color, _SingleTexturCord);
		}
	}
}
