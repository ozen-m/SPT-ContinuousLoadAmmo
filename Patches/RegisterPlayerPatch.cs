using System.Reflection;
using ContinuousLoadAmmo.Components;
using EFT;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

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

        if (iPlayer is Player player)
        {
            player.gameObject.AddComponent<LoadAmmo>();
            Plugin.LoadAmmoUI.Init();
            Plugin.LogSource.LogInfo($"Added LoadAmmoComponent to player: {player.Profile.Nickname}");
            return;
        }
        Plugin.LogSource.LogError($"Unable to add LoadAmmoComponent to player: {iPlayer.Profile.Nickname}");
    }
}
