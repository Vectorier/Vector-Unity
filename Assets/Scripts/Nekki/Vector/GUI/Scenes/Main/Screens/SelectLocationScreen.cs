using System.Collections.Generic;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class SelectLocationScreen : UIModule
    {
        [SerializeField] ScrollSnap _scrollSnap;

        [SerializeField] LocationItem _locationItemPrefab;

        List<LocationItem> _items = new List<LocationItem>();

        LocationItem CurrentItem
        {
            get { return _scrollSnap.CurrentObject.GetComponent<LocationItem>(); }
        }

        protected override void Init()
        {
            base.Init();
            _scrollSnap.SnapEvent += i =>
            {
                var go = CurrentItem.Button.gameObject;
                ;
                SetSelectedGameObject(go);
                _SelectedObjectOnActivation = go;
            };
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            InitItems();

            _scrollSnap.Recalculate();
            _scrollSnap.Snap(0, true);
        }

        private void InitItems()
        {
            int count = 0;
            foreach (var type in LocationManager.Instance.locations.Keys)
            {
                count++;
                var info = LocationManager.Instance.GetLocationInfo(type, UserDataManager.RuntimeInfo.LocationModeType);

                bool locked = IsItemLocked(UserDataManager.Instance, info.UnlockInfo, info.Name);

                LocationItem locationItem = null;

                if (_items.Count < count)
                {
                    locationItem = Instantiate(_locationItemPrefab, _scrollSnap._content);
                    _items.Add(locationItem);
                }
                else
                {
                    locationItem = _items[count - 1];
                    locationItem.gameObject.SetActive(true);
                }

                locationItem.Lock.SetActive(locked);

                string name = info.Name.Replace("_HUNTER", "");
                CoroutineRunner.Instance.Run(ResourcesLoader.LoadLocationSpriteAsync(name,
                    loaded => { locationItem.IconLocation.sprite = loaded; }));
                if (UserDataManager.RuntimeInfo.IsHunterMode)
                {
                    locationItem.HunterCaption.gameObject.SetActive(true);
                }
                else
                {
                    locationItem.HunterCaption.gameObject.SetActive(false);
                }

                locationItem.Caption.text = LocalizationManager.Instance.GetTranslationByID(info.Name);
                int stars = 0;
                foreach (var mode in info.LocationInfos.Keys)
                {
                    foreach (var story in info.LocationInfos[mode])
                    {
                        stars += 3;
                    }
                }

                locationItem.StarsCounter.text = string.Format("{0}/{1}",
                    UserDataManager.Instance.GameStats.GetLocationStars(UserDataManager.RuntimeInfo.LocationModeType,
                        type), stars);
                locationItem.Button.onClick.RemoveAllListeners();
                locationItem.Button.onClick.AddListener(() =>
                {
                    if (!IsItemLocked(UserDataManager.Instance, info.UnlockInfo, info.Name))
                    {
                        UserDataManager.RuntimeInfo.CurentLocationType = type;
                        MainScene.Current.SwitchScreen<SelectStoryScreen>();
                    }
                    else
                    {
                        if (info.UnlockInfo.StarsLocationUnlockInfo == null)
                        {
                            MainScene.Current.GetDialog<BuyLocationDialog>()
                                .Show(info.Name, info.UnlockInfo.Price,
                                    LocalizationManager.Instance.GetTranslationByID(info.Name), InitItems);
                        }
                        else
                        {
                            var mode = UserDataManager.RuntimeInfo.LocationModeType == LocationModeType.Classic
                                ? LocalizationManager.Instance.GetTranslation("item_mode_story")
                                : LocalizationManager.Instance.GetTranslation("item_mode_hunter");
                            var storyName =
                                LocalizationManager.Instance.GetTranslationByID(info.UnlockInfo.StarsLocationUnlockInfo
                                    .StoryId) + " (" + mode + ")";

                            MainScene.Current.GetDialog<UnlockStoryDialog>().Show(info.UnlockInfo.Price,
                                info.UnlockInfo.StarsLocationUnlockInfo.Stars,
                                LocalizationManager.Instance.GetTranslationByID(info.Name), storyName, info.Name,
                                InitItems);
                        }
                    }
                });
            }

            if (_items.Count > count)
            {
                for (int i = count; i < _items.Count; i++)
                {
                    _items[i].gameObject.SetActive(false);
                }
            }

            _scrollSnap.StartIndex = 0;
            _scrollSnap.EndIndex = count - 1;
        }

        bool IsItemLocked(UserDataManager playerData, UnlockInfo unlockInfo, string itemName,
            StoryInfo currentStory = null)
        {
            if (unlockInfo != null && !playerData.ShopData.Contains(itemName))
            {
                if (unlockInfo.StarsLocationUnlockInfo == null)
                {
                    return true;
                }

                int stars = 0;
                if (string.IsNullOrEmpty(unlockInfo.StarsLocationUnlockInfo.StoryId))
                {
                    var locationLocator = unlockInfo.StarsLocationUnlockInfo.LocationLocator;
                    if (locationLocator == null)
                    {
                        stars = playerData.GameStats.GetAllStars();
                    }
                    else
                    {
                        stars = playerData.GameStats.GetLocationStars(locationLocator.LocationModeType,
                            locationLocator.LocationType);
                    }
                }
                else
                {
                    stars = playerData.GameStats.GetStarsCount(unlockInfo.StarsLocationUnlockInfo.StoryId);
                }

                return stars < unlockInfo.StarsLocationUnlockInfo.Stars;
            }

            return false;
        }

        public void OnContinueTap()
        {
            var item = CurrentItem;
            if (item == null)
                return;

            item.Button.onClick.Invoke();
        }

        public void OnBackTap()
        {
            MainScene.Current.GoToPreviousScreen();
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }
    }
}