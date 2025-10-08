using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ContinuousLoadAmmo.Components
{
    public class LoadAmmoUI
    {
        protected GridItemView magItemView;
        protected ItemViewLoadAmmoComponent itemViewLoadAmmoComponent;
        protected Transform clonedAmmoValueTransform;
        protected Transform clonedMagImageTransform;
        protected CancellationTokenSource cancellationTokenSource;

        public static Transform EftBattleUIScreenTransform { get; protected set; }
        protected static FieldInfo itemViewLoadAmmoComponentField;
        protected static FieldInfo itemViewAnimationField;

        public void Init()
        {
            EftBattleUIScreenTransform ??= Singleton<CommonUI>.Instance.EftBattleUIScreen.transform;
            itemViewLoadAmmoComponentField ??= typeof(ItemViewAnimation).GetField("itemViewLoadAmmoComponent_0", BindingFlags.Instance | BindingFlags.NonPublic);
            itemViewAnimationField ??= typeof(ItemView).GetField("Animator", BindingFlags.Instance | BindingFlags.NonPublic);

            LoadAmmo.Inst.OnStartLoading += CreateUI;
            LoadAmmo.Inst.OnCloseInventory += Show;
            LoadAmmo.Inst.OnEndLoading += DestroyUI;
            LoadAmmo.Inst.OnDestroyComponent += Unsubscribe;
        }

        protected void CreateUI(InventoryController playerInventoryController, LoadAmmo.LoadingEventType eventType, GEventArgs7 loadEvent, GEventArgs8 unloadEvent)
        {
            // Safeguard?
            if (magItemView != null)
            {
                DestroyUI();
            }

            if (eventType == LoadAmmo.LoadingEventType.Load)
            {
                MagazineItemClass magazine = loadEvent.TargetItem as MagazineItemClass;
                magItemView = GridItemView.Create(magazine, new GClass3243(magazine, EItemViewType.Inventory), ItemRotation.Horizontal, playerInventoryController, playerInventoryController, null, null, null, null, null);
                magItemView.SetLoadMagazineStatus(loadEvent);
            }
            else if (eventType == LoadAmmo.LoadingEventType.Unload)
            {
                MagazineItemClass magazine = unloadEvent.FromItem;
                magItemView = GridItemView.Create(magazine, new GClass3243(magazine, EItemViewType.Inventory), ItemRotation.Horizontal, playerInventoryController, playerInventoryController, null, null, null, null, null);
                magItemView.SetUnloadMagazineStatus(unloadEvent);
            }

            if (Plugin.LoadAmmoSpinnerUI.Value)
            {
                var itemViewAnimation = (ItemViewAnimation)itemViewAnimationField.GetValue(magItemView);
                itemViewLoadAmmoComponent = (ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentField.GetValue(itemViewAnimation);
            }
            Transform instanceTransform = magItemView.transform;
            if (Plugin.LoadAmmoTextUI.Value)
            {
                var ammoValueTransform = instanceTransform.Find("Info Panel/BottomLayoutGroup/Value") ?? instanceTransform.Find("Info Panel/RightLayout/BottomVerticalGroup/Value");
                clonedAmmoValueTransform = Object.Instantiate(ammoValueTransform, EftBattleUIScreenTransform);
            }
            if (Plugin.LoadMagazineImageUI.Value)
            {
                var imageTransform = instanceTransform.Find("Image") ?? instanceTransform.Find("Item Image");
                clonedMagImageTransform = Object.Instantiate(imageTransform, EftBattleUIScreenTransform);
            }
        }

        protected void Show()
        {
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();

                if (itemViewLoadAmmoComponent != null)
                {
                    itemViewLoadAmmoComponent.SetStopButtonStatus(false);

                    Transform transform = itemViewLoadAmmoComponent.transform;
                    transform.SetParent(EftBattleUIScreenTransform, false);
                    SetUI(transform, new Vector2(0f, -150f), new Vector3(1.5f, 1.5f, 1.5f));
                }
                if (clonedAmmoValueTransform != null)
                {
                    SetUI(clonedAmmoValueTransform, new Vector2(0f, -190f));
                    if (clonedAmmoValueTransform.TryGetComponent(out TextMeshProUGUI textMesh))
                    {
                        _ = UpdateTextValue(textMesh, cancellationTokenSource.Token);
                    }
                }
                if (clonedMagImageTransform != null)
                {
                    SetUI(clonedMagImageTransform, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
                    if (clonedMagImageTransform.TryGetComponent(out Image mainImage))
                    {
                        mainImage.color.SetAlpha(1f);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"LoadAmmoUI::Show {ex}");
            }
        }

        protected async Task UpdateTextValue(TextMeshProUGUI textMesh, CancellationToken token)
        {
            textMesh.enableWordWrapping = false;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.alignment = TextAlignmentOptions.Center;

            while (!token.IsCancellationRequested)
            {
                textMesh.SetText(LoadAmmo.Inst.GetMagAmmoCountByLevel());

                await Task.Yield();
            }
        }

        public bool IsSameLoaderUI(ItemViewLoadAmmoComponent component)
        {
            if (LoadAmmo.Inst.IsActive && itemViewLoadAmmoComponent == component)
            {
                return true;
            }
            return false;
        }

        protected void DestroyUI()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            if (itemViewLoadAmmoComponent != null)
            {
                itemViewLoadAmmoComponent.Destroy();
                itemViewLoadAmmoComponent = null;
            }
            if (clonedAmmoValueTransform != null)
            {
                Object.Destroy(clonedAmmoValueTransform.gameObject);
                clonedAmmoValueTransform = null;
            }
            if (clonedMagImageTransform != null)
            {
                Object.Destroy(clonedMagImageTransform.gameObject);
                clonedMagImageTransform = null;
            }
            if (magItemView != null)
            {
                magItemView.Kill();
                magItemView = null;
            }
        }

        public void Unsubscribe()
        {
            LoadAmmo.Inst.OnStartLoading -= CreateUI;
            LoadAmmo.Inst.OnCloseInventory -= Show;
            LoadAmmo.Inst.OnEndLoading -= DestroyUI;
            LoadAmmo.Inst.OnDestroyComponent -= Unsubscribe;
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
    }
}
