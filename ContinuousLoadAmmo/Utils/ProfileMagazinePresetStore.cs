using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EFT;
using Newtonsoft.Json;
using SPT.Reflection.Utils;

namespace ContinuousLoadAmmo.Utils;

public static class ProfileMagazinePresetStore
{
    private const string StoreFilename = "lastMagPresets.json";

    // TODO: 4.1.x, move assembly to its own folder
    // TODO: 4.1.x, update storeFileNameDirectory
    private static readonly string _storeFileNameDirectory = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "ozen-ContinuousLoadAmmo"
    );

    private static readonly string _pathToStoreFile = Path.Combine(_storeFileNameDirectory, StoreFilename);

    private static ProfileLastMagPresets _profileLastMagPresets = [];

    private static string ProfileId => field ??= ClientAppUtils.GetMainApp().Session.Profile.ProfileId;

    public static MagazineBuildPresetClass GetMagPreset(string caliber)
    {
        if (!_profileLastMagPresets.TryGetValue(ProfileId, out var caliberLastPreset)) return null;
        if (!caliberLastPreset.TryGetValue(caliber, out var presetId)) return null;

        var magBuildsStorage = ClientAppUtils.GetClientApp().Session.MagBuildsStorage;
        return magBuildsStorage?.TryFindPresetById(presetId, out var preset) == true ? preset : null;
    }

    public static void UpdateMagPreset(MagazineBuildPresetClass preset)
    {
        if (!_profileLastMagPresets.TryGetValue(ProfileId, out var caliberLastPreset))
        {
            caliberLastPreset = [];
            _profileLastMagPresets[ProfileId] = caliberLastPreset;
        }

        var caliber = preset.GetCaliberReally();
        if (caliberLastPreset.TryGetValue(caliber, out var prevPresetId) && prevPresetId == preset.Id)
        {
            return;
        }

        caliberLastPreset[caliber] = preset.Id;
        SaveProfileLastPresets();
    }

    public static void LoadProfileLastPresets()
    {
        if (!File.Exists(_pathToStoreFile))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_pathToStoreFile);

            _profileLastMagPresets = JsonConvert.DeserializeObject<ProfileLastMagPresets>(json);
        }
        catch (Exception ex)
        {
            ContinuousLoadAmmo.LogSource.LogWarning(ex);
            ContinuousLoadAmmo.LogSource.LogWarning("Caught an exception while trying to load user last used magazine presets");
        }
    }

    private static void SaveProfileLastPresets()
    {
        if (!Directory.Exists(_storeFileNameDirectory))
        {
            Directory.CreateDirectory(_storeFileNameDirectory);
        }

        try
        {
            var json = JsonConvert.SerializeObject(_profileLastMagPresets);
            File.WriteAllText(_pathToStoreFile, json);
        }
        catch (Exception ex)
        {
            ContinuousLoadAmmo.LogSource.LogWarning(ex);
            ContinuousLoadAmmo.LogSource.LogWarning("Caught an exception while trying to save user last used magazine presets");
        }
    }
}

/// <summary>
/// Key: ProfileId, Value: Key: Caliber, Value: Mag Preset ID
/// </summary>
public class ProfileLastMagPresets : Dictionary<string, CaliberLastPreset>;

/// <summary>
/// Key: Caliber, Value: Mag Preset ID
/// </summary>
public class CaliberLastPreset : Dictionary<string, MongoID>;
