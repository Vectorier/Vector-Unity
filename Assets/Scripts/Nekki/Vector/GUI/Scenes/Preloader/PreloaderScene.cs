using System;
using System.Collections;
using GameDiagnostics;
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

            // A step that fails is logged and shown; the game goes on to the menu instead of stopping here.
            Step("Loading the localization", "The texts may show their keys.", LocalizationManager.Init);
            Step("Setting the resolution", "The default resolution is used.", ResolutionManager.Init);
            Step("Loading the save data", "The game may start without the saved progress.", UserDataManager.Init);
            Step("Applying the graphics options", "The default graphics options are used.", GraphicsOptions.Apply);

            StartCoroutine(LoadProcess());
        }

        private IEnumerator LoadProcess()
        {
            yield return new WaitForEndOfFrame();
            _progressBar.SetValue(0.10f);
            yield return null;

            Step("Loading the shop (commons)", "The shop may be empty.", StoreManager.Init);
            Step("Loading the levels list (commons)", "Some levels may be missing from the menus.", LocationManager.Init);
            _progressBar.SetValue(0.15f);
            yield return null;

            Step("Loading the moves (moves.xml)", "No moves were loaded: levels cannot be played until moves.xml is fixed.", AnimationLoader.Current.Init);
            _progressBar.SetValue(0.20f);
            yield return null;

            var animations = Animations.ToList();
            int count = animations.Count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    animations[i].LoadBin();
                }
                catch (Exception ex)
                {
                    GameLog.Recovered(ex, "Loading the moves", $"The move \"{animations[i].Name}\" has no frames.");
                }
                if ((i + 1) % 20 == 0 || i == count - 1)
                {
                    _progressBar.SetValue(0.20f + 0.60f * (i + 1) / count);
                    yield return null;
                }
            }

            GC.Collect();
            _progressBar.SetValue(0.80f);
            yield return null;

            Step("Loading the sounds", "Some sounds may be silent.", () => SoundsManager.Instance.Init());
            Game.Loaded = true;

            var load = SceneManager.LoadSceneAsync(MainSceneIndex, LoadSceneMode.Single);
            while (!load.isDone)
            {
                _progressBar.SetValue(0.80f + 0.20f * Mathf.Clamp01(load.progress / 0.9f));
                yield return null;
            }
        }

        /// <summary>One loading step: an error in it is logged and shown, and the loading goes on.</summary>
        private static void Step(string stage, string outcome, Action step)
        {
            try
            {
                step();
            }
            catch (Exception ex)
            {
                GameLog.Error(ex, stage, outcome);
            }
        }
    }
}
