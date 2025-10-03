using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ContinuousLoadAmmo.Patches;
using UnityEngine;

namespace ContinuousLoadAmmo
{
    [BepInPlugin("com.ozen.continuousloadammo", "ContinuousLoadAmmo", "1.0.8")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static ConfigEntry<float> SpeedLimit;
        public static ConfigEntry<bool> ReachableOnly;
        public static ConfigEntry<bool> WeaponTopLoad;
        public static ConfigEntry<KeyboardShortcut> CancelHotkey;
        public static ConfigEntry<KeyboardShortcut> CancelHotkeyAlt;
        public static ConfigEntry<KeyboardShortcut> LoadAmmoHotkey;
        public static ConfigEntry<bool> LoadAmmoSpinnerUI;
        public static ConfigEntry<bool> LoadAmmoTextUI;
        public static ConfigEntry<bool> LoadMagazineImageUI;

        private void Awake()
        {
            LogSource = Logger;
            SpeedLimit = Config.Bind("General", "Speed Limit", 0.31f, new ConfigDescription("The speed limit, as a percentage of the walk speed, set to the player while loading ammo", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes() { Order = 8, ShowRangeAsPercent = true }));
            ReachableOnly = Config.Bind("General", "Reachable Places Only", true, new ConfigDescription("Allow loading ammo outside the inventory only when Magazine and Ammo is in your Vest or Pockets", null, new ConfigurationManagerAttributes() { Order = 7 }));
            WeaponTopLoad = Config.Bind("General", "Allow Weapon Top Loading", false, new ConfigDescription("Allow loading weapon ammo in equipped weapon slots outside the inventory", null, new ConfigurationManagerAttributes() { Order = 6 }));
            CancelHotkey = Config.Bind("General", "Cancel Hotkey", new KeyboardShortcut(KeyCode.Mouse0), new ConfigDescription("Key used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 5 }));
            CancelHotkeyAlt = Config.Bind("General", "Cancel Hotkey Alt", new KeyboardShortcut(KeyCode.Mouse1), new ConfigDescription("Key (alternative) used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 4 }));
            LoadAmmoHotkey = Config.Bind("General", "Load Ammo Hotkey", new KeyboardShortcut(KeyCode.L), new ConfigDescription("Key used to load ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 3 }));
            LoadAmmoSpinnerUI = Config.Bind("UI", "Show Spinner", true, new ConfigDescription("Show the spinner UI outside the inventory", null, new ConfigurationManagerAttributes() { Order = 2 }));
            LoadAmmoTextUI = Config.Bind("UI", "Show Text", true, new ConfigDescription("Show magazine count and capacity UI outside the inventory", null, new ConfigurationManagerAttributes() { Order = 1 }));
            LoadMagazineImageUI = Config.Bind("UI", "Show Magazine", true, new ConfigDescription("Show the magazine being loaded outside the inventory", null, new ConfigurationManagerAttributes() { Order = 0 }));

            new LoadMagazineStartPatch().Enable();
            new UnloadMagazineStartPatch().Enable();
            new InventoryScreenClosePatch().Enable();
            new DestroyPatch().Enable();
            new InventoryCheckMagazinePatch().Enable();
            new LocalGameStopPatch().Enable();
            //new MapScreenShowPatch().Enable();
            new RegisterPlayerPatch().Enable();
        }
    }
}
