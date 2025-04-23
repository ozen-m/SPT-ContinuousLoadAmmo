using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    internal class InventoryScreenClosePatch : ModulePatch
    {
        private static FieldInfo inventoryControllerField;
        private static FieldInfo screenControllerField;
        protected override MethodBase GetTargetMethod()
        {
            inventoryControllerField = AccessTools.Field(typeof(InventoryScreen), "inventoryController_0");
            screenControllerField = AccessTools.Field(typeof(InventoryScreen), "ScreenController");
            return typeof(InventoryScreen).GetMethod(nameof(InventoryScreen.Close));
        }

        [PatchPrefix]
        internal static void Prefix(InventoryScreen __instance)
        {
            if (StartPatch.IsLoadingAmmo)
            {
                Player.PlayerInventoryController playerInventoryController = (Player.PlayerInventoryController)inventoryControllerField.GetValue(__instance);
                if (playerInventoryController != null)
                {
                    playerInventoryController.SetNextProcessLocked(true);
                }
                if (screenControllerField.GetValue(__instance) is InventoryScreen.GClass3583 || screenControllerField.GetValue(__instance) is InventoryScreen.GClass3586)
                {
                    CameraClass.Instance.Blur(false, 0.5f);
                }
                InventoryController inventoryController = (InventoryController)inventoryControllerField.GetValue(__instance);
                if (inventoryController != null)
                {
                    inventoryControllerField.SetValue(__instance, null);
                }
            }
        }
    }
}
