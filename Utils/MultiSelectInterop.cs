using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace ContinuousLoadAmmo.Utils;

internal static class MultiSelectInterop
{
    private static readonly Version _requiredVersion = new(4, 0);

    private static bool? _uiFixesLoaded;
    private static Type _multiSelectType;
    private static Func<object> _loadUnloadSerializerGetter;
    private static MethodInfo _stopLoadingMethod;

    public static object LoadUnloadSerializer => Loaded() ? _loadUnloadSerializerGetter?.Invoke() : null;

    public static MethodInfo StopLoadingMethod => Loaded() ? _stopLoadingMethod : null;

    private static bool Loaded()
    {
        if (_uiFixesLoaded.HasValue) return _uiFixesLoaded.Value;

        bool present = Chainloader.PluginInfos.TryGetValue("Tyfon.UIFixes", out PluginInfo pluginInfo);
        _uiFixesLoaded = present && pluginInfo.Metadata.Version >= _requiredVersion;

        if (!_uiFixesLoaded.Value) return _uiFixesLoaded.Value;

        _multiSelectType = Type.GetType("UIFixes.MultiSelect, Tyfon.UIFixes");
        if (_multiSelectType != null)
        {
            var loadUnloadSerializerMethod = AccessTools.PropertyGetter(_multiSelectType, "LoadUnloadSerializer");
            _loadUnloadSerializerGetter = AccessTools.MethodDelegate<Func<object>>(loadUnloadSerializerMethod);
            _stopLoadingMethod = AccessTools.Method(_multiSelectType, "StopLoading");
        }
        return _uiFixesLoaded.Value;
    }
}
