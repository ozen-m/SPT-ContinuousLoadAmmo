using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ContinuousLoadAmmo.Patches;

namespace ContinuousLoadAmmo
{
    [BepInPlugin("com.ozen.ContinuousLoadAmmo", "ContinuousLoadAmmo", "1.0.0")]
    public class ContinuousLoadAmmo : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static ConfigEntry<float> SpeedLimit;

        private void Awake()
        {
            LogSource = Logger;
            SpeedLimit = Config.Bind("", "Speed Limit", 0.31f, new ConfigDescription("The speed limit, as a percentage of the walk speed, set to the player while loading ammo", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes() { ShowRangeAsPercent = true }));

            new StartPatch().Enable();
            new InventoryScreenClosePatch().Enable();

            LogSource.LogInfo("ContinuousLoadAmmo plugin loaded!");
        }
    }
}
