using System.Reflection;
using ContinuousLoadAmmo.Components;
using ContinuousLoadAmmo.Controllers;
using ContinuousLoadAmmo.Utils;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

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
    protected static void Prefix(ref InventoryController ___inventoryController_0)
    {
        if (!CommonUtils.InRaid) return;

        if (___inventoryController_0 is Player.PlayerInventoryController playerInventoryController)
        {
            // It looks like only Load/UnloadMagazine checks for process locked, this should be fine
            playerInventoryController.SetNextProcessLocked(false);
        }

        // Skip StopProcesses and SetNextProcessLocked(true) after prefix
        ___inventoryController_0 = null;

        LoadAmmoController.Inst.LoadingOutsideInventory();
    }
}
