using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Utils;
using Diz.LanguageExtensions;
using EFT.Builds;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class ApplyMagPresetPatch : ModulePatch
{
    public static event Action<MagPreset, List<Magazine>> OnApplyMagPreset;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ItemUiContext).GetMethod(nameof(ItemUiContext.ApplyMagPreset));
    }

    [PatchPrefix]
    protected static bool Prefix(
        ItemUiContext __instance,
        MagPreset preset,
        IReadOnlyCollection<Magazine> magazines,
        ref Task<Option> __result
    )
    {
        ProfileMagazinePresetStore.UpdateMagPreset(preset);

        if (!CommonUtils.InRaid) return true;

        __result = Task.FromResult(default(Option)); // Task only used by mag presets window, which we disable in-raid
        OnApplyMagPreset?.Invoke(
            preset,
            magazines as List<Magazine> ?? magazines.ToList() /* Safeguard, all calls are lists */
        );
        return false;
    }
}
