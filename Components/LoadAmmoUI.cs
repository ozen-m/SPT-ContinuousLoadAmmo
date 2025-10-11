using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System;
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
        public static Transform EftBattleUIScreenTransform { get; protected set; }

        protected Transform magUI;
        protected ItemViewLoadAmmoComponent itemViewLoadAmmoComponent;
        protected Image magImage;
        protected GClass907 imageLoader;
        protected Action unbindImageLoader;
        protected TextMeshProUGUI magValue;
        protected CancellationTokenSource cancellationTokenSource;

        protected static FieldInfo itemViewAnimationField;
        protected static FieldInfo itemViewLoadAmmoComponentTemplateField;
        protected static FieldInfo itemViewLoadAmmoComponentCTSField;
        protected static FieldInfo itemViewBottomPanelField;

        public void Init()
        {
            if (EftBattleUIScreenTransform == null)
            {
                EftBattleUIScreenTransform = Singleton<CommonUI>.Instance.EftBattleUIScreen.transform;
            }
            itemViewAnimationField ??= typeof(ItemView).GetField("Animator", BindingFlags.Instance | BindingFlags.NonPublic);
            itemViewLoadAmmoComponentTemplateField ??= typeof(ItemViewAnimation).GetField("_loadAmmoComponentTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
            itemViewLoadAmmoComponentCTSField ??= typeof(ItemViewLoadAmmoComponent).GetField("cancellationTokenSource_0", BindingFlags.Instance | BindingFlags.NonPublic);
            itemViewBottomPanelField ??= typeof(ItemView).GetField("BottomPanel", BindingFlags.Instance | BindingFlags.NonPublic);

            PrepareGameObjects();

            LoadAmmo.Inst.OnStartLoading += Create;
            LoadAmmo.Inst.OnCloseInventory += Show;
            LoadAmmo.Inst.OnEndLoading += Close;
            LoadAmmo.Inst.OnDestroyComponent += Destroy;
        }

        protected void PrepareGameObjects()
        {
            GameObject loadAmmoObj = new("LoadAmmoUI", typeof(RectTransform));
            magUI = loadAmmoObj.transform;
            magUI.SetParent(EftBattleUIScreenTransform);
            SetUI(magUI);

            GameObject imageObj = new("Image", typeof(RectTransform), typeof(Image));
            imageObj.transform.SetParent(magUI);
            SetUI(imageObj.transform, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
            magImage = imageObj.GetComponent<Image>();
            magImage.enabled = false;
        }

        protected void Create(InventoryController playerInventoryController, LoadAmmo.LoadingEventType eventType, GEventArgs7 loadEvent, GEventArgs8 unloadEvent)
        {
            if (itemViewLoadAmmoComponent == null || magValue == null)
            {
                var magItemView = GridItemView.Create(eventType == LoadAmmo.LoadingEventType.Load ? loadEvent.Item : unloadEvent.Item, new GClass3240(), ItemRotation.Horizontal, playerInventoryController, playerInventoryController, null, null, null, null, null);
                if (itemViewLoadAmmoComponent == null)
                {
                    var itemViewAnimation = (ItemViewAnimation)itemViewAnimationField.GetValue(magItemView);
                    itemViewLoadAmmoComponent = UnityEngine.Object.Instantiate((ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentTemplateField.GetValue(itemViewAnimation), magUI, false);
                    SetUI(itemViewLoadAmmoComponent.transform, new Vector2(0f, -150f), new Vector3(1.5f, 1.5f, 1.5f));
                }
                if (magValue == null)
                {
                    Transform instanceTransform = magItemView.transform;
                    var textMesh = instanceTransform.Find("Info Panel/BottomLayoutGroup/Value").GetComponent<TextMeshProUGUI>();
                    magValue = UnityEngine.Object.Instantiate(textMesh, magUI, false);
                    SetUI(magValue.transform, new Vector2(0f, -190f));
                    magValue.enableWordWrapping = false;
                    magValue.overflowMode = TextOverflowModes.Overflow;
                    magValue.alignment = TextAlignmentOptions.Center;
                    magValue.enabled = false;
                }
                magItemView.Kill();
            }

            if (Plugin.LoadAmmoSpinnerUI.Value)
            {
                if (eventType == LoadAmmo.LoadingEventType.Load)
                {
                    CancellationTokenSource cts = (CancellationTokenSource)itemViewLoadAmmoComponentCTSField.GetValue(itemViewLoadAmmoComponent);
                    cts?.Dispose();
                    itemViewLoadAmmoComponent.Show(loadEvent.LoadTime, loadEvent.LoadCount);
                }
                else if (eventType == LoadAmmo.LoadingEventType.Unload)
                {
                    CancellationTokenSource cts = (CancellationTokenSource)itemViewLoadAmmoComponentCTSField.GetValue(itemViewLoadAmmoComponent);
                    cts?.Dispose();
                    itemViewLoadAmmoComponent.Show(unloadEvent.UnloadTime, unloadEvent.UnloadCount, unloadEvent.StartCount);
                }
            }
        }

        protected void Show(Item item)
        {
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();

                if (Plugin.LoadAmmoTextUI.Value)
                {
                    magValue.enabled = true;
                    _ = UpdateTextValue(magValue, cancellationTokenSource.Token);
                }
                if (Plugin.LoadMagazineImageUI.Value)
                {
                    GetImage(item);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"LoadAmmoUI::Show {ex}");
            }
        }

        protected void GetImage(Item item)
        {
            unbindImageLoader?.Invoke();
            imageLoader = ItemViewFactory.LoadItemIcon(item);
            unbindImageLoader = imageLoader?.Changed.Bind(UpdateImage);
        }

        protected void UpdateImage()
        {
            if (imageLoader.Sprite == null) return;

            magImage.sprite = imageLoader.Sprite;
            magImage.SetNativeSize();
            magImage.enabled = true;
        }

        protected async Task UpdateTextValue(TextMeshProUGUI textMesh, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                textMesh.SetText(LoadAmmo.Inst.GetMagAmmoCountByLevel());

                await Task.Yield();
            }
        }

        protected void Close()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            if (itemViewLoadAmmoComponent != null)
            {
                CancellationTokenSource cts = (CancellationTokenSource)itemViewLoadAmmoComponentCTSField.GetValue(itemViewLoadAmmoComponent);
                cts?.Cancel();
                itemViewLoadAmmoComponent.gameObject.SetActive(false);
            }
            if (magImage != null)
            {
                magImage.enabled = false;
            }
            unbindImageLoader?.Invoke();
            if (magValue != null)
            {
                magValue.enabled = false;
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

        public void Destroy()
        {
            if (magUI != null)
            {
                UnityEngine.Object.Destroy(magUI.gameObject);
            }
            LoadAmmo.Inst.OnStartLoading -= Create;
            LoadAmmo.Inst.OnCloseInventory -= Show;
            LoadAmmo.Inst.OnEndLoading -= Close;
            LoadAmmo.Inst.OnDestroyComponent -= Destroy;
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
