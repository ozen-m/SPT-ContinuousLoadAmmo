using ContinuousLoadAmmo.Controllers;
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

        protected override MethodBase GetTargetMethod()
        {
            itemViewLoadAmmoComponentField = AccessTools.Field(typeof(ItemViewAnimation), "itemViewLoadAmmoComponent_0");
            return typeof(ItemViewAnimation).GetMethod(nameof(ItemViewAnimation.StartLoading));
        }

        [PatchPostfix]
        protected static void Postfix(ItemViewAnimation __instance)
        {
            if (LoadAmmo.IsLoadingAmmo)
            {
                LoadAmmoUI.itemViewLoadAmmoComponent = (ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentField.GetValue(__instance);

                GameObject instanceGameObject = __instance.gameObject;
                if (Plugin.loadAmmoTextUI.Value)
                {
                    LoadAmmoUI.ammoValueTransform = instanceGameObject.transform.Find("Info Panel/BottomLayoutGroup/Value");
                }
                if (Plugin.loadMagazineImageUI.Value)
                {
                    LoadAmmoUI.imageTransform = instanceGameObject.transform.Find("Image");
                }
            }
        }
    }
}
