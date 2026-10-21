using System;
using System.Collections;
using System.Runtime.InteropServices;
using Nekki.Vector.Core.Animation;
using Nekki.Vector.GUI.Scenes.Main.Dialogs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI;
using UI.Dialogs;

namespace Nekki.Vector.GUI.Scenes.Preloader
{
    public class PreloaderScene : Scene<PreloaderScene>
    {
        [SerializeField] private ProgressBar _progressBar;

        [SerializeField] private DialogCanvasController _DialogCanvasController;

        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        private static extern bool SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string className, string windowName);

        protected override void Init()
        {
            string[] args = Environment.GetCommandLineArgs();
            int levelArg = Array.IndexOf(args, "-level");
            if (levelArg >= 0 && levelArg < args.Length - 1)
            {
                string levelName = args[levelArg + 1];
                Game.Instance.Snail = true;
                Game.Instance.SnailSett.SnailLevel = levelName;

                IntPtr windowPtr = FindWindow(null, Application.productName);
                if (windowPtr != IntPtr.Zero)
                {
                    SetWindowText(windowPtr, "Snail Runner");
                }

                int showUIArg = Array.IndexOf(args, "-noui");
                if (showUIArg >= 0)
                {
                    Game.Instance.SnailSett.ShowUI = false;
                }

                int hunterModeArg = Array.IndexOf(args, "-huntermode");
                if (hunterModeArg >= 0)
                {
                    Game.Instance.SnailSett.HunterMode = true;
                }

                int triggerArg = Array.IndexOf(args, "-showtriggers");
                if (triggerArg >= 0)
                {
                    Game.Instance.SnailSett.ShowTriggers = true;
                }

                int platformArg = Array.IndexOf(args, "-showplatforms");
                if (platformArg >= 0)
                {
                    Game.Instance.SnailSett.ShowPlatforms = true;
                }

                int areaArg = Array.IndexOf(args, "-showareas");
                if (areaArg >= 0)
                {
                    Game.Instance.SnailSett.ShowAreas = true;
                }

                int detectorArg = Array.IndexOf(args, "-showdetectors");
                if (detectorArg >= 0)
                {
                    Game.Instance.SnailSett.ShowDetectors = true;
                }
            }

            base.Init();

            LocalizationManager.Init();
            ResolutionManager.Init();
            UserDataManager.Init();
            LicenseCheckingManager.Init();

            LicenseCheckingManager.Instance.RunCheck();

            StartCoroutine(LicenseChecking());
        }

        public IEnumerator LicenseChecking()
        {
            if (UserDataManager.Instance.MainData == null)
            {
                Application.Quit();
                yield
                    break;
            }

            if (UserDataManager.Instance.Options.GDPR)
            {
                UICoroutineRunner.Run(LoadProcess());
                yield
                    break;
            }

            var gdpr = _DialogCanvasController.GetOrCreateDialog<GDPRDialog>();
            gdpr.Show(() =>
            {
                UserDataManager.Instance.Options.GDPR = true;
                UserDataManager.Instance.Options.SaveData();
                UICoroutineRunner.Run(LoadProcess());
            });
        }

        public IEnumerator LoadProcess()
        {
            yield return new WaitForEndOfFrame();
            // Match high-refresh displays while gameplay remains on its fixed 60 Hz tick.
            Application.targetFrameRate = Application.isMobilePlatform
                ? Mathf.RoundToInt((float)UnityEngine.Screen.currentResolution.refreshRateRatio.value)
                : -1;
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
                    float t = (i + 1) / (float)count;
                    float value = 0.20f + 0.60f * t;
                    _progressBar.SetValue(value);

                    yield return null;
                }
            }

            if (count == 0)
            {
                _progressBar.SetValue(0.80f);
                yield return null;
            }

            GC.Collect();
            yield return null;
            SoundsManager.Instance.Init();

#if UNITY_EDITOR
            if (Xml2Prefab.Xml2PrefabLevelContainer.LoadLevel)
            {
                Game.Instance.Snail = true;
                Game.Instance.SnailSett.SnailLevel = "__EDITOR_LEVEL__";
            }
#endif

            if (Game.Instance.Snail)
            {
                if (Game.Instance.SnailSett.HunterMode)
                {
                    UserDataManager.RuntimeInfo.IsHunterMode = true;
                    UserDataManager.Instance.SetHunterMode();
                }

                var load = SceneManager.LoadSceneAsync(2, LoadSceneMode.Single);
                
                while (!load.isDone)
                {
                    var progress = Mathf.Clamp01(load.progress / 0.9f);
                    _progressBar.SetValue(progress * 0.2f + 0.8f);
                    
                    yield return null;
                }
                
                yield return new WaitUntil(() => LevelMainController.current != null);

                LevelMainController.current.pauseRender = false;
                Game.Loaded = true;

                yield break;
            }

            Game.Loaded = true;

            var asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
            while (!asyncLoad.isDone)
            {
                var progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                _progressBar.SetValue(progress * 0.2f + 0.8f);
                yield return null;
            }
        }
    }
}