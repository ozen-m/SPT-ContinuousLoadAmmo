using System.Reflection;
using ContinuousLoadAmmo.Utils;
using EFT.Communications;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

/// <summary>
/// Disable magazine presets window in-raid
/// </summary>
public class SetSubInteractionsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ItemUiContext).GetMethod(nameof(ItemUiContext.ShowMagPresetsWindow));
    }

    [PatchPrefix]
    protected static bool Prefix(ref GClass3829 __result)
    {
        if (!CommonUtils.InRaid) return true;

        CommonUtils.DisplayNotification("Cannot create a new magazine preset while in-raid", ENotificationIconType.Alert, true);
        __result = null;
        return false;
    }
}
