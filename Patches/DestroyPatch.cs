using ContinuousLoadAmmo.Controllers;
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
        protected static bool Prefix(ItemViewLoadAmmoComponent __instance)
        {
            if (LoadAmmo.IsLoadingAmmo && __instance.GetHashCode() == LoadAmmoUI.itemViewLoadAmmoComponent.GetHashCode())
            {
                return false;
            }
            return true;
        }
    }
}
