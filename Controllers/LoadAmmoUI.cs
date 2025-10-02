using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ContinuousLoadAmmo.Controllers
{
    internal static class LoadAmmoUI
    {
        internal static ItemViewLoadAmmoComponent itemViewLoadAmmoComponent;
        internal static Transform _ammoValueTransform;
        internal static Transform _imageTransform;
        private static Transform clonedAmmoValueTransform;
        private static Transform clonedMagImageTransform;


        private static FieldInfo _itemViewLoadAmmoComponentField;
        private static FieldInfo itemViewLoadAmmoComponentField
        {
            get
            {
                if (_itemViewLoadAmmoComponentField == null)
                {
                    _itemViewLoadAmmoComponentField = AccessTools.Field(typeof(ItemViewAnimation), "itemViewLoadAmmoComponent_0");
                }
                return _itemViewLoadAmmoComponentField;
            }
        }
        private static FieldInfo _itemViewAnimationField;
        private static FieldInfo itemViewAnimationField
        {
            get
            {
                if (_itemViewAnimationField == null)
                {
                    _itemViewAnimationField = AccessTools.Field(typeof(ItemView), "Animator");
                }
                return _itemViewAnimationField;
            }
        }

        public static Transform EFTBattleUIScreenTransform
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
        private static Transform _eftBattleUIScreenTransform;

        public static void CreateUI(InventoryController playerInventoryController, GEventArgs7 activeEvent)
        {
            MagazineItemClass magazine = activeEvent.TargetItem as MagazineItemClass;
            GridItemView magItemView = GridItemView.Create(magazine, new GClass3243(magazine, EItemViewType.Inventory), ItemRotation.Horizontal, playerInventoryController, playerInventoryController, null, null, null, null, null);
            Transform instanceTransform = magItemView.transform;
            if (Plugin.LoadAmmoTextUI.Value)
            {
                _ammoValueTransform = instanceTransform.Find("Info Panel/BottomLayoutGroup/Value") ?? instanceTransform.Find("Info Panel/RightLayout/BottomVerticalGroup/Value");
            }
            if (Plugin.LoadMagazineImageUI.Value)
            {
                _imageTransform = instanceTransform.Find("Image") ?? instanceTransform.Find("Item Image");
            }

            magItemView.SetLoadMagazineStatus(activeEvent);
            var itemViewAnimation = (ItemViewAnimation)itemViewAnimationField.GetValue(magItemView);
            itemViewLoadAmmoComponent = (ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentField.GetValue(itemViewAnimation);
            magItemView.ReturnToPool();
        }

        public static void Show()
        {
            try
            {
                if (Plugin.LoadAmmoSpinnerUI.Value)
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
                        Plugin.LogSource.LogDebug("itemViewLoadAmmoComponent is null");
                    }
                }

                if (Plugin.LoadAmmoTextUI.Value)
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
                        Plugin.LogSource.LogDebug("_ammoValueTransform is null");
                    }
                }

                if (Plugin.LoadMagazineImageUI.Value)
                {
                    if (_imageTransform != null)
                    {
                        clonedMagImageTransform = Object.Instantiate(_imageTransform, EFTBattleUIScreenTransform);
                        SetUI(clonedMagImageTransform, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
                    }
                    else
                    {
                        Plugin.LogSource.LogDebug("_imageTransform is null");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"LoadAmmoUI::ShowLoadAmmoUI {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
            }
        }

        public static void SetUI(Transform transform, Vector2? offset = null, Vector3? scale = null)
        {
            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.anchoredPosition = offset != null ? (Vector2)offset : Vector2.zero;
            rectTransform.localScale = scale != null ? (Vector3)scale : Vector3.one;
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
