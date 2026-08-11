using System.Reflection;
using ContinuousLoadAmmo.Utils;
using EFT.Builds;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class EnableContextPresetPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ItemContextInteractionsSwitcher).GetMethod(nameof(ItemContextInteractionsSwitcher.IsActive));
    }

    [PatchPrefix]
    protected static bool Prefix(ItemContextInteractionsSwitcher __instance, EItemInfoButton button, ref bool __result)
    {
        if (!CommonUtils.InRaid || button != EItemInfoButton.ApplyMagPreset) return true;

        __result = MagBuildsStorage.TryFindPresetSource(__instance._item).Succeeded;
        return false;
    }
}
