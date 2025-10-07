using ContinuousLoadAmmo.Components;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    public class InventoryScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen).GetMethod(nameof(InventoryScreen.Close));
        }

        /// <summary>
        /// UI, Patch to NOT stop loading ammo on close
        /// </summary>
        [PatchPrefix]
        protected static void Prefix(ref InventoryController ___inventoryController_0, InventoryScreen.GClass3581 ___ScreenController)
        {
            if (!Plugin.InRaid) return;

            if (___inventoryController_0 is Player.PlayerInventoryController playerInventoryController)
            {
                // It looks like only Load/UnloadMagazine checks for process locked, this should be fine
                playerInventoryController.SetNextProcessLocked(false);
            }
            if (___inventoryController_0 != null)
            {
                // Skip StopProcesses and SetNextProcessLocked(true) after prefix
                ___inventoryController_0 = null;
            }
            LoadAmmo.Inst.LoadingOutsideInventory();
        }
    }
}
