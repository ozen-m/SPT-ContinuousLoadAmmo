using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIFixesInterop;
using UnityEngine;
using static EFT.Player;
using static EFT.Player.PlayerInventoryController;

namespace ContinuousLoadAmmo.Components
{
    public class LoadAmmo : MonoBehaviour
    {
        public static LoadAmmo Inst;

        protected Player player;
        protected MagazineItemClass magazine;
        protected LoadAmmoSelector ammoSelector;
        protected bool isReachable;

        public bool IsActive { get; protected set; }
        public bool AmmoSelectorActive => ammoSelector.IsShown;
        protected InventoryController InventoryController => player.InventoryController;

        public event Action<float, int, int> OnStartLoading;
        public event Action<Item> OnCloseInventory;
        public event Action OnEndLoading;
        public event Action OnDestroyComponent;

        protected void Awake()
        {
            player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player == null)
            {
                Plugin.LogSource.LogError("Unable to find MainPlayer, destroying component");
                Destroy(this);
            }
            if (!player.IsYourPlayer)
            {
                Plugin.LogSource.LogError("MainPlayer is not your player, destroying component");
                Destroy(this);
            }
            if (Inst == null)
            {
                Inst = this;
            }
            else
            {
                Destroy(this);
            }

            ((PlayerInventoryController)InventoryController).SetNextProcessLocked(false);
            ammoSelector = new LoadAmmoSelector();
        }

        protected void Update()
        {
            if (!Singleton<GameWorld>.Instantiated ||
                player == null ||
                player.IsInventoryOpened ||
                InventoryController.HasAnyHandsAction() ||
                IsActive ||
                ammoSelector.IsShown)
            {
                return;
            }

            if (Input.GetKey(Plugin.LoadAmmoHotkey.Value.MainKey) && Input.mouseScrollDelta.y != 0)
            {
                _ = OpenAmmoSelector();
                return;
            }
            if (Input.GetKeyUp(Plugin.LoadAmmoHotkey.Value.MainKey))
            {
                TryLoadAmmo();
            }
        }

        protected async Task OpenAmmoSelector()
        {
            if (GetAmmoItemsFromEquipment(out List<AmmoItemClass> ammos))
            {
                if (GetMagazineForAmmo(ammos[0], out MagazineItemClass foundMagazine))
                {
                    AmmoItemClass chosenAmmo = await ammoSelector.ShowAcceptableAmmos(ammos, InventoryController);
                    if (chosenAmmo != null)
                    {
                        LoadMagazine(chosenAmmo, foundMagazine);
                    }
                }
            }
        }

        protected void TryLoadAmmo()
        {
            if (GetAmmoItemsFromEquipment(out List<AmmoItemClass> reachableAmmos))
            {
                AmmoItemClass chosenAmmo = null;
                if (!Plugin.PrioritizeHighestPenetration.Value)
                {
                    MagazineItemClass currentMagazine = player.LastEquippedWeaponOrKnifeItem.GetCurrentMagazine();
                    if (currentMagazine != null)
                    {
                        foreach (var currAmmo in reachableAmmos)
                        {
                            if (currentMagazine.FirstRealAmmo() is AmmoItemClass ammoInsideMag && ammoInsideMag.TemplateId == currAmmo.TemplateId)
                            {
                                chosenAmmo = currAmmo;
                                break;
                            }
                        }
                    }
                }
                chosenAmmo ??= reachableAmmos[0];
                if (GetMagazineForAmmo(chosenAmmo, out MagazineItemClass foundMagazine))
                {
                    LoadMagazine(chosenAmmo, foundMagazine);
                }
            }
        }

        protected void LoadMagazine(AmmoItemClass ammo, MagazineItemClass magazine)
        {
            //Plugin.LogSource.LogDebug($"Mag {magazine.LocalizedShortName()} ({magazine.Count}); Ammo {ammo.LocalizedShortName()} ({ammo.StackObjectsCount})");
            int loadCount = Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);
            ((PlayerInventoryController)InventoryController).LoadMagazine(ammo, magazine, loadCount, false);
        }

        /// <summary>
        /// Find magazine for ammo
        /// </summary>
        /// <param name="ammo">Ammo that should be compatible with the magazine</param>
        /// <returns></returns>
        public bool GetMagazineForAmmo(AmmoItemClass ammo, out MagazineItemClass foundMagazine)
        {
            // Get Magazine
            var foundMagazines = new List<MagazineItemClass>();
            InventoryController.GetAcceptableItemsNonAlloc(ReachableSlots, foundMagazines,
                item => item is MagazineItemClass mag && mag.Count != mag.MaxCount && mag.CheckCompatibility(ammo)
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
        /// Find ammo for the current weapon.
        /// </summary>
        /// <param name="reachableAmmos">One of each ammo type found then sorted by Penetration Power descending</param>
        /// <returns></returns>
        public bool GetAmmoItemsFromEquipment(out List<AmmoItemClass> reachableAmmos)
        {
            // Get Ammo
            reachableAmmos = new List<AmmoItemClass>();
            if (player.LastEquippedWeaponOrKnifeItem is Weapon weapon)
            {
                string ammoCaliber = weapon.AmmoCaliber;
                InventoryController.GetAcceptableItemsNonAlloc(
                    ReachableSlots,
                    reachableAmmos,
                    item => item is AmmoItemClass ammo && ammo.Caliber == ammoCaliber
                    );
            }
            if (reachableAmmos.Count > 0)
            {
                // Sort penetration power highest to lowest, then stack count ascending
                reachableAmmos.Sort((a, b) =>
                {
                    int result = b.PenetrationPower.CompareTo(a.PenetrationPower);
                    if (result == 0)
                    {
                        result = a.StackObjectsCount.CompareTo(b.StackObjectsCount);
                    }
                    return result;
                });
                var seen = new HashSet<MongoID>();
                reachableAmmos.RemoveAll(ammo => !seen.Add(ammo.TemplateId));
                return true;
            }
            return false;
        }

        public void LoadingStart(LoadingEventType eventType, Class1204 loadingClass, Class1207 unloadingClass)
        {
            IsActive = true;
            if (eventType == LoadingEventType.Load)
            {
                magazine = loadingClass.MagazineItemClass;
                isReachable = IsAtReachablePlace(magazine) && IsAtReachablePlace(loadingClass.AmmoItemClass);
                OnStartLoading?.Invoke(loadingClass.Float_0, loadingClass.Int_0, 0);
            }
            else if (eventType == LoadingEventType.Unload)
            {
                magazine = unloadingClass.MagazineItemClass;
                isReachable = IsAtReachablePlace(magazine);
                OnStartLoading?.Invoke(unloadingClass.Float_0, unloadingClass.Int_1, unloadingClass.Int_0 - unloadingClass.Int_1);
            }
            if (!player.IsInventoryOpened)
            {
                LoadingOutsideInventory();
            }
        }

        public void LoadingOutsideInventory()
        {
            if (IsActive && isReachable && !InventoryController.HasAnyHandsAction())
            {
                SetPlayerState(true);
                ListenForCancel();
                OnCloseInventory?.Invoke(magazine);
                return;
            }
            InventoryController.StopProcesses();
        }

        public void LoadingEnd()
        {
            if (IsActive)
            {
                SetPlayerState(false);
                ResetLoading();
                OnEndLoading?.Invoke();
            }
        }

        protected async void SetPlayerState(bool startAnim)
        {
            if (startAnim)
            {
                player.TrySaveLastItemInHands();
                player.SetEmptyHands(null);
                player.MovementContext.ChangeSpeedLimit(Plugin.SpeedLimit.Value, Player.ESpeedLimit.BarbedWire);
            }
            else
            {
                await Task.Delay(800);

                // Check for active MultiSelect load/unload
                if (MultiSelect.LoadUnloadSerializer != null) return;

                if (!player.IsWeaponOrKnifeInHands)
                {
                    player.TrySetLastEquippedWeapon(true);
                }
                player.MovementContext.RemoveStateSpeedLimit(Player.ESpeedLimit.BarbedWire);
            }
            player.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);
        }

        protected async void ListenForCancel()
        {
            while (IsActive)
            {
                while (InventoryController.HasAnyHandsAction())
                {
                    await Task.Yield();
                }
                if (!player.IsInventoryOpened && (Input.GetKeyDown(Plugin.CancelHotkey.Value.MainKey) || Input.GetKeyDown(Plugin.CancelHotkeyAlt.Value.MainKey) || !player.HandsIsEmpty))
                {
                    InventoryController.StopProcesses();
                    break;
                }
                await Task.Yield();
            }
        }

        protected void ResetLoading()
        {
            IsActive = false;
            isReachable = false;
            magazine = null;
        }

        protected void OnDestroy()
        {
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

        // Base EFT code with modifications
        public bool IsAtReachablePlace(Item item)
        {
            if (item.CurrentAddress == null)
            {
                return false;
            }
            IContainer container = item.Parent.Container as IContainer;
            if (InventoryController.Inventory.Stash == null || container != InventoryController.Inventory.Stash.Grid)
            {
                CompoundItem compoundItem = item as CompoundItem;
                if ((compoundItem == null || !compoundItem.MissingVitalParts.Any()) && InventoryController.Inventory.GetItemsInSlots(ReachableSlots).Contains(item) && InventoryController.Examined(item)) // linq
                {
                    return true;
                }
            }
            return false;
        }

        public string GetMagAmmoCountByLevel()
        {
            int skill = Mathf.Max(
            [
                player.Profile.MagDrillsMastering,
                player.Profile.CheckedMagazineSkillLevel(magazine.Id),
                magazine.CheckOverride
            ]);
            //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine) // Is mag examined?

            return magazine.GetAmmoCountByLevel(magazine.Count, magazine.MaxCount, skill, "#ffffff", true, false, "<color={2}>{0}</color>/{1}");
        }

        public void StopLoading() => InventoryController.StopProcesses();

        public static EquipmentSlot[] ReachableSlots => Plugin.ReachableOnly.Value ? ReachableOnly : ReachableAll;
        public static readonly EquipmentSlot[] ReachableOnly = Inventory.FastAccessSlots.AddRangeToArray([EquipmentSlot.SecuredContainer, EquipmentSlot.ArmBand]);
        public static readonly EquipmentSlot[] ReachableAll = Inventory.FastAccessSlots.AddRangeToArray([EquipmentSlot.ArmorVest, EquipmentSlot.Backpack, EquipmentSlot.SecuredContainer, EquipmentSlot.ArmBand]);

        public enum LoadingEventType
        {
            None,
            Load,
            Unload
        }
    }
}
