using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;
using static EFT.Player;
using static EFT.Player.PlayerInventoryController;

namespace ContinuousLoadAmmo.Components;

public class LoadAmmo : MonoBehaviour
{
    internal static LoadAmmo Inst;

    private Player _player;
    private PlayerInventoryController _playerInventoryController;
    private MagazineItemClass _magazine;
    private bool _isReachable;

    public bool IsActive => _playerInventoryController.Interface19_0 != null;
    public PlayerInventoryController PlayerInventoryController => _playerInventoryController;

    public event Action<float, int, int> OnStartLoading;
    public event Action<Item> OnCloseInventory;
    public event Action OnEndLoading;
    public event Action OnDestroyComponent;

    public void Awake()
    {
        _player = gameObject.GetComponent<Player>();
        if (Inst != null)
        {
            Destroy(this);
            return;
        }
        if (_player.InventoryController is not PlayerInventoryController playerInvCont)
        {
            Plugin.LogSource.LogError("LoadAmmo::Awake Unable to properly initialize ContinuousLoadAmmo");
            Destroy(this);
            return;
        }

        _playerInventoryController = playerInvCont;
        _playerInventoryController.SetNextProcessLocked(false);
        _player.OnHandsControllerChanged += StopLoadingOnHandsChange;
        LoadAmmoSelector.Create(gameObject, this);
        Inst = this;
    }

    public bool CanLoadOutsideInventory()
    {
        return !_player.IsInventoryOpened &&
               !_playerInventoryController.HasAnyHandsAction();
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
        if (!Plugin.PrioritizeHighestPenetration.Value)
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
        _ = _playerInventoryController.LoadMagazine(ammo, magazine, loadCount, false);
    }

    /// <summary>
    /// Find reachable magazine for ammo
    /// </summary>
    /// <param name="ammo">Ammo that should be compatible with the magazine</param>
    /// <returns></returns>
    public bool GetMagazineForAmmo(AmmoItemClass ammo, out MagazineItemClass foundMagazine)
    {
        List<MagazineItemClass> foundMagazines = [];
        _playerInventoryController.GetAcceptableItemsNonAlloc(
            ReachableSlots,
            foundMagazines,
            (mag) => _playerInventoryController.Examined(mag) && mag.Count != mag.MaxCount && mag.CheckCompatibility(ammo)
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
            var items = _playerInventoryController.Inventory.GetItemsInSlots(ReachableSlots); // linq
            foreach (var item in items)
            {
                if (item is AmmoItemClass ammo &&
                    _playerInventoryController.Examined(ammo) &&
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

    public void LoadingStart(LoadingEventType eventType, Class1204 loadingClass, Class1207 unloadingClass)
    {
        switch (eventType)
        {
            case LoadingEventType.Load:
                _magazine = loadingClass.MagazineItemClass;
                _isReachable = IsAtReachablePlace(_magazine) && IsAtReachablePlace(loadingClass.AmmoItemClass);
                OnStartLoading?.Invoke(loadingClass.Float_0, loadingClass.Int_0, 0);
                break;
            case LoadingEventType.Unload:
                _magazine = unloadingClass.MagazineItemClass;
                _isReachable = IsAtReachablePlace(_magazine);
                OnStartLoading?.Invoke(unloadingClass.Float_0, unloadingClass.Int_1, unloadingClass.Int_0 - unloadingClass.Int_1);
                break;
            case LoadingEventType.None:
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
        }

        // Started loading from outside the inventory
        if (!_player.IsInventoryOpened)
        {
            LoadingOutsideInventory();
        }
    }

    public void LoadingOutsideInventory()
    {
        if (IsActive && _isReachable && !_playerInventoryController.HasAnyHandsAction())
        {
            _ = SetPlayerStateAsync(true);
            OnCloseInventory?.Invoke(_magazine);
            return;
        }

        StopLoading();
    }

    public void LoadingEnd()
    {
        _ = SetPlayerStateAsync(false);
        ResetLoading();
        OnEndLoading?.Invoke();
    }

    private async Task SetPlayerStateAsync(bool startAnim)
    {
        if (startAnim)
        {
            _player.TrySaveLastItemInHands();
            _player.SetEmptyHands(null);
            _player.MovementContext.ChangeSpeedLimit(Plugin.SpeedLimit.Value, ESpeedLimit.BarbedWire);
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
        _isReachable = false;
        _magazine = null;
    }

    protected void OnDestroy()
    {
        _player.OnHandsControllerChanged -= StopLoadingOnHandsChange;
        OnDestroyComponent?.Invoke();
        OnStartLoading = null;
        OnCloseInventory = null;
        OnEndLoading = null;
        OnDestroyComponent = null;
        if (Inst == this)
        {
            Inst = null;
        }
    }

    /// <summary>
    /// Base EFT code with modifications
    /// Only used internally with ammo and magazine outside the stash, so fewer checks
    /// </summary>
    private bool IsAtReachablePlace(Item item)
    {
        if (item.CurrentAddress == null) return false;

        return _playerInventoryController.Inventory.GetItemsInSlots(ReachableSlots).Contains(item) && _playerInventoryController.Examined(item);
    }

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

    private void StopLoadingOnHandsChange(AbstractHandsController oldHands, AbstractHandsController newHands)
    {
        if (!IsActive) return;

        if (newHands is not (null or EmptyHandsController))
        {
            StopLoading();
        }
    }

    public void StopLoading() => _playerInventoryController.StopProcesses();

    private static EquipmentSlot[] ReachableSlots => Plugin.ReachableOnly.Value ? _reachableOnly : _reachableAll;

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

    public enum LoadingEventType
    {
        None,
        Load,
        Unload
    }
}
