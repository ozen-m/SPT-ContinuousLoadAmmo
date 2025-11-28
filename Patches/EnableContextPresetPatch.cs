using System.Reflection;
using ContinuousLoadAmmo.Utils;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class EnableContextPresetPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ContextInteractionSwitcherClass).GetMethod(nameof(ContextInteractionSwitcherClass.IsActive));
    }

    [PatchPrefix]
    protected static bool Prefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
    {
        if (!CommonUtils.InRaid || button != EItemInfoButton.ApplyMagPreset) return true;

        __result = MagazineBuildClass.TryFindPresetSource(__instance.Item_0_1).Succeeded;
        return false;
    }
}
