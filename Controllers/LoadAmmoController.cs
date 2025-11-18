using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Patches;
using ContinuousLoadAmmo.Utils;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using UnityEngine;
using static EFT.Player;

namespace ContinuousLoadAmmo.Controllers;

public class LoadAmmoController
{
    private readonly Player _player;
    private MagazineItemClass _magazine;
    private bool _isReachable = true;

    public event Action<float, int, int> OnStartLoading;
    public event Action<Item> OnCloseInventoryLoading;
    public event Action OnEndLoading;

    public bool IsActive => PlayerInventoryController.Interface19_0 != null;
    public bool IsInventoryOpened => _player.IsInventoryOpened;
    public PlayerInventoryController PlayerInventoryController { get; }

    public LoadAmmoController(Player player)
    {
        _player = player;
        if (_player.InventoryController is not PlayerInventoryController playerInvCont)
        {
            throw new InvalidOperationException("Player.InventoryController is not Player.PlayerInventoryController");
        }

        PlayerInventoryController = playerInvCont;
        PlayerInventoryController.SetNextProcessLocked(false);

        PlayerInventoryController.ActiveEventAdded += LoadingStart; // Always CommandStatus.Begin
        InventoryScreenClosePatch.OnInventoryClose += LoadingOutsideInventory; // Sucks to have to use this workaround
        UnloadMagazineStartPatch.OnLoadingEnd += LoadingEnd;
        LoadMagazineStartPatch.OnLoadingEnd += LoadingEnd;
        // _player.OnInventoryOpened += LoadingOutsideInventory; // Why does BSG CALLS THIS _TWICE_
        // _player.InventoryController.ActiveEventsChanged += LoadingEnd; // Can't use since always CommandStatus.Begin, but why
        _player.OnHandsControllerChanged += StopLoadingOnHandsChange;
        _player.OnIPlayerDeadOrUnspawn += Destroy;
    }

    public bool CanLoadOutsideInventory()
    {
        return !PlayerInventoryController.HasAnyHandsAction() &&
               _isReachable;
    }

    public bool IsLoadAmmoAvailable(out List<AmmoItemClass> reachableAmmo, out MagazineItemClass foundMagazine)
    {
        reachableAmmo = null;
        foundMagazine = null;
        return GetAmmoItemsFromEquipment(out reachableAmmo) && GetMagazineForAmmo(reachableAmmo[0], out foundMagazine);
    }

    public void TryQuickLoadAmmo()
    {
        if (!IsLoadAmmoAvailable(out List<AmmoItemClass> reachableAmmo, out MagazineItemClass foundMagazine)) return;

        AmmoItemClass chosenAmmo = null;
        if (!ContinuousLoadAmmo.PrioritizeHighestPenetration.Value)
        {
            MagazineItemClass currentMagazine = _player.LastEquippedWeaponOrKnifeItem.GetCurrentMagazine();
            if (currentMagazine != null)
            {
                foreach (var currAmmo in reachableAmmo)
                {
                    if (currentMagazine.FirstRealAmmo() is not AmmoItemClass ammoInsideMag || ammoInsideMag.TemplateId != currAmmo.TemplateId) continue;

                    // Magazine ammo matched with current reachable ammo
                    chosenAmmo = currAmmo;
                    break;
                }
            }
        }
        // PrioritizeHighestPenetration is false or if no ammo matched from magazine's first ammo, choose first reachable ammo available
        chosenAmmo ??= reachableAmmo[0];
        LoadMagazine(chosenAmmo, foundMagazine);

        if (ContinuousLoadAmmo.QuickLoadNotify.Value)
        {
            NotificationManagerClass.DisplayMessageNotification($"Loading {chosenAmmo.LocalizedShortName()}", iconType: ENotificationIconType.Note);
        }
    }

    public void LoadMagazine(AmmoItemClass ammo, MagazineItemClass magazine)
    {
        //Plugin.LogSource.LogDebug($"Mag {magazine.LocalizedShortName()} ({magazine.Count}); Ammo {ammo.LocalizedShortName()} ({ammo.StackObjectsCount})");
        int loadCount = Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);
        _ = PlayerInventoryController.LoadMagazine(ammo, magazine, loadCount, false);
    }

    /// <summary>
    /// Find reachable magazine for ammo
    /// </summary>
    /// <param name="ammo">Ammo that should be compatible with the magazine</param>
    /// <returns></returns>
    public bool GetMagazineForAmmo(AmmoItemClass ammo, out MagazineItemClass foundMagazine)
    {
        foundMagazine = null;
        var foundMagazines = new List<MagazineItemClass>();
        if (ContinuousLoadAmmo.ReachableOnly.Value)
        {
            // Only get top level container's items for quick load
            PlayerInventoryController.GetAcceptableItemsNonAlloc(
                ReachableSlots,
                foundMagazines,
                (mag) =>
                    PlayerInventoryController.Examined(mag) &&
                    mag.Count != mag.MaxCount &&
                    mag.CheckCompatibility(ammo),
                (container) =>
                    container is not SearchableItemItemClass searchableContainer ||
                    PlayerInventoryController.SearchController.IsSearched(searchableContainer) /* Only searched containers */
            );
        }
        else
        {
            // Can be recursive
            GetReachableItems(
                foundMagazines,
                (mag) =>
                    PlayerInventoryController.Examined(mag) &&
                    mag.Count != mag.MaxCount &&
                    mag.CheckCompatibility(ammo)
            );
        }
        if (foundMagazines.Count <= 0) return false;

        // Some magazines can have multiple calibers
        foundMagazines.RemoveAll(mag => mag.CheckIfAnyDifferentCaliber(ammo));

        // Sort by almost full
        foundMagazines.Sort((a, b) =>
            (a.MaxCount - a.Count).CompareTo(b.MaxCount - b.Count)
        );
        // Mag with most amount
        foundMagazine = foundMagazines[0];
        return true;
    }

    /// <summary>
    /// Find reachable ammo for the current weapon.
    /// </summary>
    /// <param name="reachableAmmo">One of each ammo type found then sorted by Penetration Power descending</param>
    /// <returns></returns>
    public bool GetAmmoItemsFromEquipment(out List<AmmoItemClass> reachableAmmo)
    {
        reachableAmmo = [];
        if (_player.LastEquippedWeaponOrKnifeItem is not Weapon weapon) return false;

        string weaponCaliber = weapon.AmmoCaliber;
        if (ContinuousLoadAmmo.ReachableOnly.Value)
        {
            // Only get top level container's items for quick load
            PlayerInventoryController.GetAcceptableItemsNonAlloc(
                ReachableSlots,
                reachableAmmo,
                (ammo) =>
                    PlayerInventoryController.Examined(ammo) &&
                    ammo.Caliber == weaponCaliber &&
                    ammo.Parent.Container.ParentItem is not MagazineItemClass /* Do not pull from ammo inside mags */,
                (container) =>
                    container is not SearchableItemItemClass searchableContainer ||
                    PlayerInventoryController.SearchController.IsSearched(searchableContainer) /* Only searched containers */
            );
        }
        else
        {
            // Can be recursive
            GetReachableItems(
                reachableAmmo,
                (ammo) =>
                    PlayerInventoryController.Examined(ammo) &&
                    ammo.Caliber == weaponCaliber &&
                    ammo.Parent.Container.ParentItem is not MagazineItemClass /* Do not pull from ammo inside mags */
            );
        }
        if (reachableAmmo.Count <= 0) return false;

        // Sort penetration power highest to lowest, then stack count ascending
        reachableAmmo.Sort((a, b) =>
        {
            int result = b.PenetrationPower.CompareTo(a.PenetrationPower);
            if (result == 0)
            {
                result = a.StackObjectsCount.CompareTo(b.StackObjectsCount);
            }
            return result;
        });
        // Only return one of each type
        var seen = new HashSet<MongoID>();
        reachableAmmo.RemoveAll(ammo => !seen.Add(ammo.TemplateId));
        return true;
    }

    private void LoadingStart(GEventArgs1 eventArgs)
    {
        // if (eventArgs.Status != CommandStatus.Begin) return;

        switch (eventArgs)
        {
            case GEventArgs7 loadEvent:
                if (loadEvent.TargetItem is not MagazineItemClass loadMagazine || loadEvent.Item is not AmmoItemClass ammo) return;

                _magazine = loadMagazine;
                _isReachable = IsAtReachablePlace(_magazine, ammo);
                OnStartLoading?.Invoke(loadEvent.LoadTime, loadEvent.LoadCount, 0);
                break;
            case GEventArgs8 unloadEvent:
                // ReSharper disable once ConvertTypeCheckPatternToNullCheck
                if (unloadEvent.FromItem is not MagazineItemClass unloadMagazine) return;

                _magazine = unloadMagazine;
                _isReachable = IsAtReachablePlace(_magazine);
                OnStartLoading?.Invoke(unloadEvent.UnloadTime, unloadEvent.UnloadCount, unloadEvent.StartCount);
                break;
            default:
                return;
        }

        // Started loading from outside the inventory
        if (!_player.IsInventoryOpened)
        {
            LoadingOutsideInventory();
        }
    }

    private void LoadingOutsideInventory()
    {
        if (IsActive && CanLoadOutsideInventory())
        {
            _ = SetPlayerStateAsync(true);
            OnCloseInventoryLoading?.Invoke(_magazine);
            return;
        }
        StopLoading();
    }

    private void LoadingEnd()
    {
        _ = SetPlayerStateAsync(false);
        ResetLoading();
        OnEndLoading?.Invoke();
    }

    public void StopLoading() => PlayerInventoryController.StopProcesses();

    public string GetMagAmmoCountByLevel()
    {
        int skill = Mathf.Max(
        [
            _player.Profile.MagDrillsMastering,
            _player.Profile.CheckedMagazineSkillLevel(_magazine.Id),
            _magazine.CheckOverride
        ]);
        //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine) // Is mag examined?

        return _magazine.GetAmmoCountByLevel(_magazine.Count, _magazine.MaxCount, skill, "#ffffff", true, false, "<color={2}>{0}</color>/{1}");
    }

    private async Task SetPlayerStateAsync(bool startAnim)
    {
        if (startAnim)
        {
            _player.TrySaveLastItemInHands();
            _player.SetEmptyHands(null);
            _player.MovementContext.ChangeSpeedLimit(ContinuousLoadAmmo.SpeedLimit.Value, ESpeedLimit.BarbedWire);
        }
        else
        {
            // Timing delay
            await Task.Delay(800);

            // Check for active MultiSelect load/unload
            if (MultiSelectInterop.IsMultiSelectLoadSerializerActive) return;

            if (_player.HandsIsEmpty)
            {
                _player.TrySetLastEquippedWeapon();
            }
            _player.MovementContext.RemoveStateSpeedLimit(ESpeedLimit.BarbedWire);
        }
        _player.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);
    }

    private void ResetLoading()
    {
        _isReachable = true;
        _magazine = null;
    }

    /// <summary>
    /// Check if item is reachable, recursively
    /// </summary>
    private bool IsAtReachablePlace(Item item)
    {
        if (item.CurrentAddress == null) return false;

        var reachableItems = new List<Item>();
        GetReachableItems(reachableItems);
        return reachableItems.Contains(item) && PlayerInventoryController.Examined(item);
    }

    private bool IsAtReachablePlace(Item item, Item item2)
    {
        if (item.CurrentAddress == null || item2.CurrentAddress == null) return false;

        var reachableItems = new List<Item>();
        GetReachableItems(reachableItems);
        return reachableItems.Contains(item) &&
               PlayerInventoryController.Examined(item) &&
               reachableItems.Contains(item2) &&
               PlayerInventoryController.Examined(item2);
    }

    private void GetReachableItems<TItem>(
        List<TItem> preAllocatedList,
        Predicate<TItem> predicate = null)
        where TItem : Item
    {
        GetAcceptableItemsNonAlloc(
            ReachableSlots,
            preAllocatedList,
            predicate,
            (item) =>
                item is not SearchableItemItemClass searchable ||
                PlayerInventoryController.SearchController.IsSearched(searchable) /* Only searched containers */
        );
    }

    /// <summary>
    /// Get acceptable items recursively
    /// </summary>
    private void GetAcceptableItemsNonAlloc<TItem>(
        EquipmentSlot[] equipmentSlots,
        List<TItem> preAllocatedList,
        Predicate<TItem> predicate = null,
        Predicate<GClass3248> goDeeperPredicate = null)
        where TItem : Item
    {
        InventoryEquipment equipment = PlayerInventoryController.Inventory.Equipment;
        foreach (EquipmentSlot equipmentSlot in equipmentSlots)
        {
            if (equipment.GetSlot(equipmentSlot).ContainedItem is not GClass3248 containedItem || (goDeeperPredicate != null && !goDeeperPredicate(containedItem))) continue;

            foreach (var container in containedItem.Containers)
            {
                foreach (Item obj1 in container.Items)
                {
                    if (obj1 is GClass3248 obj3 && (goDeeperPredicate == null || goDeeperPredicate(obj3)))
                    {
                        GetAllItemsOfContainer(obj3, preAllocatedList, predicate, goDeeperPredicate);
                    }
                    if (obj1 is TItem obj2 && (predicate == null || predicate(obj2)))
                    {
                        preAllocatedList.Add(obj2);
                    }
                }
            }
        }
    }

    private static void GetAllItemsOfContainer<TItem>(
        GClass3248 containedItem,
        List<TItem> preAllocatedList,
        Predicate<TItem> predicate = null,
        Predicate<GClass3248> goDeeperPredicate = null)
        where TItem : Item
    {
        foreach (var container in containedItem.Containers)
        {
            foreach (Item obj1 in container.Items)
            {
                if (obj1 is GClass3248 obj3 && (goDeeperPredicate == null || goDeeperPredicate(obj3)))
                {
                    GetAllItemsOfContainer(obj3, preAllocatedList, predicate, goDeeperPredicate);
                }
                if (obj1 is TItem obj2 && (predicate == null || predicate(obj2)))
                {
                    preAllocatedList.Add(obj2);
                }
            }
        }
    }

    private void StopLoadingOnHandsChange(AbstractHandsController oldHands, AbstractHandsController newHands)
    {
        if (!IsActive) return;

        if (newHands is not (null or EmptyHandsController))
        {
            StopLoading();
        }
    }

    private void Destroy(IPlayer player)
    {
        PlayerInventoryController.StopProcesses();
        if (PlayerInventoryController != null)
        {
            PlayerInventoryController.ActiveEventAdded -= LoadingStart;
        }
        if (_player != null)
        {
            InventoryScreenClosePatch.OnInventoryClose -= LoadingOutsideInventory;
            UnloadMagazineStartPatch.OnLoadingEnd -= LoadingEnd;
            LoadMagazineStartPatch.OnLoadingEnd -= LoadingEnd;
            _player.OnHandsControllerChanged -= StopLoadingOnHandsChange;
            _player.OnIPlayerDeadOrUnspawn -= Destroy;
        }
        OnStartLoading = null;
        OnCloseInventoryLoading = null;
        OnEndLoading = null;
    }

    private static EquipmentSlot[] ReachableSlots => ContinuousLoadAmmo.ReachableOnly.Value ? _reachableOnly : _reachableAll;

    private static readonly EquipmentSlot[] _reachableOnly =
    [
        EquipmentSlot.Pockets,
        EquipmentSlot.TacticalVest,
        EquipmentSlot.SecuredContainer,
        EquipmentSlot.ArmBand
    ];

    private static readonly EquipmentSlot[] _reachableAll =
    [
        EquipmentSlot.Pockets,
        EquipmentSlot.TacticalVest,
        EquipmentSlot.ArmorVest,
        EquipmentSlot.Backpack,
        EquipmentSlot.SecuredContainer,
        EquipmentSlot.ArmBand
    ];
}
