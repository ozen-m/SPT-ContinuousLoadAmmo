using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Reflection;

namespace UIFixesInterop
{
    internal static class MultiSelect
    {
        private static readonly Version RequiredVersion = new Version(4, 0);

        private static bool? UIFixesLoaded;

        private static Type MultiSelectType;
        private static Func<object> LoadUnloadSerializerGetter;

        public static object LoadUnloadSerializer
        {
            get
            {
                if (!Loaded())
                {
                    return null;
                }

                return LoadUnloadSerializerGetter?.Invoke();
            }
        }

        private static MethodInfo _stopLoadingMethod;
        public static MethodInfo StopLoadingMethod
        {
            get
            {
                if (!Loaded())
                {
                    return null;
                }
                return _stopLoadingMethod ?? null;
            }
        }

        public static bool Loaded()
        {
            if (!UIFixesLoaded.HasValue)
            {
                bool present = Chainloader.PluginInfos.TryGetValue("Tyfon.UIFixes", out PluginInfo pluginInfo);
                UIFixesLoaded = present && pluginInfo.Metadata.Version >= RequiredVersion;

                if (UIFixesLoaded.Value)
                {
                    MultiSelectType = Type.GetType("UIFixes.MultiSelect, Tyfon.UIFixes");
                    if (MultiSelectType != null)
                    {
                        var LoadUnloadSerializerMethod = AccessTools.PropertyGetter(MultiSelectType, "LoadUnloadSerializer");
                        LoadUnloadSerializerGetter = AccessTools.MethodDelegate<Func<object>>(LoadUnloadSerializerMethod);
                        _stopLoadingMethod = AccessTools.Method(MultiSelectType, "StopLoading");
                    }
                }
            }

            return UIFixesLoaded.Value;
        }
    }
}