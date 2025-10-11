using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using ContinuousLoadAmmo.Components;
using ContinuousLoadAmmo.Patches;
using EFT;
using UnityEngine;

namespace ContinuousLoadAmmo
{
    [BepInPlugin("com.ozen.continuousloadammo", "ContinuousLoadAmmo", "1.0.9")]
    [BepInDependency("Tyfon.UIFixes", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static ConfigEntry<float> SpeedLimit;
        public static ConfigEntry<bool> ReachableOnly;
        public static ConfigEntry<bool> InventoryTabs;
        public static ConfigEntry<KeyboardShortcut> CancelHotkey;
        public static ConfigEntry<KeyboardShortcut> CancelHotkeyAlt;
        public static ConfigEntry<KeyboardShortcut> LoadAmmoHotkey;
        public static ConfigEntry<bool> PrioritizeHighestPenetration;

        public static LoadAmmoUI LoadAmmoUI;

        protected void Awake()
        {
            LogSource = Logger;
            SpeedLimit = Config.Bind("General", "Speed Limit", 0.31f, new ConfigDescription("The speed limit, as a percentage of the walk speed, set to the player while loading ammo", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes() { Order = 6, ShowRangeAsPercent = true }));
            ReachableOnly = Config.Bind("General", "Reachable Places Only", true, new ConfigDescription("Allow loading ammo outside the inventory only when Magazine and Ammo is in your Vest, Pockets, or Secure Container", null, new ConfigurationManagerAttributes() { Order = 5 }));
            InventoryTabs = Config.Bind("General", "Inventory Tabs", true, new ConfigDescription("Do not interrupt loading ammo when switching inventory tabs (maps tab, tasks tab, etc.)", null, new ConfigurationManagerAttributes() { Order = 4 }));
            CancelHotkey = Config.Bind("General", "Cancel Hotkey", new KeyboardShortcut(KeyCode.Mouse0), new ConfigDescription("Key used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 3 }));
            CancelHotkeyAlt = Config.Bind("General", "Cancel Hotkey Alt", new KeyboardShortcut(KeyCode.Mouse1), new ConfigDescription("Key (alternative) used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 2 }));
            LoadAmmoHotkey = Config.Bind("General", "Load Ammo Hotkey", new KeyboardShortcut(KeyCode.K), new ConfigDescription("Key used to load ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 1 }));
            PrioritizeHighestPenetration = Config.Bind("General", "Prioritize Highest Penetration", true, new ConfigDescription("When using Load Ammo Hotkey, choose ammo that has the highest penetration power if Enabled. If Disabled, prioritize the same ammo in the weapon's magazine", null, new ConfigurationManagerAttributes() { Order = 0 }));

            LoadAmmoUI = new();

            new LoadMagazineStartPatch().Enable();
            new UnloadMagazineStartPatch().Enable();
            new InventoryScreenClosePatch().Enable();
            new DestroyPatch().Enable();
            new LocalGameStopPatch().Enable();
            new RegisterPlayerPatch().Enable();
            new TranslateCommandPatch().Enable();
            ScreensPatches.Enable();
        }

        public static bool InRaid => Singleton<AbstractGame>.Instance is AbstractGame abstractGame && abstractGame.InRaid;
    }
}
