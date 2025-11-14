using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using ContinuousLoadAmmo.Components;
using EFT;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class LoadMagazineStartPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.PlayerInventoryController.Class1204).GetMethod(nameof(Player.PlayerInventoryController.Class1204.Start));
    }

    [PatchPostfix]
    protected static async void Postfix(Player.PlayerInventoryController.Class1204 __instance, Task<IResult> __result)
    {
        LoadAmmo.Inst.LoadingStart(LoadAmmo.LoadingEventType.Load, __instance, null);
        await __result;
        LoadAmmo.Inst.LoadingEnd();
    }
}
