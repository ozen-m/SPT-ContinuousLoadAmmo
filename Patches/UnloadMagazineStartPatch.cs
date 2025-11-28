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

public class UnloadMagazineStartPatch : ModulePatch
{
    public static event Action OnLoadingEnd;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.PlayerInventoryController.Class1207).GetMethod(nameof(Player.PlayerInventoryController.Class1207.Start));
    }

    [PatchPostfix]
    protected static async void Postfix(Player.PlayerInventoryController.Class1207 __instance, Task<IResult> __result)
    {
        await __result;
        OnLoadingEnd?.Invoke();
    }
}
