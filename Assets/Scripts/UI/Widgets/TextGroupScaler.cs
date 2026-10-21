using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
	[ExecuteInEditMode]
	public class TextGroupScaler : MonoBehaviour
	{
		[Serializable]
		public class TextData
		{
			public Text Text;

			public LayoutElement Layout;
		}

		public List<TextData> Texts = new List<TextData>();

		public int CheckFontStep = 2;

		private int _OptimalFontSize;

		private void Awake()
		{
		}

		private void LateUpdate()
		{
			if (Texts != null && Texts.Count != 0)
			{
				Refresh();
			}
		}

		public void Refresh()
		{
			if (CheckFontStep <= 0) throw new ArgumentOutOfRangeException(nameof(CheckFontStep), "Font step must be positive.");
			if (Texts == null) return;
			CalcOptimalFontSize();
			SetFontSizeToOptimal();
		}

		private void CalcOptimalFontSize()
		{
			_OptimalFontSize = int.MaxValue;
			foreach (TextData text in Texts)
			{
				if (text != null && text.Text != null && text.Layout != null && text.Layout.preferredWidth > 0)
				{
					_OptimalFontSize = Mathf.Min(_OptimalFontSize, GetOptimalFontSize(text));
				}
			}
		}

		private void SetFontSizeToOptimal()
		{
			if (_OptimalFontSize == int.MaxValue) return;
			foreach (TextData text in Texts)
			{
				if (text != null && text.Text != null)
				{
					text.Text.fontSize = _OptimalFontSize;
				}
			}
		}

		private int GetOptimalFontSize(TextData p_data)
		{
			p_data.Text.fontSize = Mathf.Clamp(p_data.Text.fontSize, 1, 512);
			int attempts = 0;
			while (attempts++ < 512 && p_data.Text.fontSize > 1 && p_data.Text.preferredWidth > p_data.Layout.preferredWidth)
			{
				p_data.Text.fontSize = Mathf.Max(1, p_data.Text.fontSize - CheckFontStep);
				Canvas.ForceUpdateCanvases();
			}
			return p_data.Text.fontSize;
		}
	}
}
