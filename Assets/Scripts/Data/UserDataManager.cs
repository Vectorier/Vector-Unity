using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Loads and saves the player's progress (coins, stats, options, track records, purchases) as one JSON
/// file: "saves/UserData.data" next to the game on desktop, the persistent data path on mobile.
/// </summary>
public class UserDataManager : AbstractManager<UserDataManager>
{
    /// <summary>2: track stats and shop items are plain nested JSON instead of JSON strings.</summary>
    private const int Version = 2;

    public static readonly string USER_DATA_FOLDER = Application.isMobilePlatform
        ? Application.persistentDataPath
        : Path.Combine(Directory.GetParent(Application.dataPath).FullName, "saves");

    public static readonly string USER_DATA_FILE_NAME = Path.Combine(USER_DATA_FOLDER, "UserData.data");

    public static RuntimeInfo RuntimeInfo = new RuntimeInfo();

    public MainData MainData;

    public Stats Stats;

    public Options Options;

    public GameStats GameStats;

    public ShopData ShopData;

    private JObject _userJsonRoot;

    public LocationInfo CurrentBalanceLocation => LocationManager.Instance.GetLocationInfo(RuntimeInfo.CurentLocationType, RuntimeInfo.LocationModeType);

    protected override void InitInternal()
    {
        Directory.CreateDirectory(USER_DATA_FOLDER);

        _userJsonRoot = LoadDataFromFile();
        RemoveObsoleteKeys(_userJsonRoot);
        MainData = MainData.Create(_userJsonRoot);
        Stats = Stats.Create(_userJsonRoot);
        Options = Options.Create(_userJsonRoot);
        GameStats = GameStats.Create(_userJsonRoot);
        ShopData = ShopData.Create(_userJsonRoot);

        // Tricks every player starts with.
        foreach (var trick in new[] { "TRICK_JUMPTUMBLE", "TRICK_SPINNINGVAULT" })
        {
            if (!ShopData.IsBought(trick))
            {
                ShopData.Add(trick, 1);
            }
        }
        SetClassicMode();

        Options.Locale ??= LocalizationManager.Instance.GetSystem();
        Options.Update();

        // Rewrite older saves in the current, hand-editable layout.
        if ((int?)_userJsonRoot["V"] != Version)
        {
            _userJsonRoot["V"] = Version;
            GameStats.SaveData();
            ShopData.SaveData();
        }
    }

    /// <summary>Drops keys written by older versions of the game that nothing reads any more.</summary>
    private static void RemoveObsoleteKeys(JObject root)
    {
        root.Remove("OldData");
        (root["MainData"] as JObject)?.Remove("Unlock");
        if (root["Options"] is JObject options)
        {
            foreach (var key in new[] { "GDPR", "CurrentLocaleIndex", "GameRated", "ShowRateGameNextTime" })
            {
                options.Remove(key);
            }
        }
    }

    private static JObject LoadDataFromFile()
    {
        if (File.Exists(USER_DATA_FILE_NAME))
        {
            try
            {
                var content = File.ReadAllText(USER_DATA_FILE_NAME);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return JObject.Parse(content);
                }
            }
            catch (System.Exception e)
            {
                // Keep the unreadable file for the player instead of overwriting it on the next save.
                var backup = USER_DATA_FILE_NAME + ".corrupt";
                File.Copy(USER_DATA_FILE_NAME, backup, true);
                Debug.LogError($"Save file could not be read and was copied to {backup}: {e.Message}");
            }
        }
        return new JObject { ["V"] = Version };
    }

    public void SetClassicMode()
    {
        RuntimeInfo.LocationModeType = LocationModeType.Classic;
    }

    public void SetHunterMode()
    {
        RuntimeInfo.LocationModeType = LocationModeType.Hunter;
    }

    public void SaveUserDate()
    {
        File.WriteAllText(USER_DATA_FILE_NAME, _userJsonRoot.ToString());
    }
}
