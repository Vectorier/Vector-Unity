using System;
using UnityEngine;
using UnityEngine.Events;

namespace UI.InputControllers
{
	[Serializable]
	public class KeyEvent : UnityEvent<KeyCode>
	{
	}
}
