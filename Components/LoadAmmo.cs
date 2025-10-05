using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        protected static FieldInfo interfaceFieldInfo;

        public event Action<InventoryController, LoadingEventType, GEventArgs7, GEventArgs8> OnStartLoading;
        public event Action OnCloseInventory;
        public event Action OnEndLoading;
        public event Action OnDestroyComponent;

        protected Player player;
        protected InventoryController inventoryController;
        protected MagazineItemClass magazine;
        protected bool isReachable;
        public bool IsActive { get; protected set; }

        protected void Awake()
        {
            player = (Player)Singleton<GameWorld>.Instance.MainPlayer;
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

            inventoryController = player.InventoryController;
            interfaceFieldInfo ??= typeof(PlayerInventoryController).GetField("interface17_0", BindingFlags.Instance | BindingFlags.NonPublic);

            ((PlayerInventoryController)inventoryController).SetNextProcessLocked(false);
        }

        protected void Update()
        {
            if (!Singleton<GameWorld>.Instantiated) return;
            if (player == null) return;

            if (!player.IsInventoryOpened && Input.GetKeyDown(Plugin.LoadAmmoHotkey.Value.MainKey))
            {
                TryLoadAmmo();
            }
        }

        protected void TryLoadAmmo()
        {
            if (IsActive || inventoryController.HasAnyHandsAction())
            {
                return;
            }
            if (FindMagAmmoFromEquipment(out AmmoItemClass ammo, out MagazineItemClass magazine))
            {
                int loadCount = Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);
                ((PlayerInventoryController)inventoryController).LoadMagazine(ammo, magazine, loadCount, false);
            }
        }

        protected bool FindMagAmmoFromEquipment(out AmmoItemClass ammo, out MagazineItemClass magazine)
        {
            ammo = null;
            magazine = null;
            StringBuilder sb = new();

            // Get Ammo
            MagazineItemClass currentMagazine = null;
            List<AmmoItemClass> reachableAmmos = new();
            AmmoItemClass chosenAmmo = null;
            if (player.LastEquippedWeaponOrKnifeItem is Weapon weapon)
            {
                sb.Append($"Weapon: {weapon}. ");
                currentMagazine = weapon.GetCurrentMagazine();
                if (currentMagazine != null)
                {
                    inventoryController.GetAcceptableItemsNonAlloc(
                        ReachableSlots,
                        reachableAmmos,
                        item => item is AmmoItemClass ammo && currentMagazine.CheckCompatibility(ammo)
                        );
                }
                else
                {   // Fallback if no magazine
                    string ammoCaliber = weapon.AmmoCaliber;
                    inventoryController.GetAcceptableItemsNonAlloc(
                        ReachableSlots,
                        reachableAmmos,
                        item => item is AmmoItemClass ammo && ammo.Caliber == ammoCaliber
                        );
                }
            }
            if (reachableAmmos.Count > 0)
            {
                // Sort penetration power highest to lowest
                reachableAmmos.Sort((a, b) =>
                {
                    int result = b.PenetrationPower.CompareTo(a.PenetrationPower);
                    if (result == 0)
                    {
                        result = b.StackObjectsCount.CompareTo(a.StackObjectsCount);
                    }
                    return result;
                });
                if (!Plugin.PrioritizeHighestPenetration.Value && currentMagazine != null)
                {
                    foreach (var currAmmo in reachableAmmos)
                    {
                        if (currentMagazine.FirstRealAmmo() is AmmoItemClass ammoInsideMag && ammoInsideMag.TemplateId == currAmmo.TemplateId)
                        {
                            sb.Append("Found same ammo. ");
                            ammo = chosenAmmo = currAmmo;
                            break;
                        }
                    }
                    if (ammo == null)
                    {
                        sb.Append("No same ammo from magazine found, fallback to ammo with highest penetration. ");
                        ammo = chosenAmmo = reachableAmmos[0];
                    }
                }
                else
                {
                    sb.Append("Choosing ammo with highest penetration. ");
                    ammo = chosenAmmo = reachableAmmos[0];
                }
            }
            else
            {
                sb.Append("No ammo found.");
                Plugin.LogSource.LogDebug(sb.ToString());
                return false;
            }
            sb.Append($"Ammo {ammo.LocalizedShortName()}. ");

            // Get Magazine
            List<MagazineItemClass> reachableMagazines = new();
            inventoryController.GetAcceptableItemsNonAlloc(ReachableSlots, reachableMagazines,
                item => item is MagazineItemClass mag && mag.Count != mag.MaxCount && mag.CheckCompatibility(chosenAmmo)
                );
            if (reachableMagazines.Count > 0)
            {
                // Sort by almost full
                reachableMagazines.Sort((a, b) =>
                    (a.MaxCount - a.Count).CompareTo(b.MaxCount - b.Count)
                    );
                magazine = reachableMagazines[0];
                sb.Append($"Magazine {magazine.LocalizedShortName()}");
                Plugin.LogSource.LogDebug(sb.ToString());
                return true;
            }
            sb.Append("No magazine found.");
            Plugin.LogSource.LogDebug(sb.ToString());
            return false;
        }

        public void LoadingStart(LoadingEventType eventType, Class1085 loadingClass, Class1088 unloadingClass)
        {
            IsActive = true;
            if (eventType == LoadingEventType.Load)
            {
                magazine = loadingClass.magazineItemClass;
                isReachable = IsAtReachablePlace(magazine) && IsAtReachablePlace(loadingClass.ammoItemClass);
                GEventArgs7 loadAmmoEvent = new(loadingClass.ammoItemClass, loadingClass.magazineItemClass, loadingClass.int_0, loadingClass.float_0, CommandStatus.Begin, loadingClass.inventoryController_0);
                OnStartLoading?.Invoke(inventoryController, eventType, loadAmmoEvent, null);
            }
            else if (eventType == LoadingEventType.Unload)
            {
                magazine = unloadingClass.magazineItemClass;
                isReachable = IsAtReachablePlace(magazine);
                GEventArgs8 unloadAmmoEvent = new(unloadingClass.item_0, unloadingClass.item_1, unloadingClass.magazineItemClass, unloadingClass.int_0 - unloadingClass.int_1, unloadingClass.int_1, unloadingClass.float_0, EFT.InventoryLogic.CommandStatus.Begin, unloadingClass.inventoryController_0);
                OnStartLoading?.Invoke(inventoryController, eventType, null, unloadAmmoEvent);
            }
            if (!player.IsInventoryOpened)
            {
                LoadingOutsideInventory();
            }
        }

        public void LoadingOutsideInventory()
        {
            if (IsActive && isReachable && !inventoryController.HasAnyHandsAction())
            {
                SetPlayerState(true);
                ListenForCancel();
                OnCloseInventory?.Invoke();
                return;
            }
            inventoryController.StopProcesses();
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
                while (inventoryController.HasAnyHandsAction())
                {
                    await Task.Yield();
                }
                if (!player.IsInventoryOpened && (Input.GetKeyDown(Plugin.CancelHotkey.Value.MainKey) || Input.GetKeyDown(Plugin.CancelHotkeyAlt.Value.MainKey) || !player.HandsIsEmpty))
                {
                    inventoryController.StopProcesses();
                    break;
                }
                await Task.Yield();
            }
        }

        // Base EFT code with modifications
        protected bool IsAtReachablePlace(Item item)
        {
            if (item.CurrentAddress == null)
            {
                return false;
            }
            IContainer container = item.Parent.Container as IContainer;
            if (inventoryController.Inventory.Stash == null || container != inventoryController.Inventory.Stash.Grid)
            {
                CompoundItem compoundItem = item as CompoundItem;
                if ((compoundItem == null || !compoundItem.MissingVitalParts.Any()) && inventoryController.Inventory.GetItemsInSlots(ReachableSlots).Contains(item) && inventoryController.Examined(item)) // linq
                {
                    return true;
                }
            }
            return false;
        }

        protected void ResetLoading()
        {
            IsActive = false;
            isReachable = false;
            magazine = null;
        }

        public void OnDestroy()
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

        public string GetMagAmmoCountByLevel()
        {
            int skill = Mathf.Max(
            [
                player.Profile.MagDrillsMastering,
                player.Profile.CheckedMagazineSkillLevel(magazine.Id),
                magazine.CheckOverride
            ]);
            //bool @checked = player.InventoryController.CheckedMagazine(StartPatch.Magazine) // Is mag examined?

            var value = magazine.GetAmmoCountByLevel(magazine.Count, magazine.MaxCount, skill, "#ffffff", true, false, "<color={2}>{0}</color>/{1}");
            return value;
        }

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
