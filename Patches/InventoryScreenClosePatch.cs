using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ContinuousLoadAmmo.Patches
{
    internal class InventoryScreenClosePatch : ModulePatch
    {
        private static FieldInfo inventoryControllerField;
        private static FieldInfo screenControllerField;
        protected override MethodBase GetTargetMethod()
        {
            inventoryControllerField = AccessTools.Field(typeof(InventoryScreen), "inventoryController_0");
            screenControllerField = AccessTools.Field(typeof(InventoryScreen), "ScreenController");
            return typeof(InventoryScreen).GetMethod(nameof(InventoryScreen.Close));
        }

        [PatchPrefix]
        internal static void Prefix(InventoryScreen __instance)
        {
            if (StartPatch.IsLoadingAmmo)
            {
                Player.PlayerInventoryController playerInventoryController = (Player.PlayerInventoryController)inventoryControllerField.GetValue(__instance) as Player.PlayerInventoryController;
                if (playerInventoryController != null)
                {
                    playerInventoryController.SetNextProcessLocked(true);
                }
                if (screenControllerField.GetValue(__instance) is InventoryScreen.GClass3583 || screenControllerField.GetValue(__instance) is InventoryScreen.GClass3586)
                {
                    CameraClass.Instance.Blur(false, 0.5f);
                }
                InventoryController inventoryController = (InventoryController)inventoryControllerField.GetValue(__instance);
                if (inventoryController != null)
                {
                    inventoryControllerField.SetValue(__instance, null);
                }
            }
        }

        [PatchPostfix]
        internal static void Postfix()
        {
            if (StartPatch.IsLoadingAmmo)
            {
                StartPatch.SetLoadingAmmoAnim(true);
                LoadAmmoUIMover();
            }
        }

        public static async void LoadAmmoUIMover()
        {
            int elapsed = 0;
            int increment = 100;
            GameObject eftBattleUIScreenGameObject = null;
            while (eftBattleUIScreenGameObject == null && elapsed < 1000)
            {
                await Task.Delay(increment);
                eftBattleUIScreenGameObject = GameObject.Find("EFTBattleUIScreen Variant");
                elapsed += increment;
            }
            if (eftBattleUIScreenGameObject == null)
            {
                ContinuousLoadAmmo.LogSource.LogError("EFTBattleUIScreen game object not found within timeout!");
                return;
            }
            if (!eftBattleUIScreenGameObject.TryGetComponent(out Canvas canvas))
            {
                ContinuousLoadAmmo.LogSource.LogError("EFTBattleUIScreen canvas not found!");
                return;
            }
            StartLoadingPatch.itemViewLoadAmmoComponent_0.transform.SetParent(canvas.transform, false);

            RectTransform componentRect = StartLoadingPatch.itemViewLoadAmmoComponent_0.RectTransform();
            componentRect.anchorMin = new Vector2(0.5f, 0.5f);
            componentRect.anchorMax = new Vector2(0.5f, 0.5f);
            componentRect.pivot = new Vector2(0.5f, 0.5f);
            componentRect.anchoredPosition = new Vector2(0f, -100f);
        }
    }
}
