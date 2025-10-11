using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ContinuousLoadAmmo.Components
{
    public class LoadAmmoSelector // : AmmoSelector
    {
        protected List<GridItemView> gridItemViews = new();
        protected List<AmmoItemClass> ammoItems = new();
        protected TaskCompletionSource<AmmoItemClass> tcsChosenAmmo;

        public bool IsShown => tcsChosenAmmo != null;

        protected int _index;
        protected int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (value != _index)
                {
                    HighlightIndex(_index, value);
                    _index = value;
                }
            }
        }

        public Task<AmmoItemClass> ShowAcceptableAmmos(IEnumerable<AmmoItemClass> foundAmmos, InventoryController inventoryController) // method_5
        {
            foreach (AmmoItemClass foundAmmo in foundAmmos)
            {
                GridItemView view = GridItemView.Create(foundAmmo, new GClass3450(), ItemRotation.Horizontal, inventoryController, inventoryController, null, null, null, null, null);
                gridItemViews.Add(view);
                ammoItems.Add(foundAmmo);
            }
            SetLayout();
            _index = 0;
            HighlightIndex(_index, 0);

            SetChosenAmmo(null);
            tcsChosenAmmo = new();
            _ = InputLoop();

            return tcsChosenAmmo.Task;
        }

        protected async Task InputLoop()
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

        protected void SetChosenAmmo(AmmoItemClass ammo)
        {
            tcsChosenAmmo?.SetResult(ammo);
            tcsChosenAmmo = null;
        }

        protected AmmoItemClass GetSelectedAmmo()
        {
            if (_index >= ammoItems.Count)
            {
                return null;
            }

            return ammoItems[_index];
        }

        protected void Close()
        {
            foreach (var gridItemView in gridItemViews)
            {
                gridItemView.Highlight(false);
                Object.Destroy(gridItemView.gameObject.GetComponent<LayoutElement>());
                gridItemView.Kill();
            }
            gridItemViews.Clear();
            ammoItems.Clear();
            SetChosenAmmo(null);
        }

        protected void Previous() // method_3
        {
            int num = gridItemViews.Count + 1;
            Index = (Index + 1) % num;
        }

        protected void Next() // method_4
        {
            int num = gridItemViews.Count + 1;
            Index = (Index - 1 + num) % num;
        }

        protected void SetLayout()
        {
            if (gridItemViews == null || gridItemViews.Count == 0) return;

            float spacing = 5f;
            float gridWidth = ((RectTransform)gridItemViews[0].transform).rect.width;

            float totalGridWidth = spacing + gridWidth;
            float totalWidth = ((gridItemViews.Count - 1) * totalGridWidth) - spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < gridItemViews.Count; i++)
            {
                var transform = gridItemViews[i].transform;
                transform.SetParent(LoadAmmoUI.EftBattleUIScreenTransform, worldPositionStays: false);
                LoadAmmoUI.SetUI(transform, new Vector2((startX + (i * totalGridWidth)), -150f));
            }
        }

        protected void HighlightIndex(int prevSelectionIndex, int currentSelectionIndex) // method_1
        {
            HighlightGridItemView(prevSelectionIndex, false);
            HighlightGridItemView(currentSelectionIndex, true);
        }

        protected void HighlightGridItemView(int index, bool isSelected) // method_2
        {
            if (index < gridItemViews.Count)
            {
                gridItemViews[index].Highlight(isSelected);
            }
        }
    }
}
