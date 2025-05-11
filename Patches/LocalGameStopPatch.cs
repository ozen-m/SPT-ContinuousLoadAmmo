using ContinuousLoadAmmo.Controllers;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ContinuousLoadAmmo.Patches
{
    internal class LocalGameStopPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.Stop));
        }

        [PatchPrefix]
        protected static void Prefix()
        {
            if (LoadAmmo.IsLoadingAmmo)
            {
                LoadAmmo.Reset();
                LoadAmmo.SetPlayerState(false);
                LoadAmmoUI.DestroyUI();
            }
        }
    }
}
