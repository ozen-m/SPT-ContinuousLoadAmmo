using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ContinuousLoadAmmo.Patches;
using UnityEngine;

namespace ContinuousLoadAmmo
{
    [BepInPlugin("com.ozen.ContinuousLoadAmmo", "ContinuousLoadAmmo", "1.0.4")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static ConfigEntry<float> SpeedLimit;
        public static ConfigEntry<bool> ReachableOnly;
        public static ConfigEntry<KeyboardShortcut> CancelHotkey;
        public static ConfigEntry<KeyboardShortcut> CancelHotkeyAlt;
        public static ConfigEntry<bool> loadAmmoSpinnerUI;
        public static ConfigEntry<bool> loadAmmoTextUI;
        public static ConfigEntry<bool> loadMagazineImageUI;

        private void Awake()
        {
            LogSource = Logger;
            SpeedLimit = Config.Bind("General", "Speed Limit", 0.31f, new ConfigDescription("The speed limit, as a percentage of the walk speed, set to the player while loading ammo", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes() { Order = 6, ShowRangeAsPercent = true }));
            ReachableOnly = Config.Bind("General", "Reachable Places Only", true, new ConfigDescription("Allow loading ammo outside the inventory only when Magazine and Ammo is in your Vest or Pockets", null, new ConfigurationManagerAttributes() { Order = 5 }));
            CancelHotkey = Config.Bind("General", "Cancel Hotkey", new KeyboardShortcut(KeyCode.Mouse0), new ConfigDescription("Key used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 4 }));
            CancelHotkeyAlt = Config.Bind("General", "Cancel Hotkey Alt", new KeyboardShortcut(KeyCode.Mouse1), new ConfigDescription("Key (alternative) used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 3 }));
            loadAmmoSpinnerUI = Config.Bind("UI", "Show Spinner", true, new ConfigDescription("Show the spinner UI outside the inventory", null, new ConfigurationManagerAttributes() { Order = 2 }));
            loadAmmoTextUI = Config.Bind("UI", "Show Text", true, new ConfigDescription("Show magazine count and capacity UI outside the inventory", null, new ConfigurationManagerAttributes() { Order = 1 }));
            loadMagazineImageUI = Config.Bind("UI", "Show Magazine", true, new ConfigDescription("Show the magazine being loaded outside the inventory", null, new ConfigurationManagerAttributes() { Order = 0 }));

            new StartPatch().Enable();
            new InventoryScreenClosePatch().Enable();
            new StartLoadingPatch().Enable();
            new DestroyPatch().Enable();
            new InventoryCheckMagazinePatch().Enable();
        }
    }
}
