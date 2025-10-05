using Comfort.Common;
using ContinuousLoadAmmo.Components;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    public class RegisterPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.RegisterPlayer));
        }

        [PatchPostfix]
        protected static void Postfix(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                Plugin.LogSource.LogError("Could not add component, player was null!");
                return;
            }
            if (!iPlayer.IsYourPlayer)
            {
                return;
            }

            var mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            mainPlayer.gameObject.AddComponent<LoadAmmo>();
            Plugin.LoadAmmoUI.Init();
            Plugin.LogSource.LogInfo($"Added LoadAmmoComponent to player: {mainPlayer.Profile.Nickname}");
        }
    }
}
