using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    public class DestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemViewLoadAmmoComponent).GetMethod(nameof(ItemViewLoadAmmoComponent.Destroy));
        }

        /// <summary>
        /// Keeps from destroying the loader UI outside the inventory
        /// </summary>
        [PatchPrefix]
        protected static bool Prefix(ItemViewLoadAmmoComponent __instance)
        {
            if (Plugin.LoadAmmoUI.IsSameLoaderUI(__instance))
            {
                return false;
            }
            return true;
        }
    }
}
