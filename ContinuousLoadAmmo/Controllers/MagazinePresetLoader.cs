using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContinuousLoadAmmo.Patches;
using ContinuousLoadAmmo.Utils;
using EFT;
using EFT.Builds;
using EFT.Communications;
using EFT.InventoryLogic;
using UnityEngine;

namespace ContinuousLoadAmmo.Controllers;

public class MagazinePresetLoader : IDisposable
{
    private readonly LoadAmmoController _loadAmmoController;
    private CancellationTokenSource _loadPresetCancellationSource;

    public bool PresetLoaderIsActive => _loadPresetCancellationSource is not null;

    public MagazinePresetLoader(LoadAmmoController loadAmmoController)
    {
        _loadAmmoController = loadAmmoController;

        OnClickPatch.CancelPresetLoaderOnClick += CancelMagPresetLoading;
        ApplyMagPresetPatch.OnApplyMagPreset += InventoryLoadMagPreset;
    }

    public void QuickLoadMagPreset(MagPreset preset)
    {
        if (!_loadAmmoController.IsQuickLoadAvailable(out var availableAmmo, out var magazine, preset.GetCaliberReally()))
        {
            CommonUtils.DisplayNotification(
                $"No reachable ammo or magazines found to load for preset: {preset.DisplayText()}",
                ENotificationIconType.Alert,
                true
            );
            return;
        }

        var token = StartNewLoadMagPreset();
        _ = LoadingMagPresetInternalAsync(preset, [magazine], availableAmmo, token);
    }

    public void CancelMagPresetLoading()
    {
        if (!PresetLoaderIsActive) return;

        _loadPresetCancellationSource.Cancel();
        _loadPresetCancellationSource.Dispose();
        _loadPresetCancellationSource = null;

        // BUG: While loading preset, dragging ammo to another magazine does not stop the previous load preset process
    }

    public bool IsPresetAvailableForCurrentWeapon(out MagPreset preset)
    {
        var weaponCaliber = _loadAmmoController.GetCurrentWeaponCaliber();
        preset = ProfileMagazinePresetStore.GetMagPreset(weaponCaliber);

        return preset is not null;
    }

    private CancellationToken StartNewLoadMagPreset()
    {
        CancelMagPresetLoading();
        _loadPresetCancellationSource = new CancellationTokenSource();
        return _loadPresetCancellationSource.Token;
    }

    private void InventoryLoadMagPreset(MagPreset preset, List<Magazine> magazines)
    {
        if (!_loadAmmoController.GetAllAmmoForMagazine(out var availableAmmo, magazines[0]))
        {
            CommonUtils.DisplayNotification(
                $"No reachable ammo or magazines found to load for preset: {preset.DisplayText()}",
                ENotificationIconType.Alert,
                true
            );
            return;
        }

        var token = StartNewLoadMagPreset();
        _ = LoadingMagPresetInternalAsync(preset, magazines, availableAmmo, token);
    }

    private async Task LoadingMagPresetInternalAsync(
        MagPreset preset,
        List<Magazine> magazines,
        List<Ammo> availableAmmo,
        CancellationToken token = default
    )
    {
        try
        {
            foreach (var magazine in magazines)
            {
                token.ThrowIfCancellationRequested();

                CommonUtils.DisplayNotification($"Loading {preset.DisplayText()}", ENotificationIconType.Note);

                // Bottom
                var bottomCount = 0;
                foreach (var bottom in preset.Bottom)
                {
                    token.ThrowIfCancellationRequested();

                    if (bottom is null) continue;

                    bottomCount = bottom.Count;
                    if (magazine.Count >= bottom.Count) continue;

                    var toLoad = Mathf.Min(bottom.Count, bottom.Count - magazine.Count);
                    await TryLoadPresetStepAsync(availableAmmo, magazine, bottom, toLoad, token);
                }

                // Loop
                // Track toSkip to resume loading from current count
                var toSkip = magazine.Count - bottomCount;
                var freeLoopSpace = magazine.MaxCount - magazine.Count;
                var topCount = 0;
                foreach (var top in preset.Top)
                {
                    if (top is not null) topCount += top.Count;
                }
                freeLoopSpace -= topCount;

                while (freeLoopSpace > 0)
                {
                    foreach (var loop in preset.Loop)
                    {
                        token.ThrowIfCancellationRequested();
                        if (loop is null || freeLoopSpace <= 0) continue;

                        var toLoad = (int)loop.Count;
                        if (toSkip > 0) // Resume loading from current count
                        {
                            // Should skip entire group?
                            if (toSkip >= toLoad)
                            {
                                toSkip -= toLoad;
                                continue;
                            }

                            // Load remaining
                            toLoad -= toSkip;
                            toSkip = 0;
                        }
                        toLoad = Mathf.Min(toLoad, freeLoopSpace);
                        await TryLoadPresetStepAsync(availableAmmo, magazine, loop, toLoad, token);
                        freeLoopSpace -= toLoad;
                    }
                }

                // Top
                foreach (var top in preset.Top)
                {
                    token.ThrowIfCancellationRequested();

                    if (top is null) continue;

                    var toLoad = Mathf.Min(top.Count, magazine.MaxCount - magazine.Count);
                    await TryLoadPresetStepAsync(availableAmmo, magazine, top, toLoad, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // CancelMagPresetLoading is called elsewhere
            return;
        }

        CancelMagPresetLoading();
    }

    private async Task TryLoadPresetStepAsync(
        List<Ammo> availableAmmo,
        Magazine magazine,
        MagPreset.MagPresetItem preset,
        int toLoad,
        CancellationToken token
    )
    {
        var matchingAmmo = GetMatchingAmmo(availableAmmo, preset.TemplateId, toLoad);
        if (matchingAmmo is null)
        {
            var missingMessage =
                $"{MagPreset.NotEnoughAmmoError.MAG_PRESET_MISSING_ITEMS_NOTIFICATION.Localized()} {preset.TemplateId.LocalizedShortName()}, Count: {toLoad}";
            CommonUtils.DisplayNotification(missingMessage, ENotificationIconType.Alert, true, ENotificationDurationType.Long);

            if (ContinuousLoadAmmo.MagPresetFallback.Value)
            {
                var fallbackAmmo = GetValidAmmo(availableAmmo);
                if (fallbackAmmo is not null)
                {
                    CommonUtils.DisplayNotification($"Loading {fallbackAmmo.LocalizedShortName()}", ENotificationIconType.Note);
                    await _loadAmmoController.LoadMagazineAsync(fallbackAmmo, magazine, token: token);
                }
            }
            CancelMagPresetLoading();
            return;
        }
        await _loadAmmoController.LoadMagazineAsync(matchingAmmo, magazine, toLoad, token);
    }

    private static Ammo GetMatchingAmmo(List<Ammo> ammo, MongoID templateId, int count)
    {
        foreach (var ammoItem in ammo)
        {
            if (ammoItem.TemplateId != templateId || ammoItem.StackObjectsCount < count) continue;

            return ammoItem;
        }

        return null;
    }

    private static Ammo GetValidAmmo(List<Ammo> ammo)
    {
        foreach (var ammoItem in ammo)
        {
            if (ammoItem.StackObjectsCount <= 0) continue;

            return ammoItem;
        }

        return null;
    }

    public void Dispose()
    {
        CancelMagPresetLoading();

        OnClickPatch.CancelPresetLoaderOnClick -= CancelMagPresetLoading;
        ApplyMagPresetPatch.OnApplyMagPreset -= InventoryLoadMagPreset;
    }
}
