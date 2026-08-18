using System;
using System.Threading;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ContinuousLoadAmmo.Controllers;

public class LoadAmmoUI
{
    private LoadAmmoController _loadAmmoController;
    private Transform _loadUITransform;
    private ItemViewLoadAmmoComponent _itemViewLoadAmmoComponent;
    private Image _magImage;
    private ItemIcon _itemIcon;
    private Action _unbindImageLoader;
    private TextMeshProUGUI _magValue;

    public void Initialize(Transform parent, LoadAmmoController loadAmmoController)
    {
        _loadAmmoController = loadAmmoController;
        SubscribeToController();
        if (_loadUITransform != null)
        {
            _loadUITransform.gameObject.SetActive(true);
            return;
        }

        PrepareGameObjects(parent);
        CloneTemplates();
    }

    public static void SetUI(Transform transform, Vector2? offset = null, Vector3? scale = null)
    {
        var rectTransform = (RectTransform)transform;
        rectTransform.anchoredPosition = offset ?? Vector2.zero;
        rectTransform.localScale = scale ?? Vector3.one;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void SubscribeToController()
    {
        _loadAmmoController.OnStartLoading += HandleStart;
        _loadAmmoController.OnCloseInventoryLoading += Show;
        _loadAmmoController.OnEndLoading += Close;
        _loadAmmoController.PlayerInventoryController.OnAmmoLoaded += UpdateTextValue;
        _loadAmmoController.PlayerInventoryController.OnAmmoUnloaded += UpdateTextValue;
        _loadAmmoController.OnPlayerDestroy += Dispose;
    }

    private void PrepareGameObjects(Transform parent)
    {
        GameObject loadAmmoObj = new(nameof(LoadAmmoUI), typeof(RectTransform));
        _loadUITransform = loadAmmoObj.transform;
        _loadUITransform.SetParent(parent);
        SetUI(_loadUITransform);

        GameObject imageObj = new(nameof(Image), typeof(RectTransform), typeof(Image));
        imageObj.transform.SetParent(_loadUITransform);
        SetUI(imageObj.transform, new Vector2(0f, -150f), new Vector3(0.25f, 0.25f, 0.25f));
        _magImage = imageObj.GetComponent<Image>();
        _magImage.enabled = false;
    }

    private void CloneTemplates()
    {
        var gridItemView = ItemViewFactory.CreateFromPrefab<GridItemView>("grid_layout");
        var itemViewAnimation = gridItemView.Animator;
        var itemViewLoadAmmoComponentTemplate = itemViewAnimation._loadAmmoComponentTemplate;
        _itemViewLoadAmmoComponent = Object.Instantiate(itemViewLoadAmmoComponentTemplate, _loadUITransform, false);
        SetUI(_itemViewLoadAmmoComponent.transform, new Vector2(0f, -150f), new Vector3(1.5f, 1.5f, 1.5f));

        var itemViewBottomPanelTemplate = gridItemView.BottomPanel;
        _magValue = Object.Instantiate(itemViewBottomPanelTemplate!.ItemValue, _loadUITransform, false);
        SetUI(_magValue.transform, new Vector2(0f, -190f));
        _magValue.enableWordWrapping = false;
        _magValue.overflowMode = TextOverflowModes.Overflow;
        _magValue.alignment = TextAlignmentOptions.Center;
        _magValue.enabled = false;

        gridItemView.Kill();
    }

    private void HandleStart(float oneAmmoDuration, int ammoTotal, int ammoDone)
    {
        var cts = _itemViewLoadAmmoCtsField(_itemViewLoadAmmoComponent);
        cts?.Dispose();
        _itemViewLoadAmmoComponent.Show(oneAmmoDuration, ammoTotal, ammoDone);
    }

    private void Show(Item item)
    {
        _magValue.enabled = true;
        _magValue.text = _loadAmmoController.GetMagAmmoCountByLevel();

        GetImage(item);
    }

    private void GetImage(Item item)
    {
        _unbindImageLoader?.Invoke();
        _itemIcon = ItemViewFactory.LoadItemIcon(item);
        _unbindImageLoader = _itemIcon?.Changed.Bind(UpdateImage);
    }

    private void UpdateImage()
    {
        if (_itemIcon.Sprite == null) return;

        _magImage.sprite = _itemIcon.Sprite;
        _magImage.SetNativeSize();
        _magImage.enabled = true;
    }

    private void UpdateTextValue(int count)
    {
        if (_loadAmmoController.IsInventoryOpened) return;

        _magValue.SetText(_loadAmmoController.GetMagAmmoCountByLevel());
    }

    private void Close()
    {
        if (_itemViewLoadAmmoComponent != null)
        {
            var cts = _itemViewLoadAmmoCtsField(_itemViewLoadAmmoComponent);
            cts?.Cancel();
            _itemViewLoadAmmoComponent.gameObject.SetActive(false);
        }
        if (_magImage != null)
        {
            _magImage.enabled = false;
        }
        _unbindImageLoader?.Invoke();
        _unbindImageLoader = null;
        if (_magValue != null)
        {
            _magValue.enabled = false;
        }
    }

    public void Dispose()
    {
        Close();
        _loadUITransform.gameObject.SetActive(false);
#if DEBUG
        Object.Destroy(_loadUITransform.gameObject);
#endif
        if (_loadAmmoController is null) return;

        _loadAmmoController.OnStartLoading -= HandleStart;
        _loadAmmoController.OnCloseInventoryLoading -= Show;
        _loadAmmoController.OnEndLoading -= Close;
        _loadAmmoController.PlayerInventoryController.OnAmmoLoaded -= UpdateTextValue;
        _loadAmmoController.PlayerInventoryController.OnAmmoUnloaded -= UpdateTextValue;
        _loadAmmoController.OnPlayerDestroy -= Dispose;
        _loadAmmoController = null;
    }

    private static readonly AccessTools.FieldRef<ItemViewLoadAmmoComponent, CancellationTokenSource> _itemViewLoadAmmoCtsField =
        AccessTools.FieldRefAccess<ItemViewLoadAmmoComponent, CancellationTokenSource>("_taskCancellation");
}
