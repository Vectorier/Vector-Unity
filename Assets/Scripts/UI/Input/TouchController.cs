using System.Collections.Generic;
using UnityEngine;

namespace UI.InputControllers
{
	public class TouchController : MonoBehaviour
	{
		private const float _MinimumDistance = 30f;

		public SlideEvent OnSlide = new SlideEvent();

		public SlideEvent OnDrag = new SlideEvent();

		public TapEvent OnTap = new TapEvent();

		private Dictionary<int, Vector2> _Touches = new Dictionary<int, Vector2>();

		public static void SetEnabledAll(bool p_value)
		{
			TouchController[] array = Object.FindObjectsOfType<TouchController>();
			TouchController[] array2 = array;
			foreach (TouchController touchController in array2)
			{
				touchController.enabled = p_value;
			}
		}


        private void Update()
        {
            HandleTouchControl();
            if (Input.touchCount == 0) HandleMouseControl();
        }
        private void HandleMouseControl()
        {
            Vector2 position=Input.mousePosition;
            for(int button=0;button<3;button++)
            {
                int id=-button-1; // Mouse IDs cannot collide with touch finger IDs.
                if(Input.GetMouseButtonDown(button)) ProcessPointer(id,position,TouchPhase.Began);
                else if(Input.GetMouseButtonUp(button)) ProcessPointer(id,position,TouchPhase.Ended);
                else if(Input.GetMouseButton(button)) ProcessPointer(id,position,TouchPhase.Moved);
            }
        }
        private void HandleTouchControl()
        {
            foreach(var touch in Input.touches) ProcessPointer(touch.fingerId,touch.position,touch.phase);
        }
        /// <summary>Accepts press/move/release events; taps and slides emit once at release.</summary>
        public void ProcessPointer(int id,Vector2 position,TouchPhase phase)
        {
            if(phase==TouchPhase.Began) { _Touches[id]=position;return; }
            if(!_Touches.TryGetValue(id,out var from)) return;
            if(phase==TouchPhase.Canceled) { _Touches.Remove(id);return; }
            if(phase==TouchPhase.Ended)
            {
                _Touches.Remove(id);
                if(Vector2.Distance(from,position)<_MinimumDistance) OnTap.Invoke(id,position);
                else OnSlide.Invoke(id,from,position);
            }
            else if(phase==TouchPhase.Moved) OnDrag.Invoke(id,from,position);
        }
        private void OnDisable() { _Touches.Clear(); }

		public static Direction GetDirection(Vector2 p_from, Vector2 p_to)
		{
			float num = p_to.x - p_from.x;
			float num2 = p_to.y - p_from.y;
			if (num2 > 0f && Mathf.Abs(num) < Mathf.Abs(num2))
			{
				return Direction.Up;
			}
			if (num < 0f && Mathf.Abs(num) > Mathf.Abs(num2))
			{
				return Direction.Left;
			}
			if (num > 0f && Mathf.Abs(num) > Mathf.Abs(num2))
			{
				return Direction.Right;
			}
			if (num2 < 0f && Mathf.Abs(num) < Mathf.Abs(num2))
			{
				return Direction.Bottom;
			}
			return Direction.None;
		}
	}
}
