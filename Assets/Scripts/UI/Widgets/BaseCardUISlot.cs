using DG.Tweening;
using UI.Graphics;
using UnityEngine;

namespace UI.Common
{
	public class BaseCardUISlot : MonoBehaviour
	{
		private Sequence colorTween;
		[SerializeField]
		private UICircle _Background;

		[SerializeField]
		private UICircleBorder _Border;

		[SerializeField]
		private ResolutionImage _Icon;

		public void Init(string p_iconName)
		{
			_Icon.SpriteName = p_iconName;
		}

		public void SetContentColor(Color p_color, float p_duration = 0f)
		{
			StopColorTween();
			if (p_duration > 1E-05f)
			{
				colorTween=DOTween.Sequence();
				if (_Border != null) colorTween.Join(_Border.DOColor(p_color, p_duration));
				if (_Icon != null) colorTween.Join(_Icon.DOColor(p_color, p_duration));
			}
			else
			{
				if (_Border != null) _Border.color = p_color;
				if (_Icon != null) _Icon.color = p_color;
			}
		}
		private void StopColorTween() { colorTween?.Kill();colorTween=null; }
		private void OnDisable() { StopColorTween(); }
		private void OnDestroy() { StopColorTween(); }
	}
}
