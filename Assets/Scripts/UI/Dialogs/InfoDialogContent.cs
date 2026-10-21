using System.Collections.Generic;
using UI.Localization;
using UnityEngine;

namespace UI.Dialogs
{
	public class InfoDialogContent : DialogContent
	{
		[SerializeField]
		private LabelAlias _Title;

		[SerializeField]
		private LabelAlias _Text;

		public void Init(List<DialogButtonData> p_buttons, string p_title, string p_text)
		{
			Init(p_buttons);
			_Title.SetAlias(p_title);
			_Text.SetAlias(p_text);
		}
	}
}
