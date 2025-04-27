using EFT;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    internal class InventoryCheckMagazinePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController).GetMethod(nameof(Player.PlayerInventoryController.InventoryCheckMagazine));
        }

        // Fixes: Examining another magazine while loading ammo leaves behind UI element
        [PatchPrefix]
        internal static void Prefix()
        {
            StartPatch.IsLoadingAmmo = false;
            StartPatch.IsReachable = false;
        }
    }
}
