using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoundsManager : MonoBehaviour
{
    private const string PREFAB_PATH = "Prefabs/SoundsManager";

    [Header("Audio Sources")]
    [Tooltip("Used as the first SFX channel; additional channels are created at runtime.")]
    public AudioSource AudioSourceMain;              // SFX channel #0 (pooled)
    [SerializeField] private AudioSource AudioSourceBackground;     // Music (current track)

    [Header("SFX Channel Pool")]
    [Min(1)] public int initialSfxChannels = 11;      // includes AudioSourceMain if assigned
    [Min(1)] public int maxSfxChannels = 16;
    public bool autoExpandPool = true;

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultSfxVolume = 1f;
    [Range(0f, 1f)] public float defaultMusicVolume = 1f;

    private static SoundsManager _instance;
    public static SoundsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var prefab = Resources.Load<GameObject>(PREFAB_PATH);
                var obj = Instantiate(prefab);
                obj.name = "SoundsManager";
                DontDestroyOnLoad(obj);
                _instance = obj.GetComponent<SoundsManager>();
            }
            return _instance;
        }
    }

    private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
    private Transform _sfxContainer;

    private void Awake()
    {
        // Singleton guard for scene-dropped instances
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        BuildPool();
        Init();
    }

    private void BuildPool()
    {
        // Container object (keeps Hierarchy tidy)
        _sfxContainer = new GameObject("SFX_Channels").transform;
        _sfxContainer.SetParent(transform, false);

        // Use AudioSourceMain as first SFX channel if provided
        if (AudioSourceMain != null)
        {
            PrepareSfxSource(AudioSourceMain);
            _sfxSources.Add(AudioSourceMain);
        }

        // Create extra channels up to initialSfxChannels
        int need = Mathf.Max(1, initialSfxChannels) - _sfxSources.Count;
        for (int i = 0; i < need; i++)
        {
            _sfxSources.Add(CreateSfxSource());
        }

        // Default volumes
        _musicLevel = defaultMusicVolume;
        EnsureMusicSources();
        ApplyMusicVolume();
        foreach (var s in _sfxSources) s.volume = defaultSfxVolume;
    }

    private AudioSource CreateSfxSource()
    {
        var go = new GameObject($"SFX_Channel_{_sfxSources.Count}");
        go.transform.SetParent(_sfxContainer, false);
        var src = go.AddComponent<AudioSource>();
        PrepareSfxSource(src);
        return src;
    }

    private void PrepareSfxSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.volume = AudioSourceMain.volume;
        src.loop = false;
        src.spatialBlend = 0f; // 2D by default
        src.dopplerLevel = 0f;
    }

    private AudioSource GetFreeSfxSource()
    {
        // Prefer an idle source
        var idle = _sfxSources.FirstOrDefault(s => !s.isPlaying);
        if (idle != null) return idle;

        // Expand if allowed and under cap
        if (autoExpandPool && _sfxSources.Count < Mathf.Max(1, maxSfxChannels))
        {
            var extra = CreateSfxSource();
            _sfxSources.Add(extra);
            return extra;
        }

        // Steal the one that�s closest to finishing (least remaining time)
        // Fallback if all are busy and expansion is not allowed or maxed
        AudioSource candidate = _sfxSources[0];
        float minRemaining = Remaining(candidate);
        for (int i = 1; i < _sfxSources.Count; i++)
        {
            float rem = Remaining(_sfxSources[i]);
            if (rem < minRemaining)
            {
                minRemaining = rem;
                candidate = _sfxSources[i];
            }
        }
        candidate.Stop(); // preempt
        return candidate;

        float Remaining(AudioSource s)
        {
            if (s.clip == null || !s.isPlaying) return 0f;
            return Mathf.Max(0f, s.clip.length - s.time);
        }
    }

    public void Init() { /* reserved for future setup */ }

    // ======== Global Controls ========

    public void SetMusicPause(bool state)
    {
        AudioSourceBackground.mute = state;
        if (_musicFadeSource != null) _musicFadeSource.mute = state;
    }

    public void SetSoundsPause(bool state)
    {
        foreach (var s in _sfxSources) s.mute = state;
    }

    public void SetSoundLevel(float value)
    {
        foreach (var s in _sfxSources) s.volume = value;
    }

    public void SetMusicLevel(float value)
    {
        _musicLevel = value;
        ApplyMusicVolume();
    }

    // ======== Music / Background ========
    // Two sources: AudioSourceBackground always holds the current track, _musicFadeSource the one fading out
    // during a crossfade. Each source's volume is the player's music level times its own fade gain. Every
    // music operation stops the running fade first and starts from the current gains, so calls can interrupt
    // each other at any time (pause/unpause spam, leaving a level mid-fade, a new track mid-crossfade).

    private AudioSource _musicFadeSource;
    private float _musicLevel = 1f;
    private float _musicGain = 1f;
    private float _fadeSourceGain;
    private bool _musicPaused;
    private bool _musicFadingOut;
    private float _musicDuck = 1f;
    private Coroutine _duckFade;
    private Coroutine _musicFade;
    private int _musicRequest;

    private void EnsureMusicSources()
    {
        if (_musicFadeSource != null) return;
        _musicFadeSource = gameObject.AddComponent<AudioSource>();
        _musicFadeSource.playOnAwake = false;
        _musicFadeSource.loop = true;
        _musicFadeSource.spatialBlend = 0f;
        _musicFadeSource.outputAudioMixerGroup = AudioSourceBackground.outputAudioMixerGroup;
        _musicFadeSource.mute = AudioSourceBackground.mute;
    }

    private void ApplyMusicVolume()
    {
        AudioSourceBackground.volume = _musicLevel * _musicGain * _musicDuck;
        if (_musicFadeSource != null) _musicFadeSource.volume = _musicLevel * _fadeSourceGain * _musicDuck;
    }

    private void StopMusicFade()
    {
        if (_musicFade != null)
        {
            StopCoroutine(_musicFade);
            _musicFade = null;
        }
    }

    public void StopBackground()
    {
        EnsureMusicSources();
        StopMusicFade();
        _musicRequest++;
        _musicPaused = false;
        _musicFadingOut = false;
        AudioSourceBackground.Stop();
        _musicFadeSource.Stop();
    }

    public void PlayBackground(MusicType musicType, float crossfade = 0f) => PlayBackground(musicType.ToString(), crossfade);

    /// <summary>Plays a music track, crossfading from the current one over <paramref name="crossfade"/> seconds.</summary>
    public void PlayBackground(string musicName, float crossfade = 0f)
    {
        EnsureMusicSources();
        int request = ++_musicRequest;
        StartCoroutine(ResourcesLoader.LoadMusicClipAsync(musicName, loaded =>
        {
            if (request != _musicRequest)
            {
                return; // a newer music request replaced this one while it was loading
            }
            if (loaded == null)
            {
                Debug.LogError("Music clip not found " + musicName);
                return;
            }
            StartTrack(loaded, crossfade);
        }));
    }

    private void StartTrack(AudioClip clip, float crossfade)
    {
        StopMusicFade();
        bool audible = AudioSourceBackground.isPlaying && !_musicPaused && !_musicFadingOut && _musicGain > 0f;
        _musicPaused = false;
        _musicFadingOut = false;

        if (crossfade <= 0f || !audible)
        {
            _musicFadeSource.Stop();
            _fadeSourceGain = 0f;
        }
        else
        {
            // The current track moves to the fade source and fades out from where it is.
            (AudioSourceBackground, _musicFadeSource) = (_musicFadeSource, AudioSourceBackground);
            _fadeSourceGain = _musicGain;
        }

        AudioSourceBackground.Stop();
        AudioSourceBackground.clip = clip;
        AudioSourceBackground.loop = true;
        _musicGain = crossfade > 0f ? 0f : 1f;
        ApplyMusicVolume();
        AudioSourceBackground.Play();

        if (crossfade > 0f)
        {
            _musicFade = StartCoroutine(FadeMusic(1f, crossfade, () => _musicFadeSource.Stop()));
        }
    }

    /// <summary>
    /// Fades the music out and pauses it; <see cref="ResumeMusic"/> brings it back. Returns false (and does
    /// nothing) when there is no playing track to pause.
    /// </summary>
    public bool PauseMusic(float fade = 0.25f)
    {
        EnsureMusicSources();
        if (_musicPaused || _musicFadingOut || !AudioSourceBackground.isPlaying) return false;
        _musicPaused = true;
        StopMusicFade();
        _musicFade = StartCoroutine(FadeMusic(0f, fade, () =>
        {
            AudioSourceBackground.Pause();
            _musicFadeSource.Stop();
        }));
        return true;
    }

    /// <summary>Unpauses music paused by <see cref="PauseMusic"/> and fades it back in. Does nothing otherwise.</summary>
    public void ResumeMusic(float fade = 0.5f)
    {
        EnsureMusicSources();
        if (!_musicPaused) return;
        _musicPaused = false;
        StopMusicFade();
        AudioSourceBackground.UnPause();
        _musicFade = StartCoroutine(FadeMusic(1f, fade, null));
    }

    /// <summary>Fades the music out over <paramref name="fade"/> seconds, then stops it.</summary>
    public void FadeOutMusic(float fade)
    {
        EnsureMusicSources();
        _musicRequest++;
        _musicPaused = false;
        _musicFadingOut = true;
        StopMusicFade();
        _musicFade = StartCoroutine(FadeMusic(0f, fade, () =>
        {
            AudioSourceBackground.Stop();
            _musicFadeSource.Stop();
        }));
    }

    /// <summary>
    /// Temporarily lowers the music to <paramref name="target"/> (0-1) of its normal volume, e.g. 0.5 while the
    /// player is dead, and back with 1. It multiplies the player's music volume and the other fades, so the
    /// settings slider, mute and pause/track fades keep working; each call replaces the previous duck fade.
    /// </summary>
    public void DuckMusic(float target, float fade)
    {
        EnsureMusicSources();
        if (_duckFade != null)
        {
            StopCoroutine(_duckFade);
            _duckFade = null;
        }
        target = Mathf.Clamp01(target);
        if (fade <= 0f || !isActiveAndEnabled)
        {
            _musicDuck = target;
            ApplyMusicVolume();
            return;
        }
        _duckFade = StartCoroutine(FadeDuck(target, fade));
    }

    private IEnumerator FadeDuck(float target, float duration)
    {
        float start = _musicDuck;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _musicDuck = Mathf.Lerp(start, target, t / duration);
            ApplyMusicVolume();
            yield return null;
        }
        _musicDuck = target;
        ApplyMusicVolume();
        _duckFade = null;
    }

    /// <summary>Moves the current track's gain to <paramref name="target"/> while any fading-out track goes to 0.</summary>
    private IEnumerator FadeMusic(float target, float duration, System.Action onDone)
    {
        float startGain = _musicGain;
        float startFadeGain = _fadeSourceGain;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            _musicGain = Mathf.Lerp(startGain, target, k);
            _fadeSourceGain = Mathf.Lerp(startFadeGain, 0f, k);
            ApplyMusicVolume();
            yield return null;
        }
        _musicGain = target;
        _fadeSourceGain = 0f;
        ApplyMusicVolume();
        _musicFade = null;
        onDone?.Invoke();
    }

    // ======== SFX: Single / Parallel ========

    public void PlaySounds(params SoundType[] soundTypes)
    {
        string[] names = soundTypes.Select(s => s.ToString()).ToArray();
        PlaySounds(names);
    }

    /// <summary>Plays all sounds in parallel on free channels.</summary>
    public void PlaySounds(params string[] soundNames)
    {
        foreach (var name in soundNames)
        {
            StartCoroutine(ResourcesLoader.LoadAudioClipAsync(name, clip =>
            {
                if (clip == null)
                {
                    Debug.LogError("Clip not found " + name);
                    return;
                }
                var src = GetFreeSfxSource();
                src.pitch = 1f;
                src.spatialBlend = 0f;
                src.PlayOneShot(clip);
            }));

        }
    }

    /// <summary>Play one sound once with custom volume and optional pitch.</summary>
    public void PlaySoundsOnce(string soundName, float volume, float pitch = 1f)
    {
        StartCoroutine(ResourcesLoader.LoadAudioClipAsync(soundName, clip =>
        {
            if (clip == null)
            {
                Debug.LogError("Clip not found " + soundName);
                return;
            }
            var src = GetFreeSfxSource();
            src.pitch = pitch;
            src.spatialBlend = 0f;
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
        }));
    }

    /// <summary>3D variant at a world position.</summary>
    public void PlaySoundAt(string soundName, Vector3 position, float minDistance = 1f, float maxDistance = 25f, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
    {
        StartCoroutine(ResourcesLoader.LoadAudioClipAsync(soundName, clip =>
        {
            if (clip == null)
            {
                Debug.LogError("Clip not found " + soundName);
                return;
            }
            var src = GetFreeSfxSource();
            src.transform.position = position;
            src.pitch = pitch;
            src.spatialBlend = Mathf.Clamp01(spatialBlend);
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
        }));        
    }

    // ======== SFX: Sequential ========

    /// <summary>Plays the provided sounds sequentially on a dedicated channel.</summary>
    public void PlaySoundsSequential(params string[] soundNames)
    {
        StartCoroutine(PlaySequentially(soundNames));
    }

    /// <summary>Plays SoundTypes sequentially.</summary>
    public void PlaySoundsSequential(params SoundType[] soundTypes)
    {
        string[] names = soundTypes.Select(s => s.ToString()).ToArray();
        StartCoroutine(PlaySequentially(names));
    }

    private IEnumerator PlaySequentially(params string[] soundNames)
    {
        var src = GetFreeSfxSource();
        src.pitch = 1f;
        src.spatialBlend = 0f;

        foreach (var name in soundNames)
        {
            AudioClip clip = null;

            yield return ResourcesLoader.LoadAudioClipAsync(name, loaded => clip = loaded);

            if (clip == null)
            {
                Debug.LogError("Clip not found " + name);
                continue;
            }

            src.clip = clip;
            src.Play();

            // Wait for the clip to finish
            while (src != null && src.isPlaying)
                yield return null;
        }
    }

    // ======== Utilities ========

    public void StopAllSfx()
    {
        foreach (var s in _sfxSources) s.Stop();
    }

    public bool IsAnySfxPlaying()
    {
        return _sfxSources.Any(s => s.isPlaying);
    }
}