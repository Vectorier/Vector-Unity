using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum AntiAliasingMode
{
    Off,
    FXAA,
    SMAA,
    MSAA2x,
    MSAA4x,
    MSAA8x
}

/// <summary>
/// Player graphics settings (VSync, anti-aliasing) stored in PlayerPrefs next to the display settings
/// of <see cref="ResolutionManager"/>. Anti-aliasing is applied to every URP camera: MSAA through the
/// pipeline asset, FXAA / SMAA as the camera's post-process anti-aliasing.
/// </summary>
public static class GraphicsOptions
{
    private const string VSyncKey = "VSyncCount";

    private const string AntiAliasingKey = "AntiAliasing";

    private const AntiAliasingMode DefaultAntiAliasing = AntiAliasingMode.MSAA4x;

    private static bool _initialized;

    private static int _assetMsaaAtStartup = -1;

    public static bool VSync { get; private set; }

    public static AntiAliasingMode AntiAliasing { get; private set; } = DefaultAntiAliasing;

    /// <summary>Loads the saved settings and applies them; also re-applies anti-aliasing to cameras of every scene loaded later.</summary>
    public static void Apply()
    {
        VSync = !Application.isMobilePlatform && PlayerPrefs.GetInt(VSyncKey, 0) != 0;
        var savedAa = PlayerPrefs.GetInt(AntiAliasingKey, (int)DefaultAntiAliasing);
        AntiAliasing = System.Enum.IsDefined(typeof(AntiAliasingMode), savedAa) ? (AntiAliasingMode)savedAa : DefaultAntiAliasing;

        if (!_initialized)
        {
            _initialized = true;
            SceneManager.sceneLoaded += (_, _) => ApplyAntiAliasing();
            Application.quitting += RestorePipelineAsset;
        }

        ApplyFrameRate();
        ApplyAntiAliasing();
    }

    public static void SetVSync(bool enabled)
    {
        VSync = enabled;
        PlayerPrefs.SetInt(VSyncKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyFrameRate();
    }

    public static void SetAntiAliasing(AntiAliasingMode mode)
    {
        AntiAliasing = mode;
        PlayerPrefs.SetInt(AntiAliasingKey, (int)mode);
        PlayerPrefs.Save();
        ApplyAntiAliasing();
    }

    public static AntiAliasingMode NextAntiAliasing()
    {
        int count = System.Enum.GetValues(typeof(AntiAliasingMode)).Length;
        return (AntiAliasingMode)(((int)AntiAliasing + 1) % count);
    }

    public static string GetAntiAliasingName(AntiAliasingMode mode)
    {
        switch (mode)
        {
            case AntiAliasingMode.FXAA: return "FXAA";
            case AntiAliasingMode.SMAA: return "SMAA";
            case AntiAliasingMode.MSAA2x: return "MSAA 2x";
            case AntiAliasingMode.MSAA4x: return "MSAA 4x";
            case AntiAliasingMode.MSAA8x: return "MSAA 8x";
            default: return "Off";
        }
    }

    private static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        // Gameplay runs on its own fixed 60 Hz tick with interpolation, so rendering can follow the display.
        Application.targetFrameRate = Application.isMobilePlatform
            ? Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value)
            : -1;
    }

    private static int MsaaSamples(AntiAliasingMode mode)
    {
        switch (mode)
        {
            case AntiAliasingMode.MSAA2x: return 2;
            case AntiAliasingMode.MSAA4x: return 4;
            case AntiAliasingMode.MSAA8x: return 8;
            default: return 1;
        }
    }

    private static void ApplyAntiAliasing()
    {
        int samples = MsaaSamples(AntiAliasing);
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            if (_assetMsaaAtStartup < 0)
            {
                _assetMsaaAtStartup = urp.msaaSampleCount;
            }
            urp.msaaSampleCount = samples;
        }

        var postAa = AntialiasingMode.None;
        if (AntiAliasing == AntiAliasingMode.FXAA) postAa = AntialiasingMode.FastApproximateAntialiasing;
        else if (AntiAliasing == AntiAliasingMode.SMAA) postAa = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        foreach (var camera in Camera.allCameras)
        {
            camera.allowMSAA = samples > 1;
            var data = camera.GetUniversalAdditionalCameraData();
            data.antialiasing = postAa;
            data.antialiasingQuality = AntialiasingQuality.High;
            // URP runs FXAA / SMAA in its post-processing pass.
            data.renderPostProcessing = postAa != AntialiasingMode.None;
        }
    }

    /// <summary>Keeps the MSAA change from leaking into the project's pipeline asset when playing in the Editor.</summary>
    private static void RestorePipelineAsset()
    {
        if (_assetMsaaAtStartup > 0 && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.msaaSampleCount = _assetMsaaAtStartup;
        }
    }
}
