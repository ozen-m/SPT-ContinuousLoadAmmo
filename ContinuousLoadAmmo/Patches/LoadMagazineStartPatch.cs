using System;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

#pragma warning disable VSTHRD100
#pragma warning disable VSTHRD003
// ReSharper disable AsyncVoidMethod

namespace ContinuousLoadAmmo.Patches;

// TODO: Fika
public class LoadMagazineStartPatch : ModulePatch
{
    public static event Action OnLoadingEnd;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.PlayerInventoryController.LoadMagazineProcess).GetMethod(nameof(Player.PlayerInventoryController.LoadMagazineProcess.Start));
    }

    [PatchPostfix]
    protected static async void Postfix(Player.PlayerInventoryController.LoadMagazineProcess __instance, Task<IResult> __result)
    {
        await __result;
        OnLoadingEnd?.Invoke();
    }
}
