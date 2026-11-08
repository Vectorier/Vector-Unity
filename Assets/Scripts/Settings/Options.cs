using Banzai.Json;

/// <summary>Audio and language options, saved in the player's save file (graphics live in <see cref="GraphicsOptions"/>).</summary>
public class Options : BaseUserHolder<Options>
{
    public float SoundLevel = 1;

    public float MusicLevel = 1;

    public float LastUsedSetSoundValue = 1;

    public float LastUsedSetMusicValue = 1;

    public bool Sound = true;

    public bool Music = true;

    public string Locale;

    protected override string JSONName => "Options";

    public override void ParseData()
    {
        SoundLevel = _userjObject.GetFloat("SoundLevel", 1);
        MusicLevel = _userjObject.GetFloat("MusicLevel", 1);
        LastUsedSetSoundValue = _userjObject.GetFloat("LastUsedSetSoundValue", 1);
        LastUsedSetMusicValue = _userjObject.GetFloat("LastUsedSetMusicValue", 1);
        Sound = _userjObject.GetBool("Sound", true);
        Music = _userjObject.GetBool("Music", true);
        Locale = _userjObject.GetText("Locale");
    }

    public override void SaveData()
    {
        _userjObject["SoundLevel"] = SoundLevel;
        _userjObject["MusicLevel"] = MusicLevel;
        _userjObject["LastUsedSetSoundValue"] = LastUsedSetSoundValue;
        _userjObject["LastUsedSetMusicValue"] = LastUsedSetMusicValue;
        _userjObject["Sound"] = Sound;
        _userjObject["Music"] = Music;
        _userjObject["Locale"] = Locale;
        UserDataManager.Instance.SaveUserDate();
    }

    /// <summary>Pushes the options to the audio and localization systems and saves them.</summary>
    public void Update()
    {
        SoundsManager.Instance.SetSoundLevel(SoundLevel);
        SoundsManager.Instance.SetMusicLevel(MusicLevel);
        LocalizationManager.Instance.ChangeLocale(Locale);
        SaveData();
    }

    public void SetMusic(bool value)
    {
        Music = value;
        MusicLevel = Music ? LastUsedSetMusicValue : 0;
        SoundsManager.Instance.SetMusicPause(!Music);
        Update();
    }

    public void SetSound(bool value)
    {
        Sound = value;
        SoundLevel = Sound ? LastUsedSetSoundValue : 0;
        SoundsManager.Instance.SetSoundsPause(!Sound);
        Update();
    }
}
