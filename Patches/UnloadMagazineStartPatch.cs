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
    internal class UnloadMagazineStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController.Class1088).GetMethod(nameof(Player.PlayerInventoryController.Class1088.Start));
        }

        [PatchPrefix]
        protected static void Prefix(Player.PlayerInventoryController.Class1088 __instance)
        {
            InventoryController inventoryController = LoadAmmoComponent.MainPlayer.InventoryController;
            LoadAmmo.IsLoadingAmmo = true;
            LoadAmmo.Magazine = __instance.magazineItemClass;
            LoadAmmo.IsReachable = LoadAmmo.IsAtReachablePlace(inventoryController, LoadAmmo.Magazine);
            var unloadAmmoEvent = new GEventArgs8(__instance.item_0, __instance.item_1, __instance.magazineItemClass, __instance.int_0 - __instance.int_1, __instance.int_1, __instance.float_0, EFT.InventoryLogic.CommandStatus.Begin, __instance.inventoryController_0);
            LoadAmmoUI.CreateUI(inventoryController, LoadAmmo.LoadingEventType.Unload, null, unloadAmmoEvent);
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
