using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace ContinuousLoadAmmo.Patches
{
    internal class StartLoadingPatch : ModulePatch
    {
        private static FieldInfo itemViewLoadAmmoComponentField;
        internal static ItemViewLoadAmmoComponent itemViewLoadAmmoComponent;
        internal static Transform ammoValueTransform;
        internal static Transform imageTransform;

        protected override MethodBase GetTargetMethod()
        {
            itemViewLoadAmmoComponentField = AccessTools.Field(typeof(ItemViewAnimation), "itemViewLoadAmmoComponent_0");
            return typeof(ItemViewAnimation).GetMethod(nameof(ItemViewAnimation.StartLoading));
        }

        // UI
        [PatchPostfix]
        protected static void Postfix(ItemViewAnimation __instance)
        {
            if (StartPatch.IsLoadingAmmo)
            {
                itemViewLoadAmmoComponent = (ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentField.GetValue(__instance);

                GameObject instanceGameObject = __instance.gameObject;
                if (ContinuousLoadAmmo.loadAmmoTextUI.Value)
                {
                    ammoValueTransform = instanceGameObject.transform.Find("Info Panel/BottomLayoutGroup/Value");
                }
                if (ContinuousLoadAmmo.loadMagazineImageUI.Value)
                {
                    imageTransform = instanceGameObject.transform.Find("Image");
                }
            }
        }
    }
}
