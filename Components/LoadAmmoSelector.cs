using System.Collections.Generic;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Utils;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ContinuousLoadAmmo.Components;

public class LoadAmmoSelector : InputNode
{
    private readonly List<GridItemView> _gridItemViews = [];
    private readonly List<AmmoItemClass> _ammoItems = [];
    private LoadAmmo _loadAmmoController;
    private TaskCompletionSource<AmmoItemClass> _chosenAmmoTcs;

    public bool IsShown => _chosenAmmoTcs != null;

    private int _index;

    private int Index
    {
        get => _index;
        set
        {
            if (value == _index) return;

            HighlightIndex(_index, value);
            _index = value;
        }
    }

    public static LoadAmmoSelector Create(GameObject target, LoadAmmo loadAmmoController)
    {
        var loadAmmoSelector = target.AddComponent<LoadAmmoSelector>();
        loadAmmoSelector._loadAmmoController = loadAmmoController;
        return loadAmmoSelector;
    }

    public void Start()
    {
        CommonUtils.InputTree.Add(this);
    }

    public override ETranslateResult TranslateCommand(ECommand command)
    {
        if (!_loadAmmoController.CanLoadOutsideInventory()) return ETranslateResult.Ignore;

        if (_loadAmmoController.IsActive)
        {
            if (command.IsCommand(ECommand.ToggleShooting) ||
                command.IsCommand(ECommand.ToggleAlternativeShooting))
            {
                _loadAmmoController.StopLoading();
                return ETranslateResult.Block;
            }
            return ETranslateResult.Ignore;
        }

        if (IsShown)
        {
            if (command.IsCommand(ECommand.ScrollNext))
            {
                Next();
                return ETranslateResult.Block;
            }
            if (command.IsCommand(ECommand.ScrollPrevious))
            {
                Previous();
                return ETranslateResult.Block;
            }
            if (Input.GetKeyUp(Plugin.LoadAmmoHotkey.Value.MainKey))
            {
                SetChosenAmmo(GetSelectedAmmo());
                Close();
            }
            return ETranslateResult.Ignore;
        }

        if (Input.GetKey(Plugin.LoadAmmoHotkey.Value.MainKey) &&
            (command.IsCommand(ECommand.ScrollNext) || command.IsCommand(ECommand.ScrollPrevious)))
        {
            _ = OpenAmmoSelectorAsync();
            return ETranslateResult.Block;
        }
        if (Input.GetKeyUp(Plugin.LoadAmmoHotkey.Value.MainKey))
        {
            _loadAmmoController.TryQuickLoadAmmo();
        }
        return ETranslateResult.Ignore;
    }

    public override void TranslateAxes(ref float[] axes)
    {
    }

    public override ECursorResult ShouldLockCursor() => ECursorResult.Ignore;

    public void OnDestroy() => CommonUtils.InputTree.Remove(this);

    private async Task OpenAmmoSelectorAsync()
    {
        if (!_loadAmmoController.IsLoadAmmoAvailable(out List<AmmoItemClass> reachableAmmo, out MagazineItemClass foundMagazine)) return;

        AmmoItemClass chosenAmmo = await ShowAcceptableAmmoAsync(reachableAmmo, _loadAmmoController.PlayerInventoryController);
        if (chosenAmmo != null)
        {
            _loadAmmoController.LoadMagazine(chosenAmmo, foundMagazine);
        }
    }

    private Task<AmmoItemClass> ShowAcceptableAmmoAsync(List<AmmoItemClass> foundAmmos, InventoryController inventoryController) // method_5
    {
        foreach (AmmoItemClass foundAmmo in foundAmmos)
        {
            GridItemView view = GridItemView.Create(foundAmmo, new GClass3450(), ItemRotation.Horizontal, inventoryController, inventoryController, null, null, null, null, null);
            _gridItemViews.Add(view);
            _ammoItems.Add(foundAmmo);
        }
        AddCancelView(foundAmmos[0], inventoryController);

        SetLayout();
        _index = 0;
        HighlightIndex(_index, 0);

        SetChosenAmmo(null);
        _chosenAmmoTcs = new TaskCompletionSource<AmmoItemClass>();

        return _chosenAmmoTcs.Task;
    }

    private void SetChosenAmmo(AmmoItemClass ammo)
    {
        _chosenAmmoTcs?.SetResult(ammo);
        _chosenAmmoTcs = null;
    }

    private AmmoItemClass GetSelectedAmmo()
    {
        return _index == _ammoItems.Count
            ? null // Cancel/no option is selected
            : _ammoItems[_index];
    }

    private void Close()
    {
        foreach (var gridItemView in _gridItemViews)
        {
            gridItemView.Highlight(false);
            gridItemView.Kill();
        }
        _gridItemViews.Clear();
        _ammoItems.Clear();
        SetChosenAmmo(null);
    }

    private void Previous() // method_3
    {
        int num = _gridItemViews.Count;
        Index = (Index + 1) % num;
    }

    private void Next() // method_4
    {
        int num = _gridItemViews.Count;
        Index = (Index - 1 + num) % num;
    }

    private void AddCancelView(AmmoItemClass templateItem, InventoryController inventoryController)
    {
        GridItemView cancelView = GridItemView.Create(templateItem, new GClass3450(), ItemRotation.Horizontal, inventoryController, inventoryController, null, null, null, null, null);
        _infoPanelField(cancelView).gameObject.SetActive(false);
        _backgroundColorField(cancelView) = Color.clear;
        _mainImageAlphaField(cancelView) = 0f; // method_4 checks this for alpha
        var mainImage = _mainImageField(cancelView);
        mainImage.color = mainImage.color with { a = 0f };
        cancelView.ChangeSelectedStatus(true); // Red stripes
        cancelView.UpdateColor(); // Update color with alpha 0f
        _gridItemViews.Add(cancelView);
    }

    private void SetLayout()
    {
        if (_gridItemViews == null || _gridItemViews.Count == 0) return;

        const float spacing = 5f;
        float gridWidth = ((RectTransform)_gridItemViews[0].transform).rect.width;

        float totalGridWidth = spacing + gridWidth;
        float totalWidth = ((_gridItemViews.Count - 1) * totalGridWidth) - spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < _gridItemViews.Count; i++)
        {
            var viewTransform = _gridItemViews[i].transform;
            viewTransform.SetParent(LoadAmmoUI.EftBattleUIScreenTransform, worldPositionStays: false);
            LoadAmmoUI.SetUI(viewTransform, new Vector2((startX + (i * totalGridWidth)), -150f));
        }
    }

    private void HighlightIndex(int prevSelectionIndex, int currentSelectionIndex) // method_1
    {
        HighlightGridItemView(prevSelectionIndex, false);
        HighlightGridItemView(currentSelectionIndex, true);
    }

    private void HighlightGridItemView(int index, bool isSelected) // method_2
    {
        if (index < _gridItemViews.Count)
        {
            _gridItemViews[index].Highlight(isSelected);
        }
    }

    private static readonly AccessTools.FieldRef<GridItemView, Image> _mainImageField =
        AccessTools.FieldRefAccess<GridItemView, Image>("MainImage");

    private static readonly AccessTools.FieldRef<GridItemView, float> _mainImageAlphaField =
        AccessTools.FieldRefAccess<GridItemView, float>("_mainImageAlpha");

    private static readonly AccessTools.FieldRef<GridItemView, Color> _backgroundColorField =
        AccessTools.FieldRefAccess<GridItemView, Color>("BackgroundColor");

    private static readonly AccessTools.FieldRef<GridItemView, RectTransform> _infoPanelField =
        AccessTools.FieldRefAccess<GridItemView, RectTransform>("_infoPanel");
}
