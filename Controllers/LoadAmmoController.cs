using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Patches;
using ContinuousLoadAmmo.Utils;
using EFT;
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
        List<MagazineItemClass> foundMagazines = [];
        PlayerInventoryController.GetAcceptableItemsNonAlloc(
            ReachableSlots,
            foundMagazines,
            (mag) => PlayerInventoryController.Examined(mag) && mag.Count != mag.MaxCount && mag.CheckCompatibility(ammo)
        );
        if (foundMagazines.Count > 0)
        {
            // Sort by almost full
            foundMagazines.Sort((a, b) =>
                (a.MaxCount - a.Count).CompareTo(b.MaxCount - b.Count)
            );
            foundMagazine = foundMagazines[0];
            return true;
        }
        foundMagazine = null;
        return false;
    }

    /// <summary>
    /// Find reachable ammo for the current weapon.
    /// </summary>
    /// <param name="reachableAmmo">One of each ammo type found then sorted by Penetration Power descending</param>
    /// <returns></returns>
    public bool GetAmmoItemsFromEquipment(out List<AmmoItemClass> reachableAmmo)
    {
        reachableAmmo = [];
        if (_player.LastEquippedWeaponOrKnifeItem is Weapon weapon)
        {
            string weaponCaliber = weapon.AmmoCaliber;
            var items = PlayerInventoryController.Inventory.GetItemsInSlots(ReachableSlots); // linq
            foreach (var item in items)
            {
                if (item is AmmoItemClass ammo &&
                    PlayerInventoryController.Examined(ammo) &&
                    ammo.Parent.Container.ParentItem is not MagazineItemClass && // Do not pull from ammo inside mags
                    ammo.Caliber == weaponCaliber)
                {
                    reachableAmmo.Add(ammo);
                }
            }
        }
        if (reachableAmmo.Count < 1) return false;

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
                _magazine = (MagazineItemClass)loadEvent.TargetItem;
                _isReachable = IsAtReachablePlace(_magazine) && IsAtReachablePlace((AmmoItemClass)loadEvent.Item);
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
            if (MultiSelectInterop.LoadUnloadSerializer != null) return;

            if (!_player.IsWeaponOrKnifeInHands)
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
    /// Base EFT code with modifications
    /// Only used internally with ammo and magazine outside the stash, so fewer checks
    /// </summary>
    private bool IsAtReachablePlace(Item item)
    {
        if (item.CurrentAddress == null) return false;

        return PlayerInventoryController.Inventory.GetItemsInSlots(ReachableSlots).Contains(item) && PlayerInventoryController.Examined(item); // linq
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
