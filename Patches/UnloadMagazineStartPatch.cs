using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using ContinuousLoadAmmo.Controllers;
using EFT;
using SPT.Reflection.Patching;

#pragma warning disable VSTHRD100
#pragma warning disable VSTHRD003
// ReSharper disable AsyncVoidMethod

namespace ContinuousLoadAmmo.Patches;

public class UnloadMagazineStartPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.PlayerInventoryController.Class1207).GetMethod(nameof(Player.PlayerInventoryController.Class1207.Start));
    }

    [PatchPostfix]
    protected static async void Postfix(Player.PlayerInventoryController.Class1207 __instance, Task<IResult> __result)
    {
        LoadAmmoController.Inst.LoadingStart(LoadAmmoController.LoadingEventType.Unload, null, __instance);
        await __result;
        LoadAmmoController.Inst.LoadingEnd();
    }
}
