using DG.Tweening;
using Nekki.Vector.Core;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nekki.Vector.GUI.Scenes.Main.Screens
{
    public class PostGameplayScreen : UIModule
    {
        [SerializeField] StarsView _starsView;

        [SerializeField] private ItemStoryView _time;
        [SerializeField] private ItemStoryView _score;
        [SerializeField] private ItemStoryView _bonuses;
        [SerializeField] private ItemStoryView _tricks;

        [SerializeField] private Text _title;

        [SerializeField] private Color _newBestColor;

        private int _starRecord;
        private int[] _coinsByStars;
        private Sequence _starsSequence;


        protected override void OnActivated()
        {
            base.OnActivated();

            SetScores();


            _starsSequence = _starsView.SetWithAnimation(_starRecord, _coinsByStars);
            _starsSequence.Play();
        }

        void SetScores()
        {
            var currentStory =
                UserDataManager.Instance.CurrentBalanceLocation.CurrentStoryModeStoryInfos[
                    UserDataManager.RuntimeInfo.CurrentStory];
            var userData = UserDataManager.Instance;
            var stats = userData.GameStats.GetTrackStats(currentStory.Name);
            var res = LevelResult.LastLevelResult;

            _starRecord = 1;
            if (res.bonusCollected == res.bonusMax)
            {
                _starRecord = 2;
            }

            if (res.trickCollected == res.trickMax)
            {
                _starRecord++;
            }

            var record = new Record();
            record.Points = res.pointsCollected;
            record.Stars = _starRecord;
            record.TimeSpan = res.timeSpan;

            var stars = 0;
            if (stats != null)
            {
                stars = stats.Stars;
            }

            _coinsByStars = new int[3];

            var rewardData = LocationManager.Instance.GetRewards(currentStory.RewardTemplate);
            var extension = rewardData.GetReward(stars, _starRecord);
            if (_starRecord > 0)
            {
                for (int i = 0; i < _starRecord; i++)
                {
                    if (i < stars)
                    {
                        _coinsByStars[i] = -1;
                    }
                    else
                    {
                        _coinsByStars[i] = rewardData[i + 1];
                    }
                }
            }
            else
            {
                extension = 0;
            }

            userData.MainData.AddCoins(extension);


            _title.text = LocalizationManager.Instance.GetTranslationByID(currentStory.Name);
            UpdateTimeLabel(res, stats);
            UpdateScoreLabel(res, stats);
            _bonuses.Current.text = string.Format("{0}/{1}", res.bonusCollected, res.bonusMax);
            _tricks.Current.text = string.Format("{0}/{1}", res.trickCollected, res.trickMax);
            if (stats == null)
            {
                userData.GameStats.TrackStatsAdd(currentStory.Name, record);
            }
            else
            {
                var timeSpan = res.timeSpan;
                if (stats.TimeSpan > timeSpan)
                {
                    timeSpan = stats.TimeSpan;
                }

                stats.Points = Mathf.Max(stats.Points, res.pointsCollected);
                stats.Stars = Mathf.Max(stats.Stars, _starRecord);
                stats.TimeSpan = timeSpan;

                userData.GameStats.SaveData();
            }

            userData.SaveUserDate();
        }

        private void UpdateTimeLabel(LevelResult res, Record trackStatRecord)
        {
            _time.BestMax.gameObject.SetActive(trackStatRecord != null);
            _time.Current.text = res.timeSpan.ToString("mm\\:ss\\:ff");
            if (trackStatRecord != null)
                _time.BestMax.text = trackStatRecord.TimeSpan.ToString("mm\\:ss\\:ff");
            bool flag = true;
            if (trackStatRecord != null)
            {
                flag = res.timeSpan <= trackStatRecord.TimeSpan;
            }

            if (flag)
            {
                _time.BestMax.color = Color.white;
                _time.Current.color = _newBestColor;
            }
            else
            {
                _time.Current.color = Color.white;
                _time.BestMax.color = _newBestColor;
            }
        }

        private void UpdateScoreLabel(LevelResult res, Record trackStatRecord)
        {
            _score.BestMax.gameObject.SetActive(trackStatRecord != null);
            _score.Current.text = string.Format("{0}", res.pointsCollected);
            if (trackStatRecord != null)
                _score.BestMax.text = string.Format("{0}", trackStatRecord.Points);
            bool flag = true;
            if (trackStatRecord != null)
            {
                flag = trackStatRecord.Points <= res.pointsCollected;
            }

            if (flag)
            {
                _score.BestMax.color = Color.white;
                _score.Current.color = _newBestColor;
            }
            else
            {
                _score.Current.color = Color.white;
                _score.BestMax.color = _newBestColor;
            }
        }

        private void PlayNext()
        {
            if (UserDataManager.RuntimeInfo.CurrentStory <
                UserDataManager.Instance.CurrentBalanceLocation.CurrentStoryModeStoryInfos.Count - 1)
            {
                var nextStory =
                    UserDataManager.Instance.CurrentBalanceLocation.CurrentStoryInfos[
                        UserDataManager.RuntimeInfo.CurrentStory + 1];
                if (IsItemLocked(UserDataManager.Instance, nextStory.UnlockInfo,
                        nextStory.Name))
                {
                    MainScene.Current.ClearHistoryUntil<SelectStoryScreen>();
                    MainScene.Current.GoToPreviousScreen();
                    return;
                }

                UserDataManager.RuntimeInfo.CurrentStory++;
                
                MainScene.Current.ClearHistoryUntil<PreGameplayScreen>();
                MainScene.Current.GoToPreviousScreen();
            }
            else
            {
                MainScene.Current.ClearHistoryUntil<SelectLocationScreen>();
                MainScene.Current.GoToPreviousScreen();
            }

            SoundsManager.Instance.PlayBackground(MusicType.menu);
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
        
        public void OnContinueTap()
        {
            PlayNext();
        }

        public void OnReloadTap()
        {
            MainScene.Current.ClearHistoryUntil<PreGameplayScreen>();
            MainScene.Current.GoToPreviousScreen();
        }

        public void OnBackTap()
        {
            MainScene.Current.ClearHistoryUntil<SelectStoryScreen>();
            MainScene.Current.GoToPreviousScreen();
        }

        protected override void YPress(InputAction.CallbackContext ctx = default)
        {
            OnReloadTap();
        }

        protected override void BackPress(InputAction.CallbackContext ctx = default)
        {
            OnBackTap();
        }
    }
}