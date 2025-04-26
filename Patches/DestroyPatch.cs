using EFT;
using EFT.InventoryLogic;
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
        internal static bool Prefix(InventoryScreen __instance)
        {
            if (StartPatch.IsLoadingAmmo)
            {
                if (StartPatch.IsReachable)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
