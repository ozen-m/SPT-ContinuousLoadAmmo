using Comfort.Common;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ContinuousLoadAmmo.Controllers
{
    internal static class LoadAmmoUI
    {
        internal static ItemViewLoadAmmoComponent itemViewLoadAmmoComponent;
        private static Transform _eftBattleUIScreenTransform;
        internal static Transform _ammoValueTransform;
        internal static Transform _imageTransform;
        private static Transform clonedAmmoValueTransform;
        private static Transform clonedMagImageTransform;

        private static Transform EFTBattleUIScreenTransform
        {
            get
            {
                if (_eftBattleUIScreenTransform == null)
                {
                    _eftBattleUIScreenTransform = Singleton<CommonUI>.Instance.EftBattleUIScreen.transform;
                }
                return _eftBattleUIScreenTransform;
            }

        }

        public static void ShowLoadAmmoUI()
        {
            try
            {
                if (Plugin.loadAmmoSpinnerUI.Value)
                {
                    if (itemViewLoadAmmoComponent != null)
                    {
                        itemViewLoadAmmoComponent.gameObject.SetActive(true);
                        itemViewLoadAmmoComponent.SetStopButtonStatus(false);

                        Transform transform = itemViewLoadAmmoComponent.transform;
                        transform.SetParent(EFTBattleUIScreenTransform, false);
                        SetUI(transform, new Vector2(0f, -150f), new Vector3(1.5f, 1.5f, 1.5f));
                    }
                    else
                    {
                        Plugin.LogSource.LogDebug($"itemViewLoadAmmoComponent is null");
                    }
                }

                if (Plugin.loadAmmoTextUI.Value)
                {
                    if (_ammoValueTransform != null)
                    {
                        clonedAmmoValueTransform = Object.Instantiate(_ammoValueTransform, EFTBattleUIScreenTransform);
                        SetUI(clonedAmmoValueTransform, new Vector2(0f, -190f));
                        if (clonedAmmoValueTransform.TryGetComponent(out TextMeshProUGUI textMesh))
                        {
                            UpdateTextValue(textMesh);
                        }
                    }
                    else
                    {
                        Plugin.LogSource.LogDebug($"_ammoValueTransform is null");
                    }
                }

                if (Plugin.loadMagazineImageUI.Value)
                {
                    if (_imageTransform != null)
                    {
                        clonedMagImageTransform = Object.Instantiate(_imageTransform, EFTBattleUIScreenTransform);
                        SetUI(clonedMagImageTransform, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
                    }
                    else
                    {
                        Plugin.LogSource.LogDebug($"_imageTransform is null");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"Exception: {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
            }
        }

        private static void SetUI(Transform transform, Vector2? offset = null, Vector3? localScale = null)
        {
            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.anchoredPosition = offset != null ? (Vector2)offset : Vector2.zero;
            rectTransform.localScale = localScale != null ? (Vector3)localScale : Vector3.one;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static async void UpdateTextValue(TextMeshProUGUI textMesh)
        {
            textMesh.enableWordWrapping = false;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.alignment = TextAlignmentOptions.Center;

            int skill = Mathf.Max(
                [
                    LoadAmmo.MainPlayer.Profile.MagDrillsMastering,
                    LoadAmmo.MainPlayer.Profile.CheckedMagazineSkillLevel(LoadAmmo.Magazine.Id),
                    LoadAmmo.Magazine.CheckOverride
                        ]);
            //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine)

            while (LoadAmmo.IsLoadingAmmo)
            {
                string value = LoadAmmo.Magazine.GetAmmoCountByLevel(LoadAmmo.Magazine.Count, LoadAmmo.Magazine.MaxCount, skill, "#ffffff", true, false, "<color={2}>{0}</color>/{1}");
                textMesh.SetText(value);

                await Task.Yield();
            }
        }

        public static void DestroyUI()
        {
            if (itemViewLoadAmmoComponent != null)
            {
                itemViewLoadAmmoComponent.Destroy();
            }
            if (clonedAmmoValueTransform != null)
            {
                Object.Destroy(clonedAmmoValueTransform.gameObject);
            }
            if (clonedMagImageTransform != null)
            {
                Object.Destroy(clonedMagImageTransform.gameObject);
            }
        }
    }
}
