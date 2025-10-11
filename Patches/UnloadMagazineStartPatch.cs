using Comfort.Common;
using ContinuousLoadAmmo.Components;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace ContinuousLoadAmmo.Patches
{
    public class UnloadMagazineStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController.Class1207).GetMethod(nameof(Player.PlayerInventoryController.Class1207.Start));
        }

        [PatchPostfix]
        protected static async void Postfix(Player.PlayerInventoryController.Class1207 __instance, Task<IResult> __result)
        {
            if (!Plugin.InRaid) return;

            LoadAmmo.Inst.LoadingStart(LoadAmmo.LoadingEventType.Unload, null, __instance);
            await __result;
            LoadAmmo.Inst.LoadingEnd();
        }
    }
}
