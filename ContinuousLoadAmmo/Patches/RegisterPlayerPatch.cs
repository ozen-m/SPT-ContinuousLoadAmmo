using System.Reflection;
using ContinuousLoadAmmo.Components;
using ContinuousLoadAmmo.Controllers;
using ContinuousLoadAmmo.Utils;
using EFT;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class RegisterPlayerPatch : ModulePatch
{
    private static LoadAmmoController _loadAmmoController;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.RegisterPlayer));
    }

    [PatchPostfix]
    public static void Postfix(GameWorld __instance, IPlayer iPlayer)
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
            _loadAmmoController = new LoadAmmoController(player);
            QuickAmmoSelector.Create(CommonUtils.EftBattleUIScreenTransform, _loadAmmoController);
            var loadAmmoUI = new LoadAmmoUI();
            loadAmmoUI.Initialize(CommonUtils.EftBattleUIScreenTransform, _loadAmmoController);

            L.Info($"Added LoadAmmoComponent to player: {player.Profile.Nickname}");
            return;
        }
        L.Error($"Unable to add LoadAmmoComponent to player: {iPlayer.Profile.Nickname}");
    }

    public static void DisposeLoadAmmoController()
    {
        _loadAmmoController?.Dispose();
        _loadAmmoController = null;
    }
}
