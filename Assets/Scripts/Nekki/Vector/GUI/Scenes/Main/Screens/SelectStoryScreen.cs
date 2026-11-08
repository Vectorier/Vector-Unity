using System.Collections.Generic;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class SelectStoryScreen : UIModule
    {
        [SerializeField]
        Text _title;

        [SerializeField] private ScrollSnap _scrollSnap;

        [SerializeField] private Toggle _storyToggle;

        [SerializeField] private Toggle _bonusToggle;
        
        [SerializeField] private StoryItem _storyItemPrefab;

        [SerializeField] private GameObject _storyItemDummyPrefab;
        
        List<StoryItem> _items = new List<StoryItem>();

        private int _dummyCount = 8;
        
        StoryItem CurrentItem
        {
            get
            {
                return _scrollSnap.CurrentObject.GetComponent<StoryItem>();
            }
        }
        
        protected override void Init()
        {
            base.Init();
            InsertDummies(_dummyCount, _dummyCount);
            _scrollSnap.SnapEvent += i =>
            {
                var go = CurrentItem.Button.gameObject;;
                SetSelectedGameObject(go);
                _SelectedObjectOnActivation = go;
                
                UserDataManager.RuntimeInfo.SetLastIndex(i);
            };
        }
        
        protected override void OnActivated()
        {
            base.OnActivated();
            InitItems();
        }

        public void InitItems()
        {
            _title.text = LocalizationManager.Instance.GetTranslationByID(UserDataManager.Instance.CurrentBalanceLocation.Name);
            
            var playerData = UserDataManager.Instance;
            var currentStoryModeInfos = UserDataManager.Instance.CurrentBalanceLocation.CurrentStoryModeStoryInfos;
            
            int index = 0;
            for (int i = 0; i < currentStoryModeInfos.Count; i++)
            {
                index = i;
                var storyInfo =  currentStoryModeInfos[i];
                StoryItem storyItem = null;

                if (_items.Count <= i)
                {
                    storyItem = Instantiate(_storyItemPrefab, _scrollSnap._content);
                    storyItem.transform.SetSiblingIndex(_dummyCount + _items.Count);
                    _items.Add(storyItem);
                }
                else
                {
                    storyItem = _items[i];
                    storyItem.gameObject.SetActive(true);
                }
                
                CoroutineRunner.Instance.Run(ResourcesLoader.LoadStoriesSpriteAsync(storyInfo.Name.Replace("_HUNTER", ""), loaded => storyItem.Icon.sprite = loaded));
                var points = playerData.GameStats.GetPointsCount(storyInfo.Name);
                if (points < 1)
                {
                    storyItem.Points.gameObject.SetActive(false);
                }
                else
                {
                    storyItem.Points.gameObject.SetActive(true);
                }
                storyItem.Points.text = points.ToString();
                storyItem.Lock.SetActive(IsItemLocked(playerData, storyInfo.UnlockInfo, storyInfo.Name, storyInfo));
                storyItem.Button.onClick.RemoveAllListeners();
                int moveto = index;
                storyItem.Button.onClick.AddListener(() =>
                {
                    Play(storyInfo, storyItem, playerData, moveto);
                });
                storyItem.StarsView.Set(playerData.GameStats.GetStarsCount(storyInfo.Name));
                storyItem.Caption.text = LocalizationManager.Instance.GetTranslationByID(storyInfo.Name);
                storyItem.HunterCaption.SetActive(UserDataManager.RuntimeInfo.IsHunterMode);
            }
            
            if (_items.Count >= index)
            {
                for (int i = index + 1; i < _items.Count; i++)
                {
                    _items[i].gameObject.SetActive(false);
                }
            }

            _scrollSnap.StartIndex = 0;
            _scrollSnap.EndIndex = index;
            
            _scrollSnap.Recalculate();
            _scrollSnap.Snap(UserDataManager.RuntimeInfo.GetLastIndex(), true);
        }
        
        void InsertDummies(int left, int right)
        {
            for (int i = 0; i < left; i++)
            {
                Instantiate(_storyItemDummyPrefab, _scrollSnap._content);
            }
            for (int i = 0; i < right; i++)
            {
                Instantiate(_storyItemDummyPrefab, _scrollSnap._content);
            }
        }
        
        private void Play(StoryInfo story, StoryItem storyItem, UserDataManager playerData, int index)
        {
            Debug.Log(story.Name);
            var scrollSnap = storyItem.GetComponent<ScrollSnapItem>();
            if (scrollSnap != null)
            {
                if (!scrollSnap.IsSelected)
                {
                    _scrollSnap.Snap(index, false);
                    return;
                }
            }
            if (IsItemLocked(playerData, story.UnlockInfo, story.Name, story))
            {
                var mode = UserDataManager.RuntimeInfo.LocationModeType == LocationModeType.Classic ? LocalizationManager.Instance.GetTranslation("item_mode_story") : LocalizationManager.Instance.GetTranslation("item_mode_hunter");
                var locationName = LocalizationManager.Instance.GetTranslationByID(UserDataManager.Instance.CurrentBalanceLocation.Name) + " (" + mode + ")";
                
                MainScene.Current.GetDialog<UnlockStoryDialog>().Show(story.UnlockInfo.Price, story.UnlockInfo.StarsLocationUnlockInfo.Stars, LocalizationManager.Instance.GetTranslationByID(story.Name), locationName, story.Name, InitItems);
            }
            else
            {
                UserDataManager.RuntimeInfo.CurrentStory = index;
                MainScene.Current.SwitchScreen<PreGameplayScreen>();
            }
        }
        
        public void OnPlayTap()
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
        
        bool IsItemLocked(UserDataManager playerData, UnlockInfo unlockInfo, string itemName, StoryInfo currentStory = null)
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
                        stars = playerData.GameStats.GetLocationStars(locationLocator.LocationModeType, locationLocator.LocationType);
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

        
        public void OnStoryToggle(bool isOn)
        {
            if (!isOn || UserDataManager.RuntimeInfo.StoryType == StoryType.Story)
            {
                return;
            }
            UserDataManager.RuntimeInfo.StoryType = StoryType.Story;
            InitItems();
        }

        public void OnBonusToggle(bool isOn)
        {
            if (!isOn || UserDataManager.RuntimeInfo.StoryType == StoryType.Bonus)
            {
                return;
            }
            UserDataManager.RuntimeInfo.StoryType = StoryType.Bonus;
            InitItems();
        }

        protected override void XPress(InputAction.CallbackContext ctx = default)
        {
            if (UserDataManager.RuntimeInfo.StoryType == StoryType.Story)
            {
                _bonusToggle.isOn = true;
            }
            else
            {
                _storyToggle.isOn = true;
            }
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }
    }
}