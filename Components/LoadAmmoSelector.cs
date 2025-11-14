using System.Collections.Generic;
using System.Threading.Tasks;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace ContinuousLoadAmmo.Components;

public class LoadAmmoSelector // : AmmoSelector
{
    private readonly List<GridItemView> _gridItemViews = [];
    private readonly List<AmmoItemClass> _ammoItems = [];
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

    public Task<AmmoItemClass> ShowAcceptableAmmosAsync(IEnumerable<AmmoItemClass> foundAmmos, InventoryController inventoryController) // method_5
    {
        foreach (AmmoItemClass foundAmmo in foundAmmos)
        {
            GridItemView view = GridItemView.Create(foundAmmo, new GClass3450(), ItemRotation.Horizontal, inventoryController, inventoryController, null, null, null, null, null);
            _gridItemViews.Add(view);
            _ammoItems.Add(foundAmmo);
        }
        SetLayout();
        _index = 0;
        HighlightIndex(_index, 0);

        SetChosenAmmo(null);
        _chosenAmmoTcs = new TaskCompletionSource<AmmoItemClass>();
        _ = InputLoopAsync();

        return _chosenAmmoTcs.Task;
    }

    private async Task InputLoopAsync()
    {
        await Task.Yield(); // Frame timing
        while (IsShown)
        {
            await Task.Yield();

            var scroll = Input.mouseScrollDelta.y;
            if (Input.GetKey(Plugin.LoadAmmoHotkey.Value.MainKey) && scroll > 0f) // scroll up
            {
                Next();
            }
            else if (Input.GetKey(Plugin.LoadAmmoHotkey.Value.MainKey) && scroll < 0f)
            {
                Previous();
            }
            else if (Input.GetKeyUp(Plugin.LoadAmmoHotkey.Value.MainKey))
            {
                SetChosenAmmo(GetSelectedAmmo());
                Close();
                break;
            }
        }
    }

    private void SetChosenAmmo(AmmoItemClass ammo)
    {
        _chosenAmmoTcs?.SetResult(ammo);
        _chosenAmmoTcs = null;
    }

    private AmmoItemClass GetSelectedAmmo()
    {
        return _index >= _ammoItems.Count
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
        int num = _gridItemViews.Count + 1;
        Index = (Index + 1) % num;
    }

    private void Next() // method_4
    {
        int num = _gridItemViews.Count + 1;
        Index = (Index - 1 + num) % num;
    }

    private void SetLayout()
    {
        // Use `LayoutElement layoutElement = view.gameObject.AddComponent<LayoutElement>();`?
        if (_gridItemViews == null || _gridItemViews.Count == 0) return;

        const float spacing = 5f;
        float gridWidth = ((RectTransform)_gridItemViews[0].transform).rect.width;

        float totalGridWidth = spacing + gridWidth;
        float totalWidth = ((_gridItemViews.Count - 1) * totalGridWidth) - spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < _gridItemViews.Count; i++)
        {
            var transform = _gridItemViews[i].transform;
            transform.SetParent(LoadAmmoUI.EftBattleUIScreenTransform, worldPositionStays: false);
            LoadAmmoUI.SetUI(transform, new Vector2((startX + (i * totalGridWidth)), -150f));
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
}
