using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ContinuousLoadAmmo.Patches
{
    internal class InventoryScreenClosePatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static Player player;
        private static GameObject clonedAmmoValueGameObject;
        private static GameObject clonedMagImageGameObject;
        private static TextMeshProUGUI textMesh_0;
        private static bool IsBusy = false;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(Player.PlayerInventoryController), "player_0");
            return typeof(InventoryScreen).GetMethod(nameof(InventoryScreen.Close));
        }

        // Patch to NOT stop loading ammo on close
        [PatchPrefix]
        protected static void Prefix(InventoryScreen __instance, ref Player.PlayerInventoryController ___inventoryController_0, InventoryScreen.GClass3581 ___ScreenController)
        {
            if (StartPatch.IsLoadingAmmo && StartPatch.IsReachable)
            {
                if (player == null)
                {
                    if ((player = (Player)playerField.GetValue(___inventoryController_0)) == null)
                    {
                        ContinuousLoadAmmo.LogSource.LogError($"InventoryScreenClosePatch::Prefix Player could not be found!");
                        return;
                    }
                }
                IsBusy = player.InventoryController.HasAnyHandsAction();
                if (IsBusy) return;

                if (___inventoryController_0 is Player.PlayerInventoryController playerInventoryController)
                {
                    playerInventoryController.SetNextProcessLocked(true);
                }
                if (___ScreenController is InventoryScreen.GClass3583 || ___ScreenController is InventoryScreen.GClass3586)
                {
                    CameraClass.Instance.Blur(false, 0.5f);
                }
                if (___inventoryController_0 != null)
                {
                    // Skip stop process after prefix
                    ___inventoryController_0 = null;
                }
            }
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            if (StartPatch.IsLoadingAmmo && StartPatch.IsReachable && !IsBusy)
            {
                StartPatch.SetPlayerState(true);
                ShowLoadAmmoUI();
            }
        }

        private static async void ShowLoadAmmoUI()
        {
            int elapsedTime = 0;
            int timeInterval = 100;
            GameObject eftBattleUIScreenGameObject = null;
            while (elapsedTime < 1500)
            {
                eftBattleUIScreenGameObject = GameObject.Find("EFTBattleUIScreen Variant");
                if (eftBattleUIScreenGameObject != null) break;
                await Task.Delay(timeInterval);
                elapsedTime += timeInterval;
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

            if (ContinuousLoadAmmo.loadAmmoSpinnerUI.Value)
            {
                SetUI(StartLoadingPatch.itemViewLoadAmmoComponent.gameObject, canvas, new Vector2(0f, -150f), new Vector3(1.5f, 1.5f, 1.5f));
            }

            if (ContinuousLoadAmmo.loadAmmoTextUI.Value)
            {
                clonedAmmoValueGameObject = GameObject.Instantiate(StartLoadingPatch.ammoValueTransform.gameObject, canvas.transform);
                clonedAmmoValueGameObject.SetActive(true);
                SetUI(clonedAmmoValueGameObject, canvas, new Vector2(0f, -190f), null);
            }

            if (ContinuousLoadAmmo.loadMagazineImageUI.Value)
            {
                clonedMagImageGameObject = GameObject.Instantiate(StartLoadingPatch.imageTransform.gameObject, canvas.transform);
                clonedMagImageGameObject.SetActive(true);
                SetUI(clonedMagImageGameObject, canvas, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
            }

            while (StartPatch.IsLoadingAmmo)
            {
                UpdateTextValue();
                await Task.Yield();
            }
        }

        private static void SetUI(GameObject gameObject, Canvas canvas, Vector2? offset, Vector3? localScale)
        {
            gameObject.transform.SetParent(canvas.transform, false);

            RectTransform componentRect = gameObject.RectTransform();
            componentRect.localScale = localScale != null ? (Vector3)localScale : new Vector3(1f, 1f, 1f);
            componentRect.anchorMin = new Vector2(0.5f, 0.5f);
            componentRect.anchorMax = new Vector2(0.5f, 0.5f);
            componentRect.pivot = new Vector2(0.5f, 0.5f);
            componentRect.anchoredPosition = offset != null ? (Vector2)offset : new Vector2(0f, 0f);

            if (!gameObject.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI textMesh))
            {
                return;
            }
            textMesh.enableWordWrapping = false;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh_0 = textMesh;
        }

        private static void UpdateTextValue()
        {
            if (textMesh_0 == null) return;
            int skill = Mathf.Max(
                        [
                            player.Profile.MagDrillsMastering,
                            player.Profile.CheckedMagazineSkillLevel(StartPatch.Magazine.Id),
                            StartPatch.Magazine.CheckOverride
                        ]);
            //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine)
            string value = StartPatch.Magazine.GetAmmoCountByLevel(StartPatch.Magazine.Count, StartPatch.Magazine.MaxCount, skill, "#ffffff", true, false, "<color={2}>{0}</color>/{1}");

            textMesh_0.SetText(value);
        }

        public static void DestroyUI()
        {
            textMesh_0 = null;
            if (StartLoadingPatch.itemViewLoadAmmoComponent != null)
            {
                StartLoadingPatch.itemViewLoadAmmoComponent.Destroy();
            }
            Object.Destroy(clonedAmmoValueGameObject);
            Object.Destroy(clonedMagImageGameObject);
        }
    }
}
