using ContinuousLoadAmmo.Controllers;
using EFT.UI;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    internal class DestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemViewLoadAmmoComponent).GetMethod(nameof(ItemViewLoadAmmoComponent.Destroy));
        }

        [PatchPrefix]
        protected static bool Prefix(InventoryScreen __instance)
        {
            if (LoadAmmo.IsLoadingAmmo && LoadAmmo.IsReachable)
            {
                return false;
            }
            return true;
        }
    }
}
