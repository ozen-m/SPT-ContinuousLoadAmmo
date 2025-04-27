using EFT.UI.DragAndDrop;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ContinuousLoadAmmo.Controllers
{
    internal static class LoadAmmoUI
    {
        internal static ItemViewLoadAmmoComponent itemViewLoadAmmoComponent;
        private static Canvas EFTBattleUIScreenCanvas;
        internal static Transform ammoValueTransform;
        internal static Transform imageTransform;
        private static GameObject clonedAmmoValueGameObject;
        private static GameObject clonedMagImageGameObject;
        private static TextMeshProUGUI textMesh_0;

        public static async void ShowLoadAmmoUI()
        {
            if (EFTBattleUIScreenCanvas == null)
            {
                if (!await TryGetCanvas())
                {
                    return;
                }
            }

            if (Plugin.loadAmmoSpinnerUI.Value)
            {
                itemViewLoadAmmoComponent.SetStopButtonStatus(false);
                itemViewLoadAmmoComponent.gameObject.transform.SetParent(EFTBattleUIScreenCanvas.transform, false);
                SetUI(itemViewLoadAmmoComponent.gameObject, new Vector2(0f, -150f), new Vector3(1.5f, 1.5f, 1.5f));
            }

            if (Plugin.loadAmmoTextUI.Value)
            {
                clonedAmmoValueGameObject = Object.Instantiate(ammoValueTransform.gameObject, EFTBattleUIScreenCanvas.transform);
                clonedAmmoValueGameObject.SetActive(true);
                SetUI(clonedAmmoValueGameObject, new Vector2(0f, -190f), null);
            }

            if (Plugin.loadMagazineImageUI.Value)
            {
                clonedMagImageGameObject = Object.Instantiate(imageTransform.gameObject, EFTBattleUIScreenCanvas.transform);
                clonedMagImageGameObject.SetActive(true);
                SetUI(clonedMagImageGameObject, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
            }

            while (LoadAmmo.IsLoadingAmmo)
            {
                UpdateTextValue();
                await Task.Yield();
            }
        }

        private static void SetUI(GameObject gameObject, Vector2? offset, Vector3? localScale)
        {
            RectTransform componentRect = gameObject.RectTransform();
            componentRect.localScale = localScale != null ? (Vector3)localScale : new Vector3(1f, 1f, 1f);
            componentRect.anchorMin = new Vector2(0.5f, 0.5f);
            componentRect.anchorMax = new Vector2(0.5f, 0.5f);
            componentRect.pivot = new Vector2(0.5f, 0.5f);
            componentRect.anchoredPosition = offset != null ? (Vector2)offset : new Vector2(0f, 0f);

            if (!gameObject.TryGetComponent(out TextMeshProUGUI textMesh))
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
                    LoadAmmo.MainPlayer.Profile.MagDrillsMastering,
                    LoadAmmo.MainPlayer.Profile.CheckedMagazineSkillLevel(LoadAmmo.Magazine.Id),
                    LoadAmmo.Magazine.CheckOverride
                        ]);
            //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine)
            string value = LoadAmmo.Magazine.GetAmmoCountByLevel(LoadAmmo.Magazine.Count, LoadAmmo.Magazine.MaxCount, skill, "#ffffff", true, false, "<color={2}>{0}</color>/{1}");

            textMesh_0.SetText(value);
        }

        public static void DestroyUI()
        {
            textMesh_0 = null;
            if (itemViewLoadAmmoComponent != null)
            {
                itemViewLoadAmmoComponent.Destroy();
            }
            Object.Destroy(clonedAmmoValueGameObject);
            Object.Destroy(clonedMagImageGameObject);
        }

        private static async Task<bool> TryGetCanvas()
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
                Plugin.LogSource.LogError("InventoryScreenClosePatch::TryGetCanvas EFTBattleUIScreen game object not found within timeout!");
                return false;
            }
            if (!eftBattleUIScreenGameObject.TryGetComponent(out EFTBattleUIScreenCanvas))
            {
                Plugin.LogSource.LogError("InventoryScreenClosePatch::TryGetCanvas EFTBattleUIScreen canvas not found!");
                return false;
            }
            return true;
        }
    }
}
