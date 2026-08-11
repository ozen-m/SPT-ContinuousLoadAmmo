using System.Reflection;
using ContinuousLoadAmmo.Utils;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class PresetSubInteractionsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BaseInventoryItemContextInteractions).GetMethod(nameof(BaseInventoryItemContextInteractions.CreateSubInteractions));
    }

    [PatchPrefix]
    protected static bool Prefix(BaseInventoryItemContextInteractions __instance, EItemInfoButton parentInteraction, ISubInteractionsWrapper subInteractionsWrapper)
    {
        if (!CommonUtils.InRaid || parentInteraction != EItemInfoButton.ApplyMagPreset) return true;

        var magPresetItemContext = InventorySelectableItemContext.CreateFromDefaultContext(__instance.ItemContext);
        var session = __instance.ItemUiContext.Session;
        var magPresetItemInfoInteractions = new MagPresetContextInteractions(magPresetItemContext, session?.MagBuildsStorage, __instance.ItemUiContext);
        subInteractionsWrapper.SetSubInteractions(magPresetItemInfoInteractions);
        return false;
    }
}
