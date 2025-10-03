using ContinuousLoadAmmo.Components;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ContinuousLoadAmmo.Controllers
{
    internal static class LoadAmmo
    {
        public static MagazineItemClass Magazine;
        public static bool IsLoadingAmmo = false;
        public static bool IsReachable = false;

        private static Player MainPlayer => LoadAmmoComponent.MainPlayer;

        public static async void SetPlayerState(bool startAnim)
        {
            if (startAnim)
            {
                MainPlayer.TrySaveLastItemInHands();
                MainPlayer.SetEmptyHands(null);
                MainPlayer.MovementContext.ChangeSpeedLimit(Plugin.SpeedLimit.Value, Player.ESpeedLimit.BarbedWire);
            }
            else
            {
                await Task.Delay(800);
                if (!MainPlayer.IsWeaponOrKnifeInHands)
                {
                    MainPlayer.TrySetLastEquippedWeapon(true);
                }
                MainPlayer.MovementContext.RemoveStateSpeedLimit(Player.ESpeedLimit.BarbedWire);
            }
            MainPlayer.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);
        }

        public static async void ListenForCancel()
        {
            while (IsLoadingAmmo)
            {
                while (MainPlayer.InventoryController.HasAnyHandsAction())
                {
                    await Task.Yield();
                }
                if (!MainPlayer.IsInventoryOpened && (Input.GetKeyDown(Plugin.CancelHotkey.Value.MainKey) || Input.GetKeyDown(Plugin.CancelHotkeyAlt.Value.MainKey) || !MainPlayer.HandsIsEmpty))
                {
                    MainPlayer.InventoryController.StopProcesses();
                    break;
                }
                await Task.Yield();
            }
        }

        // Base EFT code with modifications
        public static bool IsAtReachablePlace(InventoryController inventoryController, Item item)
        {
            if (item.CurrentAddress == null)
            {
                return false;
            }
            IContainer container = item.Parent.Container as IContainer;
            if (inventoryController.Inventory.Stash == null || container != inventoryController.Inventory.Stash.Grid)
            {
                EquipmentSlot[] slots = GetReachableSlots();
                CompoundItem compoundItem = item as CompoundItem;
                if ((compoundItem == null || !compoundItem.MissingVitalParts.Any()) && inventoryController.Inventory.GetItemsInSlots(slots).Contains(item) && inventoryController.Examined(item)) // linq
                {
                    return true;
                }
            }
            return false;
        }

        public static void Reset()
        {
            IsLoadingAmmo = false;
            IsReachable = false;
            Magazine = null;
        }

        public static EquipmentSlot[] GetReachableSlots() => Plugin.ReachableOnly.Value ? (Plugin.WeaponTopLoad.Value ? ReachableOnlyIncludeTopLoad : ReachableOnly) : InventoryEquipment.AllSlotNames;
        private static readonly EquipmentSlot[] ReachableOnlyIncludeTopLoad = Inventory.BindAvailableSlotsExtended.AddToArray(EquipmentSlot.SecuredContainer);
        private static readonly EquipmentSlot[] ReachableOnly = Inventory.FastAccessSlots.AddToArray(EquipmentSlot.SecuredContainer);

        public enum LoadingEventType
        {
            None,
            Load,
            Unload
        }
    }
}
