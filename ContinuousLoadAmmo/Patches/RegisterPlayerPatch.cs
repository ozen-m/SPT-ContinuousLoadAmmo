using System.Reflection;
using ContinuousLoadAmmo.Components;
using ContinuousLoadAmmo.Controllers;
using EFT;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class RegisterPlayerPatch : ModulePatch
{
    private static LoadAmmoUI _loadAmmoUI;

    protected override MethodBase GetTargetMethod()
    {
        _loadAmmoUI = new LoadAmmoUI();
        return typeof(GameWorld).GetMethod(nameof(GameWorld.RegisterPlayer));
    }

    [PatchPostfix]
    protected static void Postfix(GameWorld __instance, IPlayer iPlayer)
    {
        if (__instance is HideoutGameWorld)
        {
            return;
        }
        if (!iPlayer.IsYourPlayer)
        {
            return;
        }

        if (iPlayer is Player player)
        {
            var loadAmmoController = new LoadAmmoController(player);
            LoadAmmoComponent.Create(player.gameObject, loadAmmoController);
            _loadAmmoUI.Initialize(loadAmmoController);

            ContinuousLoadAmmo.LogSource.LogInfo($"Added LoadAmmoComponent to player: {player.Profile.Nickname}");
            return;
        }
        ContinuousLoadAmmo.LogSource.LogError($"Unable to add LoadAmmoComponent to player: {iPlayer.Profile.Nickname}");
    }
}
