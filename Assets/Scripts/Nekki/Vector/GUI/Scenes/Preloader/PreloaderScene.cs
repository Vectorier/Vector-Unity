using System;
using System.Collections;
using Nekki.Vector.Core.Animation;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nekki.Vector.GUI.Scenes.Preloader
{
    /// <summary>First scene: loads settings, save data, game data and animations, then opens the main menu.</summary>
    public class PreloaderScene : Scene<PreloaderScene>
    {
        private const int MainSceneIndex = 1;

        [SerializeField] private ProgressBar _progressBar;

        protected override void Init()
        {
            base.Init();

            LocalizationManager.Init();
            ResolutionManager.Init();
            UserDataManager.Init();
            GraphicsOptions.Apply();

            StartCoroutine(LoadProcess());
        }

        private IEnumerator LoadProcess()
        {
            yield return new WaitForEndOfFrame();
            _progressBar.SetValue(0.10f);
            yield return null;

            StoreManager.Init();
            LocationManager.Init();
            _progressBar.SetValue(0.15f);
            yield return null;

            AnimationLoader.Current.Init();
            _progressBar.SetValue(0.20f);
            yield return null;

            var animations = Animations.ToList();
            int count = animations.Count;
            for (int i = 0; i < count; i++)
            {
                animations[i].LoadBin();
                if ((i + 1) % 20 == 0 || i == count - 1)
                {
                    _progressBar.SetValue(0.20f + 0.60f * (i + 1) / count);
                    yield return null;
                }
            }

            GC.Collect();
            _progressBar.SetValue(0.80f);
            yield return null;

            SoundsManager.Instance.Init();
            Game.Loaded = true;

            var load = SceneManager.LoadSceneAsync(MainSceneIndex, LoadSceneMode.Single);
            while (!load.isDone)
            {
                _progressBar.SetValue(0.80f + 0.20f * Mathf.Clamp01(load.progress / 0.9f));
                yield return null;
            }
        }
    }
}
