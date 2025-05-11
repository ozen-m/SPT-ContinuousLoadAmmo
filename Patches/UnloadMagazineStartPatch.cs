using Comfort.Common;
using ContinuousLoadAmmo.Controllers;
using EFT;
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
            LoadAmmo.IsLoadingAmmo = true;
            LoadAmmo.Magazine = __instance.magazineItemClass;
            LoadAmmo.IsReachable = LoadAmmo.IsAtReachablePlace(LoadAmmo.MainPlayer.InventoryController, LoadAmmo.Magazine);
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
