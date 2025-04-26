using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ContinuousLoadAmmo.Patches
{
    internal class StartPatch : ModulePatch
    {
        internal static Player player;
        internal static bool IsLoadingAmmo = false;
        internal static bool IsReachable = false;
        internal static MagazineItemClass Magazine;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController.Class1085).GetMethod(nameof(Player.PlayerInventoryController.Class1085.Start));
        }

        [PatchPrefix]
        protected static void Prefix(Player.PlayerInventoryController.Class1085 __instance)
        {
            if (player == null)
            {
                player = Singleton<GameWorld>.Instance.MainPlayer;
            }

            if (player.IsYourPlayer)
            {
                IsLoadingAmmo = true;
                Magazine = __instance.magazineItemClass;
                IsReachable = IsAtReachablePlace(player.InventoryController, Magazine) && IsAtReachablePlace(player.InventoryController, __instance.ammoItemClass);

                ListenForCancel(player.InventoryController);
            }
        }

        [PatchPostfix]
        protected static async void Postfix(Task<IResult> __result)
        {
            await __result;

            IsLoadingAmmo = false;
            IsReachable = false;

            InventoryScreenClosePatch.DestroyUI();
            SetPlayerState(false);
        }

        public static async void SetPlayerState(bool startAnim)
        {
            if (startAnim)
            {
                player.TrySaveLastItemInHands();
                player.SetEmptyHands(null);
                player.MovementContext.AddStateSpeedLimit(ContinuousLoadAmmo.SpeedLimit.Value, Player.ESpeedLimit.Swamp);
            }
            else
            {
                await Task.Delay(800);
                if (player.HandsIsEmpty)
                {
                    player.TrySetLastEquippedWeapon(true);
                }
                player.MovementContext.RemoveStateSpeedLimit(Player.ESpeedLimit.Swamp);
            }
            player.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, startAnim);
        }

        private static async void ListenForCancel(InventoryController inventoryController)
        {
            while (IsLoadingAmmo)
            {
                if (!player.IsInventoryOpened && (Input.GetKeyDown(ContinuousLoadAmmo.CancelHotkey.Value.MainKey) || Input.GetKeyDown(ContinuousLoadAmmo.CancelHotkeyAlt.Value.MainKey)))
                {
                    IsLoadingAmmo = false;
                    inventoryController.StopProcesses();
                }
                await Task.Yield();
            }
        }

        // Base EFT code with modifications
        private static bool IsAtReachablePlace(InventoryController inventoryController, Item item)
        {
            if (item.CurrentAddress == null)
            {
                return false;
            }
            IContainer container = item.Parent.Container as IContainer;
            if (inventoryController.Inventory.Stash == null || container != inventoryController.Inventory.Stash.Grid)
            {
                EquipmentSlot[] slots = ContinuousLoadAmmo.ReachableOnly.Value ? Inventory.FastAccessSlots : (EquipmentSlot[])System.Enum.GetValues(typeof(EquipmentSlot));
                CompoundItem compoundItem = item as CompoundItem;
                if ((compoundItem == null || !compoundItem.MissingVitalParts.Any<Slot>()) && inventoryController.Inventory.GetItemsInSlots(slots).Contains(item) && inventoryController.Examined(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
