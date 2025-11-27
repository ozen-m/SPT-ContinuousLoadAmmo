using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Models;
using ContinuousLoadAmmo.Utils;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public class ApplyMagPresetPatch : ModulePatch
{
    private static readonly MagPresetCancelError _magPresetCancelError = new();

    private static CancellationTokenSource _loadPresetCancellationSource;
    private static TaskCompletionSource<GStruct155> _loadPresetResultSource;

    public static
        Action<MagazineBuildPresetClass, List<MagazineItemClass>, CancellationToken>
        OnApplyMagPreset { get; set; }

    public static bool PresetLoaderIsActive => _loadPresetResultSource is { Task.IsCompleted: false };

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
        if (!CommonUtils.InRaid) return true;

        StartNewMagPresetLoading(out var token);
        __result = _loadPresetResultSource.Task;

        OnApplyMagPreset?.Invoke(
            preset,
            magazines as List<MagazineItemClass> ?? magazines.ToList() /* Safeguard, all calls are lists */,
            token
        );
        return false;
    }

    public static void StartNewMagPresetLoading(out CancellationToken token)
    {
        CancelMagPresetLoading();
        _loadPresetResultSource = new TaskCompletionSource<GStruct155>();
        _loadPresetCancellationSource = new CancellationTokenSource();
        token = _loadPresetCancellationSource.Token;
    }

    public static void CancelMagPresetLoading()
    {
        if (_loadPresetCancellationSource is not null)
        {
            _loadPresetCancellationSource.Cancel();
            _loadPresetCancellationSource.Dispose();
            _loadPresetCancellationSource = null;
        }

        _loadPresetResultSource?.TrySetResult(_magPresetCancelError);
    }

    public static void SetMissingResult(GStruct155 result)
    {
        _loadPresetResultSource?.TrySetResult(result);
    }

    public static void SetDefaultResult()
    {
        _loadPresetResultSource?.TrySetResult(default);
    }
}
