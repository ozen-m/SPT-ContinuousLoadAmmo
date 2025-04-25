using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    internal class StartLoadingPatch : ModulePatch
    {
        private static FieldInfo itemViewLoadAmmoComponentField;
        internal static ItemViewLoadAmmoComponent itemViewLoadAmmoComponent_0;

        protected override MethodBase GetTargetMethod()
        {
            itemViewLoadAmmoComponentField = AccessTools.Field(typeof(ItemViewAnimation), "itemViewLoadAmmoComponent_0");
            return typeof(ItemViewAnimation).GetMethod(nameof(ItemViewAnimation.StartLoading));
        }

        [PatchPostfix]
        internal static void Postfix(ItemViewAnimation __instance, float oneAmmoDuration, int ammoTotal, int startCount)
        {
            if (StartPatch.IsLoadingAmmo)
            {
                itemViewLoadAmmoComponent_0 = (ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentField.GetValue(__instance);
            }
        }
    }
}
