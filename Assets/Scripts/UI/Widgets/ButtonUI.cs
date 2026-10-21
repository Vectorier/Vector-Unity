using System.Collections.Generic;
using DG.Tweening;
using UI.Localization;
using UI.Utilites;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
	[ExecuteInEditMode]
	public class ButtonUI : MonoBehaviour
	{
		public enum Type
		{
			Red = 0,
			Green = 1,
			Blue = 2,
			Grey = 3
		}

		[SerializeField]
		private Image _Backdround;

		[SerializeField]
		private LabelAlias _ButtonText;

		[SerializeField]
		private GameObject _PaidPlate;

		[SerializeField]
		private ResolutionImage _PaidIcon;

		[SerializeField]
		private LabelAlias _PaidCount;

		[SerializeField]
		private GameObject _FreeSpace;

		[SerializeField]
		private Button _Button;

		[SerializeField]
		private List<LayoutElement> _PaidPlateSpaces;

		private bool _FlexiblePaidSize;

		private string _LastPaidValue;

		private float _PaidSpacesWidth;
		private Sequence fade;

		public Image Backdround
		{
			get
			{
				return _Backdround;
			}
		}

		public LabelAlias ButtonText
		{
			get
			{
				return _ButtonText;
			}
		}

		public Image PaidPlate
		{
			get
			{
				return _PaidPlate.GetComponent<Image>();
			}
		}

		public ResolutionImage PaidIcon
		{
			get
			{
				return _PaidIcon;
			}
		}

		public LabelAlias PaidCount
		{
			get
			{
				return _PaidCount;
			}
		}

		public bool IsPaid
		{
			get
			{
				return _PaidPlate != null && _PaidPlate.activeSelf;
			}
		}

		public Button Button
		{
			get
			{
				return _Button;
			}
		}

		private bool IsFlexibleContentDirty
		{
			get
			{
				return _LastPaidValue != _PaidCount.text;
			}
		}

		public void SetType(Type p_type, bool changeOpacity = true, string soundAlias = null)
		{
			if (_Backdround == null) { Debug.LogError("ButtonUI needs its Background Image assigned.", this); return; }
			float num = 1f;
			if (!changeOpacity)
			{
				num = _Backdround.color.a;
			}
			switch (p_type)
			{
			case Type.Blue:
				_Backdround.color = new Color(0.234f, 0.375f, 0.441f, num);
				SetSound(soundAlias, "blue_button");
				break;
			case Type.Green:
				_Backdround.color = new Color(0.234f, 0.398f, 0.3125f, num);
				SetSound(soundAlias, "blue_button");
				break;
			case Type.Red:
				_Backdround.color = new Color(0.376f, 0.192f, 0.212f, num);
				SetSound(soundAlias, "red_button");
				break;
			case Type.Grey:
				_Backdround.color = ColorUtils.FromHex("515151", num);
				SetSound(soundAlias, "grey_button");
				break;
			}
		}

		private void SetSound(string p_soundAlias, string p_defaultSoundAlias)
		{
			ButtonSoundClick componentInChildren = GetComponentInChildren<ButtonSoundClick>();
			if (p_soundAlias == null)
			{
				p_soundAlias = p_defaultSoundAlias;
			}
			if (componentInChildren != null)
			{
				componentInChildren.SoundAlias = p_soundAlias;
			}
		}

		public void TurnToPaid()
		{
			if (_FreeSpace != null) _FreeSpace.SetActive(true);
			if (_PaidPlate != null) _PaidPlate.SetActive(true);
			if (_FlexiblePaidSize)
			{
				LayoutElement component = _ButtonText.GetComponent<LayoutElement>();
				component.flexibleWidth = -1f;
				ResetFlexibleContentDirty();
				ResizeContent();
			}
		}

		public void TurnToFree()
		{
			if (_FreeSpace != null) _FreeSpace.SetActive(false);
			if (_PaidPlate != null) _PaidPlate.SetActive(false);
			if (_FlexiblePaidSize)
			{
				LayoutElement component = _ButtonText.GetComponent<LayoutElement>();
				component.flexibleWidth = 1f;
				component.preferredWidth = -1f;
			}
		}

		private void Awake()
		{
			CalculatePaidSpacesWidth();
		}

		private void Update()
		{
			if (_FlexiblePaidSize && IsPaid && IsFlexibleContentDirty)
			{
				ResetFlexibleContentDirty();
				ResizeContent();
			}
		}

		private void ResetFlexibleContentDirty()
		{
			_LastPaidValue = _PaidCount.text;
		}

		private void ResizeContent()
		{
			bool flag = string.IsNullOrEmpty(_PaidCount.text);
			float actualPaidSpacesWidth = GetActualPaidSpacesWidth(flag);
			_PaidPlateSpaces[1].gameObject.SetActive(!flag);
			float num = _PaidIcon.GetComponent<LayoutElement>().minWidth + _PaidCount.preferredWidth + actualPaidSpacesWidth;
			_PaidPlate.GetComponent<LayoutElement>().preferredWidth = num;
			float b = GetComponent<RectTransform>().sizeDelta.x - (num + _FreeSpace.GetComponent<LayoutElement>().preferredWidth);
			_ButtonText.GetComponent<LayoutElement>().preferredWidth = Mathf.Max(0f, b);
		}

		private void CalculatePaidSpacesWidth()
		{
			if (_PaidPlateSpaces == null || _PaidPlateSpaces.Count == 0)
			{
				return;
			}
			_PaidSpacesWidth = 0f;
			foreach (LayoutElement paidPlateSpace in _PaidPlateSpaces)
			{
				_PaidSpacesWidth += paidPlateSpace.minWidth;
			}
			_FlexiblePaidSize = true;
		}

		private float GetActualPaidSpacesWidth(bool p_isTextEmpty)
		{
			float num = _PaidSpacesWidth;
			if (p_isTextEmpty)
			{
				num -= _PaidPlateSpaces[1].minWidth;
			}
			return num;
		}

		public void Fade(float p_time)
		{
			StopFade();
			if (!Application.isPlaying || p_time <= 0) { SetAlpha(0);return; }
			fade = DOTween.Sequence();
			foreach (var graphic in FadeGraphics()) if (graphic != null) fade.Join(graphic.DOFade(0f, p_time));
		}

		public void ResetAlpha()
		{
			StopFade();SetAlpha(1);
		}
		private IEnumerable<Graphic> FadeGraphics() { yield return _Backdround;yield return _PaidCount;yield return _ButtonText;yield return _PaidIcon;if (_PaidPlate != null) yield return _PaidPlate.GetComponent<Image>(); }
		private void SetAlpha(float alpha) { foreach (var graphic in FadeGraphics()) if (graphic != null) { var value=graphic.color;value.a=alpha;graphic.color=value; } }
		private void StopFade() { fade?.Kill();fade=null; }
		private void OnDisable() { StopFade(); }
		private void OnDestroy() { StopFade(); }
	}
}
