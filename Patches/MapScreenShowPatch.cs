using EFT.InventoryLogic;
using EFT.UI.Map;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    public class MapScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MapScreen).GetMethod(nameof(MapScreen.Show));
        }

        /// <summary>
        /// DynamicMaps compatibility
        /// </summary>
        [PatchPostfix]
        protected static void Postfix(InventoryController inventoryController)
        {
            inventoryController.StopProcesses();
        }
    }
}
