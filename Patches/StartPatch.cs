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
                SetLoadingAmmoAnim(IsLoadingAmmo);
                ListenForCancel(player.InventoryController);
            }
        }

        [PatchPostfix]
        internal static void Postfix(Task<IResult> __result)
        {
            AsyncWrapper(__result);
        }

        public static void SetLoadingAmmoAnim(bool isLoadingAmmo)
        {
            if (isLoadingAmmo)
            {
                player.SetEmptyHands(null);
                player.MovementContext.AddStateSpeedLimit(ContinuousLoadAmmo.SpeedLimit.Value, Player.ESpeedLimit.Swamp);
            }
            else
            {
                player.TrySetLastEquippedWeapon();
                player.MovementContext.RemoveStateSpeedLimit(Player.ESpeedLimit.Swamp);
            }
            player.MovementContext.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, isLoadingAmmo);
        }

        private static async void ListenForCancel(InventoryController inventoryController)
        {
            while (IsLoadingAmmo)
            {
                if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
                {
                    IsLoadingAmmo = false;
                    inventoryController.StopProcesses();
                }
                await Task.Yield();
            }
        }

        private static async void AsyncWrapper(Task<IResult> task)
        {
            await task;
            IsLoadingAmmo = false;
            SetLoadingAmmoAnim(IsLoadingAmmo);
        }
    }
}
