using System;
using System.Reflection;
using ContinuousLoadAmmo.Utils;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ContinuousLoadAmmo.Patches;

/// <summary>
/// Cancel mag preset loading when loading is canceled through ItemView click
/// </summary>
public class OnClickPatch : ModulePatch
{
    public static event Action CancelPresetLoaderOnClick;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ItemView).GetMethod(nameof(ItemView.OnClick));
    }

    [PatchPrefix]
    protected static void Prefix(ItemView __instance, PointerEventData.InputButton button)
    {
        if (!CommonUtils.InRaid || button != PointerEventData.InputButton.Left) return;

        if (Input.GetKey(KeyCode.LeftControl) // Modifier control
            || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftShift) // Modifier shift
            || Input.GetKey(KeyCode.RightShift))
        {
            return;
        }

        if (__instance.IsBeingLoadedMagazine.Value || __instance.IsBeingUnloadedMagazine.Value)
        {
            CancelPresetLoaderOnClick?.Invoke();
        }
    }
}
