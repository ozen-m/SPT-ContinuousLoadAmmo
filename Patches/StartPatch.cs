using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ContinuousLoadAmmo.Patches
{
    internal class StartPatch : ModulePatch
    {
        private static Player player;
        internal static bool IsLoadingAmmo = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController.Class1085).GetMethod(nameof(Player.PlayerInventoryController.Class1085.Start));
        }

        [PatchPrefix]
        internal static void Prefix()
        {
            if (player == null)
            {
                player = Singleton<GameWorld>.Instance.MainPlayer;
            }

            if (player.IsYourPlayer)
            {
                IsLoadingAmmo = true;
                ListenForCancel(player.InventoryController);
            }
        }

        [PatchPostfix]
        internal static async void Postfix(Task<IResult> __result)
        {
            await __result;

            IsLoadingAmmo = false;
            SetLoadingAmmoAnim(false);
        }

        public static async void SetLoadingAmmoAnim(bool startAnim)
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
                player.TrySetLastEquippedWeapon(true);
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
    }
}
