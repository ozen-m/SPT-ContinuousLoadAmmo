using ContinuousLoadAmmo.Components;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    public class InventoryCheckMagazinePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController).GetMethod(nameof(Player.PlayerInventoryController.InventoryCheckMagazine));
        }

        /// <summary>
        /// Fixes: Examining another magazine while loading ammo breaks UI
        /// </summary>
        [PatchPrefix]
        protected static void Prefix()
        {
            if (!Plugin.InRaid) return;

            LoadAmmo.Inst.LoadingEnd();
        }
    }
}
