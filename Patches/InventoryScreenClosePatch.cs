using ContinuousLoadAmmo.Controllers;
using EFT;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading;

namespace ContinuousLoadAmmo.Patches
{
    internal class InventoryScreenClosePatch : ModulePatch
    {
        private static bool IsBusy = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen).GetMethod(nameof(InventoryScreen.Close));
        }

        // UI, Patch to NOT stop loading ammo on close
        [PatchPrefix]
        protected static void Prefix(ref Player.PlayerInventoryController ___inventoryController_0, InventoryScreen.GClass3581 ___ScreenController)
        {
            if (LoadAmmo.IsLoadingAmmo && LoadAmmo.IsReachable)
            {
                if (LoadAmmo.MainPlayer == null)
                {
                    Plugin.LogSource.LogError("InventoryScreenClosePatch::Prefix MainPlayer not found!");
                    return;
                }
                IsBusy = LoadAmmo.MainPlayer.InventoryController.HasAnyHandsAction();
                if (IsBusy) return;

                LoadAmmo.IsOutsideInventory = true;

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

                if (LoadAmmo.cancellationTokenSource != null)
                    LoadAmmo.cancellationTokenSource.Cancel();
                LoadAmmo.cancellationTokenSource = new CancellationTokenSource();
                LoadAmmo.ListenForCancel(LoadAmmo.MainPlayer.InventoryController, LoadAmmo.cancellationTokenSource.Token);
            }
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            if (LoadAmmo.IsLoadingAmmo && LoadAmmo.IsReachable && !IsBusy)
            {
                LoadAmmo.SetPlayerState(true);
                LoadAmmoUI.ShowLoadAmmoUI(LoadAmmo.cancellationTokenSource.Token);
            }
        }
    }
}
