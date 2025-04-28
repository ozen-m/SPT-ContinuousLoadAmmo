using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ContinuousLoadAmmo.Controllers
{
    internal static class LoadAmmo
    {
        private static Player _mainPlayer = null;
        public static bool IsLoadingAmmo = false;
        public static bool IsReachable = false;
        public static bool IsOutsideInventory = false;
        public static MagazineItemClass Magazine;

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
                MainPlayer.MovementContext.AddStateSpeedLimit(Plugin.SpeedLimit.Value, Player.ESpeedLimit.Swamp);
            }
            else
            {
                await Task.Delay(800);
                if (MainPlayer.HandsIsEmpty)
                {
                    MainPlayer.TrySetLastEquippedWeapon(true);
                }
                MainPlayer.MovementContext.RemoveStateSpeedLimit(Player.ESpeedLimit.Swamp);
            }
            MainPlayer.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);
        }

        public static async void ListenForCancel(InventoryController inventoryController)
        {
            while (IsLoadingAmmo)
            {
                if (!MainPlayer.IsInventoryOpened && (Input.GetKeyDown(Plugin.CancelHotkey.Value.MainKey) || Input.GetKeyDown(Plugin.CancelHotkeyAlt.Value.MainKey)))
                {
                    Reset();
                    inventoryController.StopProcesses();
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
                EquipmentSlot[] slots = Plugin.ReachableOnly.Value ? Inventory.FastAccessSlots : (EquipmentSlot[])System.Enum.GetValues(typeof(EquipmentSlot));
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
