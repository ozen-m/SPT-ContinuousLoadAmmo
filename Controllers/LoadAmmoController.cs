using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Models;
using ContinuousLoadAmmo.Patches;
using ContinuousLoadAmmo.Utils;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using UnityEngine;
using static EFT.Player;

namespace ContinuousLoadAmmo.Controllers;

public class LoadAmmoController : IDisposable
{
    private readonly Player _player;
    private readonly MagazinePresetLoader _magazinePresetLoader;
    private MagazineItemClass _magazine;
    private bool _isReachable = true;

    public event Action<float, int, int> OnStartLoading;
    public event Action<Item> OnCloseInventoryLoading;
    public event Action OnEndLoading;
    public event Action OnPlayerDestroy;

    public bool IsActive => PlayerInventoryController.Interface19_0 is not null || _magazinePresetLoader.PresetLoaderIsActive;
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
        /*
         _player.OnInventoryOpened += LoadingOutsideInventory; // Why does BSG CALL THIS _TWICE_
        _player.InventoryController.ActiveEventsChanged += LoadingEnd; // Can't use since always CommandStatus.Begin, but why
        */
        _player.OnHandsControllerChanged += StopLoadingOnHandsChange;
        _player.OnIPlayerDeadOrUnspawn += OnDestroy;

        _magazinePresetLoader = new MagazinePresetLoader(this);
    }

    public bool CanLoadOutsideInventory()
    {
        return !PlayerInventoryController.HasAnyHandsActionNonLinq() && _isReachable;
    }

    public bool IsQuickLoadAvailable(out List<AmmoItemClass> reachableAmmo, out MagazineItemClass foundMagazine, string caliber = null)
    {
        reachableAmmo = null;
        foundMagazine = null;
        return GetReachableAmmoOfCaliber(out reachableAmmo, caliber) && GetMagazineForAmmo(reachableAmmo[0], out foundMagazine);
    }

    public void TryQuickLoadAmmo()
    {
        if (!IsQuickLoadAvailable(out var reachableAmmo, out var foundMagazine))
        {
            CommonUtils.DisplayNotification(
                "No reachable ammo or magazines found for the current weapon",
                ENotificationIconType.Alert,
                true
            );
            return;
        }

        AmmoItemClass chosenAmmo = null;
        if (ContinuousLoadAmmo.QuickLoadMode.Value == QuickLoadMode.LastBulletMagazine)
        {
            var currentMagazine = _player.LastEquippedWeaponOrKnifeItem.GetCurrentMagazine();
            if (currentMagazine is not null)
            {
                foreach (var currAmmo in reachableAmmo)
                {
                    if (currentMagazine.FirstRealAmmo() is not AmmoItemClass ammoInsideMag
                        || ammoInsideMag.TemplateId != currAmmo.TemplateId)
                    {
                        continue;
                    }

                    // Magazine ammo matched with current reachable ammo
                    chosenAmmo = currAmmo;
                    break;
                }
            }
        }
        // PrioritizeHighestPenetration is false or if no ammo matched from magazine's first ammo, choose first reachable ammo available
        chosenAmmo ??= reachableAmmo[0];
        LoadMagazine(chosenAmmo, foundMagazine);

        CommonUtils.DisplayNotification($"Loading {chosenAmmo.LocalizedShortName()}", ENotificationIconType.Note);
    }

    public void TryQuickLoadLastPreset()
    {
        if (_magazinePresetLoader.IsPresetAvailableForCurrentWeapon(out var preset))
        {
            _magazinePresetLoader.QuickLoadMagPreset(preset);
            return;
        }

        // Fallback, no preset selected yet through context menu or preset not compatible with weapon
        TryQuickLoadAmmo();
    }

    public void LoadMagazine(AmmoItemClass ammo, MagazineItemClass magazine)
    {
        var loadCount = Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);
        _ = PlayerInventoryController.LoadMagazine(ammo, magazine, loadCount, false);
    }

    public async Task LoadMagazineAsync(AmmoItemClass ammo, MagazineItemClass magazine, CancellationToken token, int? ammoCount = null)
    {
        var loadCount = ammoCount ?? Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);
        while (PlayerInventoryController.Locked)
        {
            token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
        await PlayerInventoryController.LoadMagazine(ammo, magazine, loadCount, false);
    }

    private readonly List<MagazineItemClass> _reachableMagazinesScratch = [];

    /// <summary>
    /// Find reachable magazine for ammo
    /// </summary>
    /// <param name="ammo">Ammo that should be compatible with the magazine</param>
    public bool GetMagazineForAmmo(AmmoItemClass ammo, out MagazineItemClass foundMagazine)
    {
        foundMagazine = null;
        _reachableMagazinesScratch.Clear();
        if (ContinuousLoadAmmo.ReachableOnly.Value)
        {
            // Only get top level container's items for quick load, non-recursive
            PlayerInventoryController.GetAcceptableItemsNonAlloc(
                ReachableSlots,
                _reachableMagazinesScratch,
                (mag) => PlayerInventoryController.Examined(mag) && mag.Count != mag.MaxCount && mag.CheckCompatibility(ammo),
                ContainerPredicate
            );
        }
        else
        {
            // Can be recursive
            GetReachableItems(
                _reachableMagazinesScratch,
                (mag) => PlayerInventoryController.Examined(mag) && mag.Count != mag.MaxCount && mag.CheckCompatibility(ammo)
            );
        }
        if (_reachableMagazinesScratch.Count <= 0) return false;

        // Some magazines can have multiple calibers
        _reachableMagazinesScratch.RemoveAll(mag => mag.HasAmmoWithDifferentCaliber(ammo));

        // Sort by almost full
        _reachableMagazinesScratch.Sort((a, b) => (a.MaxCount - a.Count).CompareTo(b.MaxCount - b.Count));

        // Mag with most amount
        foundMagazine = _reachableMagazinesScratch[0];
        return true;
    }

    private static readonly List<AmmoItemClass> _reachableAmmoScratch = [];

    /// <summary>
    /// Find reachable ammo of specified caliber. Used by quick load
    /// </summary>
    /// <param name="reachableAmmo">One of each ammo type found then sorted by Penetration Power descending</param>
    /// <param name="ammoCaliber">Optional, fallbacks to current weapon's caliber</param>
    public bool GetReachableAmmoOfCaliber(out List<AmmoItemClass> reachableAmmo, string ammoCaliber = null)
    {
        _reachableAmmoScratch.Clear();
        reachableAmmo = _reachableAmmoScratch;

        ammoCaliber ??= GetCurrentWeaponCaliber();
        if (ammoCaliber.IsNullOrEmpty())
        {
            return false;
        }

        if (ContinuousLoadAmmo.ReachableOnly.Value)
        {
            // Only get top level container's items for quick load, non-recursive
            PlayerInventoryController.GetAcceptableItemsNonAlloc(
                ReachableSlots,
                reachableAmmo,
                (ammo) => PlayerInventoryController.Examined(ammo) && ammo.Caliber == ammoCaliber,
                ContainerPredicate
            );
        }
        else
        {
            // Can be recursive
            GetReachableItems(reachableAmmo, (ammo) => PlayerInventoryController.Examined(ammo) && ammo.Caliber == ammoCaliber);
        }
        if (reachableAmmo.Count <= 0) return false;

        // Sort penetration power highest to lowest, then stack count ascending
        reachableAmmo.Sort((a, b) =>
            {
                var result = b.PenetrationPower.CompareTo(a.PenetrationPower);
                if (result == 0)
                {
                    result = a.StackObjectsCount.CompareTo(b.StackObjectsCount);
                }
                return result;
            }
        );

        return true;
    }

    /// <summary>
    /// Find ammo for <paramref name="magazine"/>. Used by loading mag presets in the inventory screen
    /// </summary>
    /// <param name="magazine">Magazine to be checked compatible with</param>
    public bool GetAllAmmoForMagazine(out List<AmmoItemClass> allAmmo, MagazineItemClass magazine)
    {
        allAmmo = [];
        PlayerInventoryController.Inventory.Equipment.GetAcceptableItemsNonAlloc(
            _reachableAll,
            allAmmo,
            (ammo) => PlayerInventoryController.Examined(ammo) && magazine.CheckCompatibility(ammo),
            ContainerPredicate
        );
        if (allAmmo.Count <= 0) return false;

        // Sort penetration power highest to lowest, then stack count ascending
        allAmmo.Sort((a, b) =>
            {
                var result = b.PenetrationPower.CompareTo(a.PenetrationPower);
                if (result == 0)
                {
                    result = a.StackObjectsCount.CompareTo(b.StackObjectsCount);
                }
                return result;
            }
        );
        return true;
    }

    public void StopLoading()
    {
        _magazinePresetLoader.CancelMagPresetLoading();
        PlayerInventoryController.StopProcesses();
    }

    public string GetMagAmmoCountByLevel()
    {
        if (_magazine is null)
        {
            ContinuousLoadAmmo.LogSource.LogError("Magazine is null while trying to get ammo count");
            return "MAG NULL";
        }

        var skill = Mathf.Max(
            _player.Profile.MagDrillsMastering,
            Mathf.Max(_player.Profile.CheckedMagazineSkillLevel(_magazine.Id), _magazine.CheckOverride)
        );
        // bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine) // Is mag checked?

        return _magazine.GetAmmoCountByLevel(
            _magazine.Count,
            _magazine.MaxCount,
            skill,
            "#ffffff",
            true,
            false,
            "<color={2}>{0}</color>/{1}"
        );
    }

    public string GetCurrentWeaponCaliber()
    {
        if (_player.HandsController is FirearmController fc)
        {
            return fc.Weapon.GetWeaponCaliber();
        }

        return string.Empty;
    }

    public void Dispose()
    {
        _magazinePresetLoader.Dispose();
        if (PlayerInventoryController is not null)
        {
            PlayerInventoryController.StopProcesses();
            PlayerInventoryController.ActiveEventAdded -= LoadingStart;
        }
        if (_player != null)
        {
            InventoryScreenClosePatch.OnInventoryClose -= LoadingOutsideInventory;
            UnloadMagazineStartPatch.OnLoadingEnd -= LoadingEnd;
            LoadMagazineStartPatch.OnLoadingEnd -= LoadingEnd;
            _player.OnHandsControllerChanged -= StopLoadingOnHandsChange;
            _player.OnIPlayerDeadOrUnspawn -= OnDestroy;
        }
        OnPlayerDestroy?.Invoke();
        OnStartLoading = null;
        OnCloseInventoryLoading = null;
        OnEndLoading = null;
        OnPlayerDestroy = null;
    }

    private void LoadingStart(GEventArgs1 eventArgs)
    {
        switch (eventArgs)
        {
            case GEventArgs7 loadEvent:
                if (loadEvent.TargetItem is not MagazineItemClass loadMagazine || loadEvent.Item is not AmmoItemClass ammo)
                {
                    return;
                }

                _magazine = loadMagazine;
                _isReachable = IsAtReachablePlace(_magazine, ammo);
                OnStartLoading?.Invoke(loadEvent.LoadTime, loadEvent.LoadCount, 0);
                break;
            case GEventArgs8 unloadEvent:
                _magazine = unloadEvent.FromItem;
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

    private async Task SetPlayerStateAsync(bool startAnim)
    {
        _player.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);

        if (startAnim)
        {
            _player.TrySaveLastItemInHands();
            _player.SetEmptyHands(null);
            _player.MovementContext.ChangeSpeedLimit(
                ContinuousLoadAmmo.SpeedLimit.Value * _player.MovementContext.MaxSpeed,
                ESpeedLimit.BarbedWire
            );
        }
        else
        {
            // Timing delay
            await Task.Delay(800);

            // Check for active MultiSelect load/unload
            if (MultiSelectInterop.MultiSelectLoadSerializerIsActive || _magazinePresetLoader.PresetLoaderIsActive)
            {
                return;
            }

            if (_player.HandsIsEmpty)
            {
                _player.TrySetLastEquippedWeapon();
            }
            _player.MovementContext.RemoveStateSpeedLimit(ESpeedLimit.BarbedWire);
        }
    }

    private void ResetLoading()
    {
        _isReachable = true;
        _magazine = null;
    }

    private readonly List<Item> _reachablePlaceItemScratch = [];

    /// <summary>
    /// Check if item is reachable, recursively
    /// </summary>
    private bool IsAtReachablePlace(Item item)
    {
        if (item.CurrentAddress is null) return false;

        _reachablePlaceItemScratch.Clear();
        GetReachableItems(_reachablePlaceItemScratch);
        return _reachablePlaceItemScratch.Contains(item) && PlayerInventoryController.Examined(item);
    }

    /// <summary>
    /// Check if item is reachable, recursively
    /// </summary>
    private bool IsAtReachablePlace(Item item, Item item2)
    {
        if (item.CurrentAddress is null || item2.CurrentAddress is null) return false;

        _reachablePlaceItemScratch.Clear();
        GetReachableItems(_reachablePlaceItemScratch);
        return _reachablePlaceItemScratch.Contains(item)
               && PlayerInventoryController.Examined(item)
               && _reachablePlaceItemScratch.Contains(item2)
               && PlayerInventoryController.Examined(item2);
    }

    private void GetReachableItems<TItem>(List<TItem> preAllocatedList, Predicate<TItem> predicate = null) where TItem : Item
    {
        PlayerInventoryController.Inventory.Equipment.GetAcceptableItemsNonAlloc(
            ReachableSlots,
            preAllocatedList,
            predicate,
            ContainerPredicate
        );
    }

    /// <summary>
    /// Do not pull ammo inside magazines/ammo boxes and only searched containers
    /// </summary>
    private bool ContainerPredicate(GClass3248 container)
    {
        return container is not IAmmoContainer
               && (container is not SearchableItemItemClass searchable
                   || PlayerInventoryController.SearchController.IsSearched(searchable));
    }

    private void StopLoadingOnHandsChange(AbstractHandsController oldHands, AbstractHandsController newHands)
    {
        if (!IsActive) return;

        if (newHands is not (null or EmptyHandsController))
        {
            StopLoading();
        }
    }

    private void OnDestroy(IPlayer player)
    {
        Dispose();
    }

    private static EquipmentSlot[] ReachableSlots => ContinuousLoadAmmo.ReachableOnly.Value ? _reachableOnly : _reachableAll;

    private static readonly EquipmentSlot[] _reachableOnly =
    [
        EquipmentSlot.Pockets,
        EquipmentSlot.TacticalVest,
        EquipmentSlot.ArmBand,
        EquipmentSlot.SecuredContainer,
    ];

    private static readonly EquipmentSlot[] _reachableAll =
    [
        EquipmentSlot.Pockets,
        EquipmentSlot.TacticalVest,
        EquipmentSlot.ArmBand,
        EquipmentSlot.SecuredContainer,
        EquipmentSlot.Backpack,
    ];
}
