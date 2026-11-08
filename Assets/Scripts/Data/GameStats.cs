using System.Collections.Generic;
using System.Linq;
using Banzai.Json;
using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GameStats : BaseUserHolder<GameStats>
{

    private Dictionary<string, Record> _trackStats = new Dictionary<string, Record>();

    protected override string JSONName => "GameStats";

    // Saved as "TrackStats": { "<track>": { "Points": 700, "Stars": 3, "Time": "00:00:56.85" }, ... }
    public override void ParseData()
    {
        _trackStats.Clear();
        var token = _userjObject["TrackStats"];
        if (token == null)
        {
            return;
        }
        if (token.Type == JTokenType.String)
        {
            // Old saves stored the records as a JSON string.
            token = JToken.Parse(token.AsString());
        }
        foreach (var track in token.Children<JProperty>())
        {
            var value = track.Value;
            var record = new Record
            {
                Points = (int?)value["Points"] ?? 0,
                Stars = (int?)value["Stars"] ?? 0,
            };
            var time = (string)(value["Time"] ?? value["TimeSpan"]);
            if (!string.IsNullOrEmpty(time) && TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out var timeSpan))
            {
                record.TimeSpan = timeSpan;
            }
            _trackStats[track.Name] = record;
        }
    }

    public override void SaveData()
    {
        var tracks = new JObject();
        foreach (var pair in _trackStats.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            tracks[pair.Key] = new JObject
            {
                ["Points"] = pair.Value.Points,
                ["Stars"] = pair.Value.Stars,
                ["Time"] = pair.Value.TimeSpan.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture),
            };
        }
        _userjObject["TrackStats"] = tracks;
        UserDataManager.Instance.SaveUserDate();
    }

    public void TrackStatsAdd(string storyName, Record record)
    {
        _trackStats.Add(storyName, record);
        SaveData();
    }

    public Record GetTrackStats(string currentStoryName)
    {
        if (_trackStats.ContainsKey(currentStoryName))
        {
            return _trackStats[currentStoryName];
        }
        return null;
    }

    public int GetStarsCount(string starsInfoStoryId)
    {
        var record = GetTrackStats(starsInfoStoryId);
        if (record != null)
        {
            return record.Stars;
        }
        return 0;
    }

    public int GetPointsCount(string starsInfoStoryId)
    {
        var trackStats = GetTrackStats(starsInfoStoryId);
        if (trackStats != null)
            return GetTrackStats(starsInfoStoryId).Points;
        return 0;
    }

    public void SetTrackStats(Dictionary<string, Record> trackStats)
    {
        _trackStats = trackStats;
    }

    public int GetLocationStars(LocationModeType locationModeType, string locationType)
    {
        var ids = LocationManager.Instance.GetLocationIds(locationModeType, locationType);
        int stars = 0;
        foreach (var id in ids)
        {
            if (_trackStats.ContainsKey(id))
                stars += _trackStats[id].Stars;
        }
        return stars;
    }

    public int GetLocationTrackCompleted(LocationModeType locationModeType, string locationType, uint stars)
    {
        var ids = LocationManager.Instance.GetLocationIds(locationModeType, locationType);
        return ids.Count(id =>
            _trackStats.TryGetValue(id, out var record) &&
            record.Stars >= stars);
    }


    public int GetLocationsTrackCompleted(LocationModeType locationModeType, uint stars)
    {
        int num = 0;
        foreach (var location in LocationManager.Instance.locations.Keys)
        {
            num += GetLocationTrackCompleted(locationModeType, location, stars);
        }
        return num;
    }

    public int GetAllTrackCompleted(uint stars)
    {
        return GetLocationsTrackCompleted(LocationModeType.Classic, stars) + GetLocationsTrackCompleted(LocationModeType.Hunter, stars);
    }

    public int GetLocationsStars(LocationModeType locationModeType)
    {
        int num = 0;
        foreach (var location in LocationManager.Instance.locations.Keys)
        {
            num += GetLocationStars(locationModeType, location);
        }
        return num;
    }

    public int GetAllStars()
    {
        return GetLocationsStars(LocationModeType.Classic) + GetLocationsStars(LocationModeType.Hunter);
    }

}
