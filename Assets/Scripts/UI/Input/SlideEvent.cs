using System;
using UnityEngine;
using UnityEngine.Events;

namespace UI.InputControllers
{
	[Serializable]
	public class SlideEvent : UnityEvent<int, Vector2, Vector2>
	{
	}
}
