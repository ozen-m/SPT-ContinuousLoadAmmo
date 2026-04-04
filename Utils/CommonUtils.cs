using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.Communications;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete

namespace ContinuousLoadAmmo.Utils;

public static class CommonUtils
{
    public static bool InRaid => GClass2340.InRaid;

    public static Transform EftBattleUIScreenTransform
    {
        get
        {
            if (field != null) return field;

            field = Singleton<CommonUI>.Instance.EftBattleUIScreen.transform;
            return field;
        }
    }

    public static InputTree InputTree
    {
        get
        {
            // Thanks Fika team/Lacyway!
            if (field != null) return field;

            var inputObj = GameObject.Find("___Input");
            if (inputObj == null)
            {
                throw new NullReferenceException("Could not find InputTree object!");
            }

            field = inputObj.GetComponent<InputTree>();
            return field;
        }
    }

    /// <summary>
    /// Check if magazine has ammo that doesn't match ammoToLoad's caliber
    /// </summary>
    public static bool HasAmmoWithDifferentCaliber(this MagazineItemClass magazine, AmmoItemClass ammoToLoad)
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
        Predicate<GClass3248> goDeeperPredicate = null
    ) where TItem : Item
    {
        foreach (var equipmentSlot in equipmentSlots)
        {
            if (inventoryEquipment.GetSlot(equipmentSlot).ContainedItem is not GClass3248 parentContainer
                || (goDeeperPredicate is not null && !goDeeperPredicate(parentContainer)))
            {
                continue;
            }

            foreach (var container in parentContainer.Containers)
            {
                foreach (var item in container.Items)
                {
                    if (item is GClass3248 childContainer && (goDeeperPredicate is null || goDeeperPredicate(childContainer)))
                    {
                        childContainer.GetAllItemsOfContainer(preAllocatedList, predicate, goDeeperPredicate);
                    }
                    if (item is TItem genericItem && (predicate is null || predicate(genericItem)))
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
        Predicate<GClass3248> goDeeperPredicate = null
    ) where TItem : Item
    {
        foreach (var container in parentContainer.Containers)
        {
            foreach (var item in container.Items)
            {
                if (item is GClass3248 childContainer && (goDeeperPredicate is null || goDeeperPredicate(childContainer)))
                {
                    childContainer.GetAllItemsOfContainer(preAllocatedList, predicate, goDeeperPredicate);
                }
                if (item is TItem genericItem && (predicate is null || predicate(genericItem)))
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
            if (eventArgs is GInterface418 and not GEventArgs10)
            {
                // (GEventArgs10) RemoveFromHandsEventArgs - not considered as busy hands, for successive unloading of different bullets
                return true;
            }
        }
        return false;
    }

    public static void DisplayNotification(
        string message,
        ENotificationIconType iconType = ENotificationIconType.Default,
        bool alwaysDisplay = false,
        ENotificationDurationType duration = ENotificationDurationType.Default
    )
    {
        if (ContinuousLoadAmmo.QuickLoadNotify.Value || alwaysDisplay)
        {
            NotificationManagerClass.DisplayMessageNotification(message, duration, iconType);
        }
    }

    public static string DisplayText(this MagazineBuildPresetClass preset)
    {
        return $"{preset.Name} ({preset.Caliber.Replace("Caliber", string.Empty)})";
    }

    /// <summary>
    /// PP-9 Klin special case
    /// </summary>
    public static string GetWeaponCaliber(this Weapon weapon)
    {
        var ammoCaliber = weapon.AmmoCaliber;
        return ammoCaliber == "9x18PMM" ? "9x18PM" : ammoCaliber;
    }

    public static string GetCaliberReally(this MagazineBuildPresetClass preset)
    {
        return preset.Caliber.Replace("Caliber", string.Empty);
    }
}
