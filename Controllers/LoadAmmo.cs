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
        private static bool isSpeedLimitSetByUs = false;

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
                if (!MainPlayer.MovementContext._speedLimits.ContainsKey(Player.ESpeedLimit.Swamp))
                {
                    MainPlayer.MovementContext.AddStateSpeedLimit(Plugin.SpeedLimit.Value, Player.ESpeedLimit.Swamp);
                    isSpeedLimitSetByUs = true;
                }
            }
            else
            {
                await Task.Delay(800);
                if (MainPlayer.HandsIsEmpty)
                {
                    MainPlayer.TrySetLastEquippedWeapon(true);
                }
                if (isSpeedLimitSetByUs)
                {
                    MainPlayer.MovementContext.RemoveStateSpeedLimit(Player.ESpeedLimit.Swamp);

                    // Reset
                    isSpeedLimitSetByUs = false;
                }
            }
            MainPlayer.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);
        }

        public static async void ListenForCancel(InventoryController inventoryController)
        {
            // Delay is for anim timing
            await Task.Delay(800);

            while (IsLoadingAmmo)
            {
                if (!MainPlayer.IsInventoryOpened && (Input.GetKeyDown(Plugin.CancelHotkey.Value.MainKey) || Input.GetKeyDown(Plugin.CancelHotkeyAlt.Value.MainKey)))
                {
                    break;
                }
                await Task.Yield();
            }

            Reset();
            inventoryController.StopProcesses();
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
                EquipmentSlot[] slots = Plugin.ReachableOnly.Value ? Inventory.BindAvailableSlotsExtended.AddToArray(EquipmentSlot.SecuredContainer) : (EquipmentSlot[])System.Enum.GetValues(typeof(EquipmentSlot));
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
