using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ContinuousLoadAmmo.Models;
using ContinuousLoadAmmo.Patches;
using ContinuousLoadAmmo.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace ContinuousLoadAmmo;

[BepInPlugin("com.ozen.continuousloadammo", "Continuous Load Ammo", "1.1.6")]
[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Tyfon.UIFixes", BepInDependency.DependencyFlags.SoftDependency)] // Old version; TODO: Remove in 4.1.x
[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public class ContinuousLoadAmmo : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static ConfigEntry<float> SpeedLimit;
    public static ConfigEntry<bool> ReachableOnly;
    public static ConfigEntry<bool> InventoryTabs;
    public static ConfigEntry<bool> MagPresetFallback;
    public static ConfigEntry<bool> QuickLoadNotify;
    public static ConfigEntry<QuickLoadMode> QuickLoadMode;
    public static ConfigEntry<KeyboardShortcut> QuickLoadHotkey;

    public void Awake()
    {
        LogSource = Logger;

        SpeedLimit = Config.Bind(
            "General",
            "Speed Limit",
            0.45f,
            new ConfigDescription(
                "The speed limit, as a percentage of the walk speed, set to the player while loading ammo",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 6, ShowRangeAsPercent = true }
            )
        );
        ReachableOnly = Config.Bind(
            "General",
            "Reachable Places Only",
            true,
            new ConfigDescription(
                "Allow loading ammo outside the inventory only when Magazine and Ammo is in your Vest, Pockets, or Secure Container",
                null,
                new ConfigurationManagerAttributes { Order = 5 }
            )
        );
        InventoryTabs = Config.Bind(
            "General",
            "Inventory Tabs",
            true,
            new ConfigDescription(
                "Do not interrupt loading ammo when switching inventory tabs (maps tab, tasks tab, etc.)",
                null,
                new ConfigurationManagerAttributes { Order = 4 }
            )
        );
        MagPresetFallback = Config.Bind(
            "General",
            "Mag Preset Fallback",
            true,
            new ConfigDescription(
                "Fallback to quick load if magazine preset is unavailable",
                null,
                new ConfigurationManagerAttributes { Order = 3 }
            )
        );
        QuickLoadHotkey = Config.Bind(
            "Quick Load",
            "Hotkey",
            new KeyboardShortcut(KeyCode.K),
            new ConfigDescription("Key used to load ammo outside the inventory", null, new ConfigurationManagerAttributes { Order = 2 })
        );
        QuickLoadMode = Config.Bind(
            "Quick Load",
            "Mode",
            Models.QuickLoadMode.LastMagazinePreset,
            new ConfigDescription(
                "Highest Penetration Available - choose ammo that has the highest penetration power.\nLast Bullet in Magazine - prioritize the last ammo of the current weapon's magazine.\nLast Used Magazine Preset - load the last used magazine preset",
                null,
                new ConfigurationManagerAttributes { Order = 1 }
            )
        );
        QuickLoadNotify = Config.Bind(
            "Quick Load",
            "Notify",
            true,
            new ConfigDescription(
                "When using Quick Load, notify the player of the ammo being loaded",
                null,
                new ConfigurationManagerAttributes { Order = 0 }
            )
        );

        var patchManager = new PatchManager(this, true);
        patchManager.EnablePatches();

        if (MultiSelectInterop.StopLoadingMethod is not null)
        {
            new ScreensPatches.MultiSelectStopLoadingPatch().Enable();
        }

        ProfileMagazinePresetStore.LoadProfileLastPresets();
    }
}
