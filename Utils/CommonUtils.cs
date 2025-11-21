using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete

namespace ContinuousLoadAmmo.Utils;

public static class CommonUtils
{
    public static bool InRaid => GClass2340.InRaid;

    private static Transform _eftBattleUIScreenTransform;

    public static Transform EftBattleUIScreenTransform
    {
        get
        {
            if (_eftBattleUIScreenTransform != null) return _eftBattleUIScreenTransform;

            _eftBattleUIScreenTransform = Singleton<CommonUI>.Instance.EftBattleUIScreen.transform;
            return _eftBattleUIScreenTransform;
        }
    }

    private static InputTree _inputTree;

    public static InputTree InputTree
    {
        get
        {
            // Thanks Lacyway!
            if (_inputTree != null) return _inputTree;

            var inputObj = GameObject.Find("___Input");
            if (inputObj == null)
            {
                throw new NullReferenceException("Could not find InputTree object!");
            }

            _inputTree = inputObj.GetComponent<InputTree>();
            return _inputTree;
        }
    }

    /// <summary>
    /// Check if magazine has ammo that doesn't match ammoToLoad's caliber
    /// </summary>
    public static bool CheckIfAnyDifferentCaliber(this MagazineItemClass magazine, AmmoItemClass ammoToLoad)
    {
        foreach (var cartridge in magazine.Cartridges.Items_1)
        {
            if (cartridge is not AmmoItemClass cartridgeAmmo) continue;

            if (cartridgeAmmo.Caliber != ammoToLoad.Caliber) return true;
        }
        return false;
    }

    /// <summary>
    /// Get acceptable items recursively
    /// </summary>
    public static void GetAcceptableItemsNonAlloc<TItem>(
        this InventoryEquipment inventoryEquipment,
        EquipmentSlot[] equipmentSlots,
        List<TItem> preAllocatedList,
        Predicate<TItem> predicate = null,
        Predicate<GClass3248> goDeeperPredicate = null)
        where TItem : Item
    {
        foreach (EquipmentSlot equipmentSlot in equipmentSlots)
        {
            if (inventoryEquipment.GetSlot(equipmentSlot).ContainedItem is not GClass3248 parentContainer || (goDeeperPredicate != null && !goDeeperPredicate(parentContainer))) continue;

            foreach (var container in parentContainer.Containers)
            {
                foreach (Item item in container.Items)
                {
                    if (item is GClass3248 childContainer && (goDeeperPredicate == null || goDeeperPredicate(childContainer)))
                    {
                        childContainer.GetAllItemsOfContainer(preAllocatedList, predicate, goDeeperPredicate);
                    }
                    if (item is TItem genericItem && (predicate == null || predicate(genericItem)))
                    {
                        preAllocatedList.Add(genericItem);
                    }
                }
            }
        }
    }

    public static void GetAllItemsOfContainer<TItem>(
        this GClass3248 parentContainer,
        List<TItem> preAllocatedList,
        Predicate<TItem> predicate = null,
        Predicate<GClass3248> goDeeperPredicate = null)
        where TItem : Item
    {
        foreach (var container in parentContainer.Containers)
        {
            foreach (Item item in container.Items)
            {
                if (item is GClass3248 childContainer && (goDeeperPredicate == null || goDeeperPredicate(childContainer)))
                {
                    childContainer.GetAllItemsOfContainer(preAllocatedList, predicate, goDeeperPredicate);
                }
                if (item is TItem genericItem && (predicate == null || predicate(genericItem)))
                {
                    preAllocatedList.Add(genericItem);
                }
            }
        }
    }

    public static bool HasAnyHandsActionNonLinq(this TraderControllerClass traderController)
    {
        foreach (var eventArgs in traderController.List_0)
        {
            if (eventArgs is GInterface418)
            {
                return true;
            }
        }
        return false;
    }
}
