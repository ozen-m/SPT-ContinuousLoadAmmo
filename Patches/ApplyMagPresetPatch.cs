using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Utils;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class ApplyMagPresetPatch : ModulePatch
{
    private static CancellationTokenSource _presetCancellationSource;
    private static TaskCompletionSource<GStruct155> _presetLoadTaskCompletion;

    public static Action<
            MagazineBuildPresetClass,
            List<MagazineItemClass>,
            TaskCompletionSource<GStruct155>,
            CancellationToken>
        OnApplyMagPreset { get; set; }

    public static bool PresetLoaderIsActive => _presetLoadTaskCompletion is { Task.IsCompleted: false };

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ItemUiContext).GetMethod(nameof(ItemUiContext.ApplyMagPreset));
    }

    [PatchPrefix]
    protected static bool Prefix(ItemUiContext __instance, MagazineBuildPresetClass preset, IReadOnlyCollection<MagazineItemClass> magazines, ref Task<GStruct155> __result)
    {
        if (!CommonUtils.InRaid) return true;

        CancelMagPresetLoading();
        _presetCancellationSource = new CancellationTokenSource();
        _presetLoadTaskCompletion = new TaskCompletionSource<GStruct155>();
        __result = _presetLoadTaskCompletion.Task;

        OnApplyMagPreset?.Invoke(
            preset,
            magazines as List<MagazineItemClass> ?? magazines.ToList() /* Safeguard, all calls are lists */,
            _presetLoadTaskCompletion,
            _presetCancellationSource.Token
        );
        return false;
    }

    public static void CancelMagPresetLoading()
    {
        _presetCancellationSource?.Cancel();
        _presetCancellationSource?.Dispose();
        _presetCancellationSource = null;
    }
}
