using Comfort.Common;
using ContinuousLoadAmmo.Components;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ContinuousLoadAmmo.Controllers.LoadAmmo;

namespace ContinuousLoadAmmo.Controllers
{
    internal static class LoadAmmoUI
    {
        private static GridItemView magItemView;
        internal static ItemViewLoadAmmoComponent itemViewLoadAmmoComponent;
        private static Transform clonedAmmoValueTransform;
        private static Transform clonedMagImageTransform;
        private static Transform _ammoValueTransform;
        private static Transform _imageTransform;

        private static readonly FieldInfo itemViewLoadAmmoComponentField = typeof(ItemViewAnimation).GetField("itemViewLoadAmmoComponent_0", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo itemViewAnimationField = typeof(ItemView).GetField("Animator", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Player MainPlayer => LoadAmmoComponent.MainPlayer;
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

        public static void CreateUI(InventoryController playerInventoryController, LoadingEventType eventType, GEventArgs7 loadEvent = null, GEventArgs8 unloadEvent = null)
        {
            magItemView = null;
            if (eventType == LoadingEventType.Load)
            {
                MagazineItemClass magazine = loadEvent.TargetItem as MagazineItemClass;
                magItemView = GridItemView.Create(magazine, new GClass3243(magazine, EItemViewType.Inventory), ItemRotation.Horizontal, playerInventoryController, playerInventoryController, null, null, null, null, null);
                magItemView.SetLoadMagazineStatus(loadEvent);
            }
            else if (eventType == LoadingEventType.Unload)
            {
                MagazineItemClass magazine = unloadEvent.FromItem;
                magItemView = GridItemView.Create(magazine, new GClass3243(magazine, EItemViewType.Inventory), ItemRotation.Horizontal, playerInventoryController, playerInventoryController, null, null, null, null, null);
                magItemView.SetUnloadMagazineStatus(unloadEvent);
            }

            Transform instanceTransform = magItemView.transform;
            if (Plugin.LoadAmmoTextUI.Value)
            {
                _ammoValueTransform = instanceTransform.Find("Info Panel/BottomLayoutGroup/Value") ?? instanceTransform.Find("Info Panel/RightLayout/BottomVerticalGroup/Value");
            }
            if (Plugin.LoadMagazineImageUI.Value)
            {
                _imageTransform = instanceTransform.Find("Image") ?? instanceTransform.Find("Item Image");
            }

            var itemViewAnimation = (ItemViewAnimation)itemViewAnimationField.GetValue(magItemView);
            itemViewLoadAmmoComponent = (ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentField.GetValue(itemViewAnimation);
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
                        if (clonedMagImageTransform.TryGetComponent(out Image mainImage))
                        {
                            mainImage.ChangeImageAlpha(1f);
                        }
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
                    MainPlayer.Profile.MagDrillsMastering,
                    MainPlayer.Profile.CheckedMagazineSkillLevel(LoadAmmo.Magazine.Id),
                    Magazine.CheckOverride
                        ]);
            //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine) // Is mag examined?

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
            if (magItemView != null)
            {
                magItemView.ReturnToPool();
                magItemView = null;
            }
        }
    }
}
