using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ContinuousLoadAmmo.Patches;
using ContinuousLoadAmmo.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace ContinuousLoadAmmo;

[BepInPlugin("com.ozen.continuousloadammo", "Continuous Load Ammo", "1.1.1")]
[BepInDependency("Tyfon.UIFixes", BepInDependency.DependencyFlags.SoftDependency)]
[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public class ContinuousLoadAmmo : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static ConfigEntry<float> SpeedLimit;
    public static ConfigEntry<bool> ReachableOnly;
    public static ConfigEntry<bool> InventoryTabs;
    public static ConfigEntry<bool> PrioritizeHighestPenetration;
    public static ConfigEntry<KeyboardShortcut> LoadAmmoHotkey;

    public void Awake()
    {
        LogSource = Logger;

        SpeedLimit = Config.Bind("General", "Speed Limit", 0.31f, new ConfigDescription("The speed limit, as a percentage of the walk speed, set to the player while loading ammo", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes() { Order = 4, ShowRangeAsPercent = true }));
        ReachableOnly = Config.Bind("General", "Reachable Places Only", true, new ConfigDescription("Allow loading ammo outside the inventory only when Magazine and Ammo is in your Vest, Pockets, or Secure Container", null, new ConfigurationManagerAttributes() { Order = 3 }));
        InventoryTabs = Config.Bind("General", "Inventory Tabs", true, new ConfigDescription("Do not interrupt loading ammo when switching inventory tabs (maps tab, tasks tab, etc.)", null, new ConfigurationManagerAttributes() { Order = 2 }));
        LoadAmmoHotkey = Config.Bind("General", "Load Ammo Hotkey", new KeyboardShortcut(KeyCode.K), new ConfigDescription("Key used to load ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 1 }));
        PrioritizeHighestPenetration = Config.Bind("General", "Prioritize Highest Penetration", true, new ConfigDescription("When using Load Ammo Hotkey, choose ammo that has the highest penetration power if Enabled. If Disabled, prioritize the same ammo in the weapon's magazine", null, new ConfigurationManagerAttributes() { Order = 0 }));

        var patchManager = new PatchManager(this, true);
        patchManager.EnablePatches();

        if (MultiSelectInterop.StopLoadingMethod != null)
        {
            new ScreensPatches.MultiSelectStopLoadingPatch().Enable();
        }
    }
}
