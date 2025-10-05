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
            if (LoadAmmo.Inst.LoadingClosedInventory())
            {
                if (___inventoryController_0 is Player.PlayerInventoryController playerInventoryController)
                {
                    playerInventoryController.SetNextProcessLocked(true);
                }
                if (___ScreenController is InventoryScreen.GClass3583 || ___ScreenController is InventoryScreen.GClass3586)
                {
                    CameraClass.Instance.Blur(false, 0.5f);
                }
                if (___inventoryController_0 != null)
                {
                    // Skip stop process after prefix
                    ___inventoryController_0 = null;
                }
            }
        }
    }
}
