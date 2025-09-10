using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ContinuousLoadAmmo.Controllers
{
    internal static class LoadAmmo
    {
        private static Player _mainPlayer = null;
        public static MagazineItemClass Magazine;
        public static bool IsLoadingAmmo = false;
        public static bool IsReachable = false;
        public static bool IsOutsideInventory = false;

        public static Player MainPlayer
        {
            get
            {
                if (_mainPlayer == null)
                {
                    _mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                }
                return _mainPlayer;
            }
        }

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
                if (MainPlayer.HandsIsEmpty)
                {
                    MainPlayer.TrySetLastEquippedWeapon(true);
                }
                MainPlayer.MovementContext.RemoveStateSpeedLimit(Player.ESpeedLimit.BarbedWire);
            }
            MainPlayer.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);
        }

        public static async void ListenForCancel(InventoryController inventoryController)
        {
            while (IsLoadingAmmo)
            {
                if (!MainPlayer.IsInventoryOpened && !inventoryController.HasAnyHandsAction() && (Input.GetKeyDown(Plugin.CancelHotkey.Value.MainKey) || Input.GetKeyDown(Plugin.CancelHotkeyAlt.Value.MainKey) || !MainPlayer.HandsIsEmpty))
                {
                    inventoryController.StopProcesses();
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
                EquipmentSlot[] slots = Plugin.ReachableOnly.Value ? (Plugin.WeaponTopLoad.Value ? Inventory.BindAvailableSlotsExtended.AddToArray(EquipmentSlot.SecuredContainer) : Inventory.FastAccessSlots.AddToArray(EquipmentSlot.SecuredContainer)) : InventoryEquipment.AllSlotNames;
                CompoundItem compoundItem = item as CompoundItem;
                if ((compoundItem == null || !compoundItem.MissingVitalParts.Any()) && inventoryController.Inventory.GetItemsInSlots(slots).Contains(item) && inventoryController.Examined(item))
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
            IsOutsideInventory = false;
            Magazine = null;
        }
    }
}
