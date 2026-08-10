using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Controllers;
using ContinuousLoadAmmo.Models;
using ContinuousLoadAmmo.Utils;
using EFT;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ContinuousLoadAmmo.Components;

public class LoadAmmoComponent : InputNode
{
    private readonly List<GridItemView> _gridItemViews = [];
    private readonly List<AmmoItemClass> _ammoItems = [];
    private readonly HashSet<MongoID> _seenAmmoTplScratch = [];
    private LoadAmmoController _loadAmmoControllerController;
    private TaskCompletionSource<AmmoItemClass> _chosenAmmoTcs;
    private GClass3450 _emptySourceContext = new();

    public bool IsShown => _chosenAmmoTcs is not null;

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

    public static LoadAmmoComponent Create(GameObject target, LoadAmmoController loadAmmoControllerController)
    {
        var loadAmmoSelector = target.AddComponent<LoadAmmoComponent>();
        loadAmmoSelector._loadAmmoControllerController = loadAmmoControllerController;
        CommonUtils.InputTree.Add(loadAmmoSelector);
        return loadAmmoSelector;
    }

    public void Update()
    {
        // Transfer to TranslateCommand, our custom hotkey _may_ not be an ECommand
        if (Input.GetKeyUp(ContinuousLoadAmmo.QuickLoadHotkey.Value.MainKey))
        {
            TranslateCommand(ECommand.BeginSpecialInteracting); // No other uses?
        }
    }

    public override ETranslateResult TranslateCommand(ECommand command)
    {
        if (!_loadAmmoControllerController.CanLoadOutsideInventory())
        {
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

            // Select ammo
            if (Input.GetKeyUp(ContinuousLoadAmmo.QuickLoadHotkey.Value.MainKey)
                && command.IsCommand(ECommand.BeginSpecialInteracting)) // Only transferred from update to avoid duplicates
            {
                SetChosenAmmo(GetSelectedAmmo());
                Close();
                return ETranslateResult.Block;
            }
            return ETranslateResult.Ignore;
        }

        if (!_loadAmmoControllerController.IsInventoryOpened)
        {
            if (_loadAmmoControllerController.IsActive)
            {
                // Cancel on shoot/alt shoot, or quick load key, if loading ammo outside inventory
                if (command.IsCommand(ECommand.ToggleShooting)
                    || command.IsCommand(ECommand.ToggleAlternativeShooting)
                    || (Input.GetKeyUp(ContinuousLoadAmmo.QuickLoadHotkey.Value.MainKey)
                        && command.IsCommand(ECommand.BeginSpecialInteracting)))
                {
                    _loadAmmoControllerController.StopLoading();
                    return ETranslateResult.Block;
                }

                return ETranslateResult.Ignore;
            }

            if (Input.GetKey(ContinuousLoadAmmo.QuickLoadHotkey.Value.MainKey)
                && (command.IsCommand(ECommand.ScrollNext) || command.IsCommand(ECommand.ScrollPrevious)))
            {
                _ = OpenAmmoSelectorAsync();
                return ETranslateResult.Block;
            }
        }

        if (Input.GetKeyUp(ContinuousLoadAmmo.QuickLoadHotkey.Value.MainKey)
            && command.IsCommand(ECommand.BeginSpecialInteracting)) // Only transferred from update to avoid duplicates
        {
            if (_loadAmmoControllerController.IsActive)
            {
                _loadAmmoControllerController.StopLoading();
                return ETranslateResult.Block;
            }

            switch (ContinuousLoadAmmo.QuickLoadMode.Value)
            {
                case QuickLoadMode.HighestPenetration:
                case QuickLoadMode.LastBulletMagazine:
                    _loadAmmoControllerController.TryQuickLoadAmmo();
                    break;
                case QuickLoadMode.LastMagazinePreset:
                    _loadAmmoControllerController.TryQuickLoadLastPreset();
                    break;
                default:
                    return ETranslateResult.Ignore;
            }
            return ETranslateResult.Block;
        }
        return ETranslateResult.Ignore;
    }

    public override void TranslateAxes(ref float[] axes)
    {
    }

    public override ECursorResult ShouldLockCursor()
    {
        return ECursorResult.Ignore;
    }

    public void OnDestroy()
    {
        _chosenAmmoTcs?.TrySetResult(null);
        _chosenAmmoTcs = null;
        CommonUtils.InputTree.Remove(this);
    }

    private async Task OpenAmmoSelectorAsync()
    {
        if (!_loadAmmoControllerController.IsQuickLoadAvailable(out var reachableAmmo, out var foundMagazine))
        {
            return;
        }

        // Only show one of each type
        _seenAmmoTplScratch.Clear();
        reachableAmmo.RemoveAll(ShouldRemoveFromList);

        var chosenAmmo = await ShowAcceptableAmmoAsync(reachableAmmo, _loadAmmoControllerController.PlayerInventoryController);
        if (chosenAmmo is not null)
        {
            _loadAmmoControllerController.LoadMagazine(chosenAmmo, foundMagazine);
        }
    }

    private bool ShouldRemoveFromList(AmmoItemClass ammo)
    {
        return !_seenAmmoTplScratch.Add(ammo.TemplateId);
    }

    [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
    private Task<AmmoItemClass> ShowAcceptableAmmoAsync(List<AmmoItemClass> foundAmmo, InventoryController inventoryController) // method_5
    {
        foreach (var ammo in foundAmmo)
        {
            var view = GridItemView.Create(
                ammo,
                _emptySourceContext,
                ItemRotation.Horizontal,
                inventoryController,
                inventoryController,
                null,
                null,
                null,
                null,
                null
            );

            _gridItemViews.Add(view);
            _ammoItems.Add(ammo);
        }
        AddCancelView(foundAmmo[0], inventoryController);

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
        var num = _gridItemViews.Count;
        Index = (Index + 1) % num;
    }

    private void Next() // method_4
    {
        var num = _gridItemViews.Count;
        Index = ((Index - 1) + num) % num;
    }

    private void AddCancelView(AmmoItemClass templateItem, InventoryController inventoryController)
    {
        var cancelView = GridItemView.Create(
            templateItem,
            _emptySourceContext,
            ItemRotation.Horizontal,
            inventoryController,
            inventoryController,
            null,
            null,
            null,
            null,
            null
        );

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
        if (_gridItemViews.Count <= 0) return;

        const float spacing = 5f;
        var gridWidth = ((RectTransform)_gridItemViews[0].transform).rect.width;

        var totalGridWidth = spacing + gridWidth;
        var totalWidth = ((_gridItemViews.Count - 1) * totalGridWidth) - spacing;
        var startX = -totalWidth / 2f;

        for (var i = 0; i < _gridItemViews.Count; i++)
        {
            var viewTransform = _gridItemViews[i].transform;
            viewTransform.SetParent(CommonUtils.EftBattleUIScreenTransform, false);
            LoadAmmoUI.SetUI(viewTransform, new Vector2(startX + (i * totalGridWidth), -150f));
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
