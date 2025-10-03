using Comfort.Common;
using ContinuousLoadAmmo.Components;
using ContinuousLoadAmmo.Controllers;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace ContinuousLoadAmmo.Patches
{
    internal class LoadMagazineStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController.Class1085).GetMethod(nameof(Player.PlayerInventoryController.Class1085.Start));
        }

        [PatchPrefix]
        protected static void Prefix(Player.PlayerInventoryController.Class1085 __instance)
        {
            InventoryController inventoryController = LoadAmmoComponent.MainPlayer.InventoryController;
            LoadAmmo.IsLoadingAmmo = true;
            LoadAmmo.Magazine = __instance.magazineItemClass;
            LoadAmmo.IsReachable = LoadAmmo.IsAtReachablePlace(inventoryController, LoadAmmo.Magazine) && LoadAmmo.IsAtReachablePlace(inventoryController, __instance.ammoItemClass);
            var loadAmmoEvent = new GEventArgs7(__instance.ammoItemClass, __instance.magazineItemClass, __instance.int_0, __instance.float_0, CommandStatus.Begin, __instance.inventoryController_0);
            LoadAmmoUI.CreateUI(inventoryController, LoadAmmo.LoadingEventType.Load, loadAmmoEvent, null);
        }

        [PatchPostfix]
        protected static async void Postfix(Task<IResult> __result)
        {
            await __result;

            LoadAmmo.Reset();
            LoadAmmo.SetPlayerState(false);
            LoadAmmoUI.DestroyUI();
        }
    }
}
