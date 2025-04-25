using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ContinuousLoadAmmo.Patches;
using UnityEngine;

namespace ContinuousLoadAmmo
{
    [BepInPlugin("com.ozen.ContinuousLoadAmmo", "ContinuousLoadAmmo", "1.0.3")]
    public class ContinuousLoadAmmo : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static ConfigEntry<float> SpeedLimit;
        public static ConfigEntry<KeyboardShortcut> CancelHotkey;
        public static ConfigEntry<KeyboardShortcut> CancelHotkeyAlt;

        private void Awake()
        {
            LogSource = Logger;
            SpeedLimit = Config.Bind("", "Speed Limit", 0.31f, new ConfigDescription("The speed limit, as a percentage of the walk speed, set to the player while loading ammo", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes() { Order = 2, ShowRangeAsPercent = true }));
            CancelHotkey = Config.Bind("", "Cancel Hotkey", new KeyboardShortcut(KeyCode.Mouse0), new ConfigDescription("Key used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 1 }));
            CancelHotkeyAlt = Config.Bind("", "Cancel Hotkey Alt", new KeyboardShortcut(KeyCode.Mouse1), new ConfigDescription("Key (alternative) used to cancel loading ammo outside the inventory", null, new ConfigurationManagerAttributes() { Order = 0 }));

            new StartPatch().Enable();
            new InventoryScreenClosePatch().Enable();
        }
    }
}
