using UnityEngine;
using UnityEngine.UI;

namespace Nekki.Vector.GUI.Scenes.Preloader
{
    public class ProgressBar : MonoBehaviour
    {
        private RectMask2D _mask;

        private void Awake()
        {
            _mask = GetComponent<RectMask2D>();
        }

        public void SetValue(float value)
        {
            var rectTransform = GetComponent<RectTransform>();
            var vector = _mask.padding;
            vector.z = (1 - value) * rectTransform.sizeDelta.x;
            _mask.padding = vector;
        }        
    }
}