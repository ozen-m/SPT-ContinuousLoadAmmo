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
            return typeof(Player.PlayerInventoryController.Class1088).GetMethod(nameof(Player.PlayerInventoryController.Class1088.Start));
        }

        [PatchPrefix]
        protected static void Prefix(Player.PlayerInventoryController.Class1088 __instance)
        {
            if (!Plugin.InRaid) return;
            LoadAmmo.Inst.LoadingStart(LoadAmmo.LoadingEventType.Unload, null, __instance);
        }

        [PatchPostfix]
        protected static async void Postfix(Task<IResult> __result)
        {
            if (!Plugin.InRaid) return;
            await __result;

            LoadAmmo.Inst.LoadingEnd();
        }
    }
}
