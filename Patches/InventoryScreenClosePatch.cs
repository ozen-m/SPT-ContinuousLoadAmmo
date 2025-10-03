using ContinuousLoadAmmo.Controllers;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    internal class InventoryScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen).GetMethod(nameof(InventoryScreen.Close));
        }

        /// <summary>
        /// UI, Patch to NOT stop loading ammo on close
        /// </summary>
        [PatchPrefix]
        protected static void Prefix(ref InventoryController ___inventoryController_0, InventoryScreen.GClass3581 ___ScreenController, out bool __state)
        {
            // bool IsBusy
            __state = false;
            if (LoadAmmo.IsLoadingAmmo && LoadAmmo.IsReachable)
            {
                __state = ___inventoryController_0.HasAnyHandsAction();
                if (__state)
                {
                    return;
                }

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

                LoadAmmo.ListenForCancel();
                LoadAmmo.SetPlayerState(true);
                LoadAmmoUI.Show();
            }
        }
    }
}
