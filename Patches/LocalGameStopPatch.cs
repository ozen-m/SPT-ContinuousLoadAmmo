using ContinuousLoadAmmo.Components;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    public class LocalGameStopPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.Stop));
        }

        /// <summary>
        /// Stops loading ammo when raid stopped
        /// </summary>
        [PatchPrefix]
        protected static void Prefix()
        {
            LoadAmmo.Inst.StopLoading();
        }
    }
}
