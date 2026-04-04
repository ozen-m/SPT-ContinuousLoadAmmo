using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace ContinuousLoadAmmo.Utils;

internal static class MultiSelectInterop
{
    private static readonly Version _requiredVersion = new(5, 3, 4);

    private static bool? _uiFixesLoaded;
    private static Func<object> _loadUnloadSerializerGetter;
    private static AccessTools.FieldRef<object, TaskCompletionSource> _totalTaskField;
    private static MethodInfo _stopLoadingMethod;

    public static bool MultiSelectLoadSerializerIsActive
    {
        get
        {
            if (!Loaded()) return false;

            var serializer = _loadUnloadSerializerGetter.Invoke();
            if (serializer is null) return false;

            return !_totalTaskField(serializer).Task.IsCompleted;
        }
    }

    public static MethodInfo StopLoadingMethod => Loaded() ? _stopLoadingMethod : null;

    private static bool Loaded()
    {
        if (_uiFixesLoaded.HasValue) return _uiFixesLoaded.Value;

        var present = Chainloader.PluginInfos.TryGetValue("Tyfon.UIFixes", out var pluginInfo) // TODO: Remove in 4.1.x
                      || Chainloader.PluginInfos.TryGetValue("com.tyfon.uifixes", out pluginInfo);
        var correctVersion = present && pluginInfo.Metadata.Version >= _requiredVersion;
        _uiFixesLoaded = present && correctVersion;

        if (present && !correctVersion)
        {
            ContinuousLoadAmmo.LogSource.LogWarning(
                $"UI Fixes {pluginInfo.Metadata.Version} is present but minimum version: {_requiredVersion} is required, interop will not work"
            );
        }
        if (!_uiFixesLoaded.Value) return false;

        try
        {
            var multiSelectType = Type.GetType("UIFixes.MultiSelect, Tyfon.UIFixes");
            var taskSerializerType = Type.GetType("UIFixes.MultiSelectItemContextTaskSerializer, Tyfon.UIFixes");
            var loadUnloadSerializerMethod = AccessTools.PropertyGetter(multiSelectType, "LoadUnloadSerializer");
            _loadUnloadSerializerGetter = AccessTools.MethodDelegate<Func<object>>(loadUnloadSerializerMethod);
            _stopLoadingMethod = AccessTools.Method(multiSelectType, "StopLoading");
            _totalTaskField = AccessTools.FieldRefAccess<TaskCompletionSource>(taskSerializerType, "_totalTask");

            if (_loadUnloadSerializerGetter is null || _stopLoadingMethod is null || _totalTaskField is null)
            {
                throw new InvalidOperationException("UI Fixes interop: Required method or field could not be found.");
            }

            ContinuousLoadAmmo.LogSource.LogInfo("UI Fixes interop loaded successfully");
        }
        catch (Exception ex)
        {
            _uiFixesLoaded = false;
            ContinuousLoadAmmo.LogSource.LogWarning(
                $"UI Fixes {pluginInfo!.Metadata.Version} is present but something went wrong, interop will not work"
            );
            ContinuousLoadAmmo.LogSource.LogWarning(ex.ToString());
        }

        return _uiFixesLoaded.Value;
    }
}
