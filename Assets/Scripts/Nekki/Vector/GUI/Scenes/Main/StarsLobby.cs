using System.Collections.Generic;
using UnityEngine;

namespace Nekki.Vector.GUI.Scenes.Main
{
    public class StarsLobby : MonoBehaviour
    {
        public GameObject Content;

        [SerializeField] private StarsLobbyItem _starsItemPrefab;

        List<StarsLobbyItem> _stars = new List<StarsLobbyItem>();

        bool _loaded = false;

        void OnEnable()
        {
            // if (_loaded)
            //     return;

            var gameStats = UserDataManager.Instance.GameStats;

            int count = 0;

            foreach (var location in LocationManager.Instance.locations.Keys)
            {
                foreach (var mode in LocationManager.Instance.locations[location].Keys)
                {
                    StarsLobbyItem starsView = null;
                    if (_stars.Count <= count)
                    {
                        starsView = Instantiate(_starsItemPrefab, Content.transform);
                        _stars.Add(starsView);
                    }
                    else
                    {
                        starsView = _stars[count];
                    }

                    starsView.Gradient.SetActive(count % 2 == 0);

                    string locationID = string.Format("%item_{0}%", location);
                    string modeID = string.Format("(%item_mode_{0}%)",
                        mode == LocationModeType.Hunter ? "hunter" : "story");

                    starsView.LocationInfo.SetText(locationID + " " + modeID);
                    starsView.Count.text = gameStats.GetLocationStars(mode, location).ToString();

                    count++;
                }
            }

            _loaded = true;
        }
    }
}