using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using ContinuousLoadAmmo.Controllers;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ContinuousLoadAmmo.Components;

public class LoadAmmoUI
{
    public static Transform EftBattleUIScreenTransform { get; private set; }

    private Transform _magUI;
    private ItemViewLoadAmmoComponent _itemViewLoadAmmoComponent;
    private Image _magImage;
    private GClass929 _imageLoader;
    private Action _unbindImageLoader;
    private TextMeshProUGUI _magValue;
    private CancellationTokenSource _cancellationTokenSource;

    public void Init()
    {
        if (EftBattleUIScreenTransform == null)
        {
            EftBattleUIScreenTransform = Singleton<CommonUI>.Instance.EftBattleUIScreen.transform;
        }

        PrepareGameObjects();
        CloneTemplates();

        LoadAmmoController.Inst.OnStartLoading += Start;
        LoadAmmoController.Inst.OnCloseInventory += Show;
        LoadAmmoController.Inst.OnEndLoading += Close;
        LoadAmmoController.Inst.OnDestroyComponent += Destroy;
    }

    public static void SetUI(Transform transform, Vector2? offset = null, Vector3? scale = null)
    {
        RectTransform rectTransform = (RectTransform)transform;
        rectTransform.anchoredPosition = offset ?? Vector2.zero;
        rectTransform.localScale = scale ?? Vector3.one;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    public bool IsSameLoaderUI(ItemViewLoadAmmoComponent component) => LoadAmmoController.Inst.IsActive && _itemViewLoadAmmoComponent == component;

    private void PrepareGameObjects()
    {
        GameObject loadAmmoObj = new("LoadAmmoUI", typeof(RectTransform));
        _magUI = loadAmmoObj.transform;
        _magUI.SetParent(EftBattleUIScreenTransform);
        SetUI(_magUI);

        GameObject imageObj = new("Image", typeof(RectTransform), typeof(Image));
        imageObj.transform.SetParent(_magUI);
        SetUI(imageObj.transform, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
        _magImage = imageObj.GetComponent<Image>();
        _magImage.enabled = false;
    }

    private void CloneTemplates()
    {
        GridItemView gridItemView = ItemViewFactory.CreateFromPool<GridItemView>("grid_layout");

        var itemViewAnimationField = typeof(ItemView).GetField("Animator", BindingFlags.Instance | BindingFlags.NonPublic);
        var itemViewAnimation = (ItemViewAnimation)itemViewAnimationField!.GetValue(gridItemView);

        var itemViewLoadAmmoComponentTemplateField = typeof(ItemViewAnimation).GetField("_loadAmmoComponentTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
        _itemViewLoadAmmoComponent = Object.Instantiate((ItemViewLoadAmmoComponent)itemViewLoadAmmoComponentTemplateField!.GetValue(itemViewAnimation), _magUI, false);
        SetUI(_itemViewLoadAmmoComponent.transform, new Vector2(0f, -150f), new Vector3(1.5f, 1.5f, 1.5f));

        var itemViewBottomPanelField = typeof(ItemView).GetField("BottomPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        _magValue = Object.Instantiate(((ItemViewBottomPanel)itemViewBottomPanelField!.GetValue(gridItemView)).ItemValue, _magUI, false);
        SetUI(_magValue.transform, new Vector2(0f, -190f));
        _magValue.enableWordWrapping = false;
        _magValue.overflowMode = TextOverflowModes.Overflow;
        _magValue.alignment = TextAlignmentOptions.Center;
        _magValue.enabled = false;

        gridItemView.Kill();
    }

    private void Start(float oneAmmoDuration, int ammoTotal, int ammoDone = 0)
    {
        CancellationTokenSource cts = _itemViewLoadAmmoCtsField(_itemViewLoadAmmoComponent);
        cts?.Dispose();
        _itemViewLoadAmmoComponent.Show(oneAmmoDuration, ammoTotal, ammoDone);
    }

    private void Show(Item item)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        _magValue.enabled = true;
        _ = UpdateTextValueAsync(_cancellationTokenSource.Token);

        GetImage(item);
    }

    private void GetImage(Item item)
    {
        _unbindImageLoader?.Invoke();
        _imageLoader = ItemViewFactory.LoadItemIcon(item);
        _unbindImageLoader = _imageLoader?.Changed.Bind(UpdateImage);
    }

    private void UpdateImage()
    {
        if (_imageLoader.Sprite == null) return;

        _magImage.sprite = _imageLoader.Sprite;
        _magImage.SetNativeSize();
        _magImage.enabled = true;
    }

    private async Task UpdateTextValueAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _magValue.SetText(LoadAmmoController.Inst.GetMagAmmoCountByLevel());

            await Task.Yield();
        }
    }

    private void Close()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        if (_itemViewLoadAmmoComponent != null)
        {
            CancellationTokenSource cts = _itemViewLoadAmmoCtsField(_itemViewLoadAmmoComponent);
            cts?.Cancel();
            _itemViewLoadAmmoComponent.gameObject.SetActive(false);
        }
        if (_magImage != null)
        {
            _magImage.enabled = false;
        }
        _unbindImageLoader?.Invoke();
        if (_magValue != null)
        {
            _magValue.enabled = false;
        }
    }

    private void Destroy()
    {
        if (_magUI != null)
        {
            Object.Destroy(_magUI.gameObject);
        }
        LoadAmmoController.Inst.OnStartLoading -= Start;
        LoadAmmoController.Inst.OnCloseInventory -= Show;
        LoadAmmoController.Inst.OnEndLoading -= Close;
        LoadAmmoController.Inst.OnDestroyComponent -= Destroy;
    }

    private static readonly AccessTools.FieldRef<ItemViewLoadAmmoComponent, CancellationTokenSource> _itemViewLoadAmmoCtsField =
        AccessTools.FieldRefAccess<ItemViewLoadAmmoComponent, CancellationTokenSource>("cancellationTokenSource_0");
}
