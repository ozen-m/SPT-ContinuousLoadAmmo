using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Utils;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class ApplyMagPresetPatch : ModulePatch
{
    public static event Action<MagazineBuildPresetClass, List<MagazineItemClass>> OnApplyMagPreset;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ItemUiContext).GetMethod(nameof(ItemUiContext.ApplyMagPreset));
    }

    [PatchPrefix]
    protected static bool Prefix(
        ItemUiContext __instance,
        MagazineBuildPresetClass preset,
        IReadOnlyCollection<MagazineItemClass> magazines,
        ref Task<GStruct155> __result
    )
    {
        ProfileMagazinePresetStore.UpdateMagPreset(preset);

        if (!CommonUtils.InRaid) return true;

        __result = Task.FromResult(default(GStruct155)); // Task only used by mag presets window, which we disable in-raid
        OnApplyMagPreset?.Invoke(
            preset,
            magazines as List<MagazineItemClass> ?? magazines.ToList() /* Safeguard, all calls are lists */
        );
        return false;
    }
}
