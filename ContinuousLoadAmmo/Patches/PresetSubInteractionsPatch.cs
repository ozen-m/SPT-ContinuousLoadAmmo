using System.Reflection;
using ContinuousLoadAmmo.Utils;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class PresetSubInteractionsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GClass3757).GetMethod(nameof(GClass3757.CreateSubInteractions));
    }

    [PatchPrefix]
    protected static bool Prefix(GClass3757 __instance, EItemInfoButton parentInteraction, ISubInteractions subInteractionsWrapper)
    {
        if (!CommonUtils.InRaid || parentInteraction != EItemInfoButton.ApplyMagPreset) return true;

        var magPresetItemContext = GClass3458.CreateFromDefaultContext(__instance.ItemContextAbstractClass);
        var session = __instance.ItemUiContext_1.Session;
        var magPresetItemInfoInteractions = new GClass3780(magPresetItemContext, session?.MagBuildsStorage, __instance.ItemUiContext_1);
        subInteractionsWrapper.SetSubInteractions(magPresetItemInfoInteractions);
        return false;
    }
}
