using Comfort.Common;
using ContinuousLoadAmmo.Controllers;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace ContinuousLoadAmmo.Patches
{
    internal class StartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController.Class1085).GetMethod(nameof(Player.PlayerInventoryController.Class1085.Start));
        }

        [PatchPrefix]
        protected static void Prefix(Player.PlayerInventoryController.Class1085 __instance)
        {
            LoadAmmo.IsLoadingAmmo = true;
            LoadAmmo.Magazine = __instance.magazineItemClass;
            LoadAmmo.IsReachable = LoadAmmo.IsAtReachablePlace(LoadAmmo.MainPlayer.InventoryController, LoadAmmo.Magazine) && LoadAmmo.IsAtReachablePlace(LoadAmmo.MainPlayer.InventoryController, __instance.ammoItemClass);

            LoadAmmo.ListenForCancel(LoadAmmo.MainPlayer.InventoryController);
        }

        [PatchPostfix]
        protected static async void Postfix(Task<IResult> __result)
        {
            await __result;

            LoadAmmo.IsLoadingAmmo = false;
            LoadAmmo.IsReachable = false;

            LoadAmmoUI.DestroyUI();
            LoadAmmo.SetPlayerState(false);
        }
    }
}
