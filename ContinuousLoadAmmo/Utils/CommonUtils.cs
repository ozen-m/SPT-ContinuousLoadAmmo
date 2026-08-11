using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Builds;
using EFT.Communications;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete

namespace ContinuousLoadAmmo.Utils;

public static class CommonUtils
{
    public static bool InRaid => InGameStatus.InRaid;

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
    public static bool HasAmmoWithDifferentCaliber(this Magazine magazine, Ammo ammoToLoad)
    {
        foreach (var cartridge in magazine.Cartridges._items)
        {
            if (cartridge is not Ammo cartridgeAmmo) continue;

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
        Predicate<ContainerCollection> goDeeperPredicate = null
    ) where TItem : Item
    {
        foreach (var equipmentSlot in equipmentSlots)
        {
            if (inventoryEquipment.GetSlot(equipmentSlot).ContainedItem is not ContainerCollection parentContainer
                || (goDeeperPredicate is not null && !goDeeperPredicate(parentContainer)))
            {
                continue;
            }

            foreach (var container in parentContainer.Containers)
            {
                foreach (var item in container.Items)
                {
                    if (item is ContainerCollection childContainer && (goDeeperPredicate is null || goDeeperPredicate(childContainer)))
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
        this ContainerCollection parentContainer,
        List<TItem> preAllocatedList,
        Predicate<TItem> predicate = null,
        Predicate<ContainerCollection> goDeeperPredicate = null
    ) where TItem : Item
    {
        foreach (var container in parentContainer.Containers)
        {
            foreach (var item in container.Items)
            {
                if (item is ContainerCollection childContainer && (goDeeperPredicate is null || goDeeperPredicate(childContainer)))
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

    public static bool HasAnyHandsActionNonLinq(this ItemController itemController)
    {
        foreach (var eventArgs in itemController.ActiveEvents)
        {
            if (eventArgs is IItemInHandsEventArgs and not RemoveFromHandsEventArgs)
            {
                // RemoveFromHandsEventArgs - not considered as busy hands, for successive unloading of different bullets
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
            NotificationManager.DisplayMessageNotification(message, duration, iconType);
        }
    }

    public static string DisplayText(this MagPreset preset)
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

    public static string GetCaliberReally(this MagPreset preset)
    {
        return preset.Caliber.Replace("Caliber", string.Empty);
    }
}
