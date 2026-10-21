using System.Collections.Generic;
using UnityEngine;

namespace UI.Scripts
{
    public class OnlyMobileElements : MonoBehaviour
    {
        public List<GameObject> Elements=new List<GameObject>();
        private void Awake() { foreach(var element in Elements) if(element!=null) element.SetActive(Application.isMobilePlatform); }
    }
}
