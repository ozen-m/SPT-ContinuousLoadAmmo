using Comfort.Common;
using ContinuousLoadAmmo.Components;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    internal class RegisterPlayerPatch : ModulePatch
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

            Singleton<GameWorld>.Instance.MainPlayer.gameObject.AddComponent<LoadAmmoComponent>();
            Plugin.LogSource.LogInfo("Added LoadAmmoComponent to player: " + Singleton<GameWorld>.Instance.MainPlayer.Profile.Nickname);
        }
    }
}
