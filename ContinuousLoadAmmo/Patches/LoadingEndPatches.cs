using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using ContinuousLoadAmmo.Utils;
using EFT.InventoryLogic;
using HarmonyLib;
using JetBrains.Annotations;

#pragma warning disable VSTHRD003

namespace ContinuousLoadAmmo.Patches;

public class LoadingEndPatches
{
    public static event Action OnLoadingEnd;
    private static Harmony _harmony;

    public static void Enable()
    {
        _harmony?.UnpatchSelf();
        _harmony = new Harmony($"{nameof(ContinuousLoadAmmo)}.{nameof(LoadingEndPatches)})");
        _harmony.CreateClassProcessor(typeof(LoadingEndPatches), true).Patch();
        L.Info($"Enabled patch {nameof(LoadingEndPatches)}");
    }

    public static void Disable()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
        L.Info($"Disabled patch {nameof(LoadingEndPatches)}");
    }

    /// <summary>
    /// Get all methods implementing <see cref="IMagazineLoadingProcess.Start"/> and patch those
    /// </summary>
    [UsedImplicitly]
    private static List<MethodBase> TargetMethods()
    {
        var methods = new List<MethodBase>();

        var interfaceType = typeof(IMagazineLoadingProcess);
        var interfaceStartMethod = interfaceType.GetMethod(nameof(IMagazineLoadingProcess.Start));

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    continue;
                }
                if (!interfaceType.IsAssignableFrom(type))
                {
                    continue;
                }

                var interfaceMap = type.GetInterfaceMap(interfaceType);
                var startMethodIndex = Array.IndexOf(interfaceMap.InterfaceMethods, interfaceStartMethod);
                if (startMethodIndex != -1)
                {
                    methods.Add(interfaceMap.TargetMethods[startMethodIndex]);
                }
            }
        }

        foreach (var method in methods)
        {
            L.Info($"Found {method.DeclaringType}.{method.Name}");
        }
        return methods;
    }

    [UsedImplicitly]
    public static void Postfix(Task<IResult> __result)
    {
        _ = WaitForEndAsync(__result);
    }

    private static async Task WaitForEndAsync(Task<IResult> __result)
    {
        await __result;
        OnLoadingEnd?.Invoke();
    }
}
