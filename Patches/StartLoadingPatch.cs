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

        // UI
        [PatchPostfix]
        protected static void Postfix(ItemViewAnimation __instance)
        {
            LoadAmmoUI.itemViewLoadAmmoComponent = (ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentField.GetValue(__instance);

            Transform instanceTransform = __instance.transform;
            if (Plugin.LoadAmmoTextUI.Value)
            {
                LoadAmmoUI._ammoValueTransform = instanceTransform.Find("Info Panel/BottomLayoutGroup/Value") ?? instanceTransform.Find("Info Panel/RightLayout/BottomVerticalGroup/Value");
            }
            if (Plugin.LoadMagazineImageUI.Value)
            {
                LoadAmmoUI._imageTransform = instanceTransform.Find("Image") ?? instanceTransform.Find("Item Image");
            }
        }
    }
}
