using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
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
    private readonly MagazineBuildPresetClass.Class1023 _missingItemsError;
    private readonly MagPresetCancelled _magPresetCancelled;
    private MagazineItemClass _magazine;
    private bool _isReachable = true;

    public event Action<float, int, int> OnStartLoading;
    public event Action<Item> OnCloseInventoryLoading;
    public event Action OnEndLoading;
    public event Action OnPlayerDestroy;

    public bool IsActive => PlayerInventoryController.Interface19_0 != null || ApplyMagPresetPatch.PresetLoaderIsActive;
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
        ApplyMagPresetPatch.OnApplyMagPreset += LoadingMagPreset;
        /*
         _player.OnInventoryOpened += LoadingOutsideInventory; // Why does BSG CALL THIS _TWICE_
        _player.InventoryController.ActiveEventsChanged += LoadingEnd; // Can't use since always CommandStatus.Begin, but why
        */
        _player.OnHandsControllerChanged += StopLoadingOnHandsChange;
        _player.OnIPlayerDeadOrUnspawn += OnDestroy;

        _missingItemsError = new MagazineBuildPresetClass.Class1023(null);
        _magPresetCancelled = new MagPresetCancelled();
    }

    public bool CanLoadOutsideInventory()
    {
        return !PlayerInventoryController.HasAnyHandsActionNonLinq() &&
               _isReachable;
    }

    public bool IsQuickLoadAvailable(out List<AmmoItemClass> reachableAmmo, out MagazineItemClass foundMagazine)
    {
        reachableAmmo = null;
        foundMagazine = null;
        return GetReachableAmmoForCurrentWeapon(out reachableAmmo) && GetMagazineForAmmo(reachableAmmo[0], out foundMagazine);
    }

    public void TryQuickLoadAmmo()
    {
        if (!IsQuickLoadAvailable(out List<AmmoItemClass> reachableAmmo, out MagazineItemClass foundMagazine))
        {
            CommonUtils.DisplayNotification(
                "No ammo or magazines found for the current weapon",
                iconType: ENotificationIconType.Alert
            );
            return;
        }

        AmmoItemClass chosenAmmo = null;
        if (!ContinuousLoadAmmo.PrioritizeHighestPenetration.Value)
        {
            MagazineItemClass currentMagazine = _player.LastEquippedWeaponOrKnifeItem.GetCurrentMagazine();
            if (currentMagazine != null)
            {
                foreach (var currAmmo in reachableAmmo)
                {
                    if (currentMagazine.FirstRealAmmo() is not AmmoItemClass ammoInsideMag ||
                        ammoInsideMag.TemplateId != currAmmo.TemplateId)
                        continue;

                    // Magazine ammo matched with current reachable ammo
                    chosenAmmo = currAmmo;
                    break;
                }
            }
        }
        // PrioritizeHighestPenetration is false or if no ammo matched from magazine's first ammo, choose first reachable ammo available
        chosenAmmo ??= reachableAmmo[0];
        LoadMagazine(chosenAmmo, foundMagazine);

        CommonUtils.DisplayNotification(
            $"Loading {chosenAmmo.LocalizedShortName()}",
            iconType: ENotificationIconType.Note
        );
    }

    public void LoadMagazine(AmmoItemClass ammo, MagazineItemClass magazine)
    {
        //Plugin.LogSource.LogDebug($"Mag {magazine.LocalizedShortName()} ({magazine.Count}); Ammo {ammo.LocalizedShortName()} ({ammo.StackObjectsCount})");
        int loadCount = Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);
        _ = PlayerInventoryController.LoadMagazine(ammo, magazine, loadCount, false);
    }

    public async Task<IResult> LoadMagazineAsync(AmmoItemClass ammo, MagazineItemClass magazine, CancellationToken token, int? ammoCount = null)
    {
        //Plugin.LogSource.LogDebug($"Mag {magazine.LocalizedShortName()} ({magazine.Count}); Ammo {ammo.LocalizedShortName()} ({ammo.StackObjectsCount})");
        int loadCount = ammoCount ?? Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);
        while (PlayerInventoryController.Locked)
        {
            token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
        return await PlayerInventoryController.LoadMagazine(ammo, magazine, loadCount, false);
    }

    /// <summary>
    /// Find reachable magazine for ammo
    /// </summary>
    /// <param name="ammo">Ammo that should be compatible with the magazine</param>
    public bool GetMagazineForAmmo(AmmoItemClass ammo, out MagazineItemClass foundMagazine)
    {
        foundMagazine = null;
        var foundMagazines = new List<MagazineItemClass>();
        if (ContinuousLoadAmmo.ReachableOnly.Value)
        {
            // Only get top level container's items for quick load, non-recursive
            PlayerInventoryController.GetAcceptableItemsNonAlloc(
                ReachableSlots,
                foundMagazines,
                (mag) =>
                    PlayerInventoryController.Examined(mag) &&
                    mag.Count != mag.MaxCount &&
                    mag.CheckCompatibility(ammo),
                ContainerIsSearched
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
    /// Find reachable ammo for the current weapon. Used by quick load
    /// </summary>
    /// <param name="reachableAmmo">One of each ammo type found then sorted by Penetration Power descending</param>
    public bool GetReachableAmmoForCurrentWeapon(out List<AmmoItemClass> reachableAmmo)
    {
        reachableAmmo = [];
        if (_player.LastEquippedWeaponOrKnifeItem is not Weapon weapon) return false;

        string weaponCaliber = weapon.AmmoCaliber;
        if (ContinuousLoadAmmo.ReachableOnly.Value)
        {
            // Only get top level container's items for quick load, non-recursive
            PlayerInventoryController.GetAcceptableItemsNonAlloc(
                ReachableSlots,
                reachableAmmo,
                (ammo) =>
                    PlayerInventoryController.Examined(ammo) &&
                    ammo.Caliber == weaponCaliber &&
                    ammo.Parent.Container.ParentItem is not MagazineItemClass /* Do not pull from ammo inside mags */,
                ContainerIsSearched
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

    /// <summary>
    /// Find ammo for <paramref name="magazine"/>. Used by mag presets
    /// </summary>
    /// <param name="magazine">Magazine to be checked compatible with</param>
    public bool GetAllAmmoForMagazine(out List<AmmoItemClass> allAmmo, MagazineItemClass magazine)
    {
        allAmmo = [];
        PlayerInventoryController.Inventory.Equipment.GetAcceptableItemsNonAlloc(
            _reachableAll,
            allAmmo,
            (ammo) =>
                PlayerInventoryController.Examined(ammo) &&
                ammo.Parent.Container.ParentItem is not MagazineItemClass && /* Do not pull from ammo inside mags */
                magazine.CheckCompatibility(ammo),
            ContainerIsSearched
        );
        if (allAmmo.Count <= 0) return false;

        // Sort stack count ascending
        allAmmo.Sort((a, b) => a.StackObjectsCount.CompareTo(b.StackObjectsCount));
        return true;
    }

    public void StopLoading()
    {
        ApplyMagPresetPatch.CancelMagPresetLoading();
        PlayerInventoryController.StopProcesses();
    }

    public string GetMagAmmoCountByLevel()
    {
        int skill = Mathf.Max(
            _player.Profile.MagDrillsMastering,
            _player.Profile.CheckedMagazineSkillLevel(_magazine.Id),
            _magazine.CheckOverride
        );
        //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine) // Is mag checked?

        return _magazine.GetAmmoCountByLevel(_magazine.Count, _magazine.MaxCount, skill, "#ffffff", true, false, "<color={2}>{0}</color>/{1}");
    }

    public void Dispose()
    {
        PlayerInventoryController?.StopProcesses();
        if (PlayerInventoryController != null)
        {
            PlayerInventoryController.ActiveEventAdded -= LoadingStart;
        }
        if (_player != null)
        {
            InventoryScreenClosePatch.OnInventoryClose -= LoadingOutsideInventory;
            UnloadMagazineStartPatch.OnLoadingEnd -= LoadingEnd;
            LoadMagazineStartPatch.OnLoadingEnd -= LoadingEnd;
            ApplyMagPresetPatch.OnApplyMagPreset -= LoadingMagPreset;
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
                if (loadEvent.TargetItem is not MagazineItemClass loadMagazine ||
                    loadEvent.Item is not AmmoItemClass ammo)
                    return;

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

// ReSharper disable once AsyncVoidMethod
#pragma warning disable VSTHRD100
    private async void LoadingMagPreset(
        MagazineBuildPresetClass preset,
        IReadOnlyCollection<MagazineItemClass> magazines,
        TaskCompletionSource<GStruct155> taskCompletion,
        CancellationToken token
    )
#pragma warning restore VSTHRD100
    {
        // TODO: Stop preset loading outside inventory if unreachable
        _missingItemsError.String_1 = MagazineBuildPresetClass.Class1023.String_0.Localized();

        if (!GetAllAmmoForMagazine(out var availableAmmo, ((List<MagazineItemClass>)magazines)[0]))
        {
            _missingItemsError.String_1 += $" No available ammo found for magazine";
            taskCompletion.TrySetResult(_missingItemsError);
            return;
        }

        try
        {
            foreach (var magazine in magazines)
            {
                CommonUtils.DisplayNotification(
                    $"Loading {preset.Name} ({MagazineBuildClass.GetCaliberName(preset.Caliber)})",
                    iconType: ENotificationIconType.Note
                );

                // Bottom
                var bottomCount = 0;
                foreach (var bottom in preset.Bottom)
                {
                    token.ThrowIfCancellationRequested();

                    if (bottom == null) continue;

                    bottomCount = bottom.Count;
                    if (magazine.Count >= bottom.Count) continue;

                    var toLoad = Mathf.Min(bottom.Count, bottom.Count - magazine.Count);
                    await TryLoadPresetStepAsync(availableAmmo, magazine, bottom, toLoad, taskCompletion, token);
                }

                // Loop
                // Track toSkip to resume loading from current count
                var toSkip = magazine.Count - bottomCount;
                var freeLoopSpace = magazine.MaxCount - magazine.Count;
                var topCount = 0;
                foreach (var top in preset.Top)
                {
                    if (top != null) topCount += top.Count;
                }
                freeLoopSpace -= topCount;

                while (freeLoopSpace > 0)
                {
                    token.ThrowIfCancellationRequested();

                    foreach (var loop in preset.Loop)
                    {
                        token.ThrowIfCancellationRequested();
                        if (loop == null) continue;

                        var toLoad = (int)loop.Count;
                        if (toSkip > 0) // Resume loading from current count
                        {
                            // Should skip entire group?
                            if (toSkip >= toLoad)
                            {
                                toSkip -= toLoad;
                                continue;
                            }

                            // Load remaining
                            toLoad -= toSkip;
                            toSkip = 0;
                        }
                        toLoad = Mathf.Min(toLoad, freeLoopSpace);
                        await TryLoadPresetStepAsync(availableAmmo, magazine, loop, toLoad, taskCompletion, token);
                        freeLoopSpace -= toLoad;
                    }
                }

                // Top
                foreach (var top in preset.Top)
                {
                    token.ThrowIfCancellationRequested();

                    if (top == null) continue;

                    var toLoad = Mathf.Min(top.Count, magazine.MaxCount - magazine.Count);
                    await TryLoadPresetStepAsync(availableAmmo, magazine, top, toLoad, taskCompletion, token);
                }
            }
            taskCompletion.TrySetResult(default);
        }
        catch (OperationCanceledException)
        {
            // NotificationManagerClass.DisplayWarningNotification(_magPresetCancelled.ToString());
            taskCompletion.TrySetResult(_magPresetCancelled);
        }
        catch (Exception ex)
        {
            taskCompletion.TrySetException(ex);
        }
    }

    private async Task TryLoadPresetStepAsync(
        List<AmmoItemClass> availableAmmo,
        MagazineItemClass magazine,
        MagazineBuildPresetClass.GClass2578 preset,
        int toLoad,
        TaskCompletionSource<GStruct155> taskCompletion,
        CancellationToken token
    )
    {
        var matchingAmmo = GetMatchingAmmo(availableAmmo, preset.TemplateId, toLoad);
        if (matchingAmmo == null)
        {
            _missingItemsError.String_1 += $" {preset.TemplateId.LocalizedShortName()}, Count: {toLoad}";
            NotificationManagerClass.DisplayWarningNotification(_missingItemsError.String_1);
            taskCompletion.TrySetResult(_missingItemsError);
            return;
        }
        await LoadMagazineAsync(matchingAmmo, magazine, token, toLoad);
    }

    private static AmmoItemClass GetMatchingAmmo(List<AmmoItemClass> ammo, MongoID templateId, int count)
    {
        foreach (var ammoItem in ammo)
        {
            if (ammoItem.TemplateId != templateId ||
                ammoItem.StackObjectsCount < count)
            {
                continue;
            }

            return ammoItem;
        }

        return null;
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
            if (MultiSelectInterop.IsMultiSelectLoadSerializerActive || ApplyMagPresetPatch.PresetLoaderIsActive) return;

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
        Predicate<TItem> predicate = null
    ) where TItem : Item
    {
        PlayerInventoryController.Inventory.Equipment.GetAcceptableItemsNonAlloc(
            ReachableSlots,
            preAllocatedList,
            predicate,
            ContainerIsSearched
        );
    }

    private bool ContainerIsSearched(GClass3248 container)
    {
        return container is not SearchableItemItemClass searchable ||
               PlayerInventoryController.SearchController.IsSearched(searchable); /* Only searched containers */
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

    private static EquipmentSlot[] ReachableSlots =>
        ContinuousLoadAmmo.ReachableOnly.Value
            ? _reachableOnly
            : _reachableAll;

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
