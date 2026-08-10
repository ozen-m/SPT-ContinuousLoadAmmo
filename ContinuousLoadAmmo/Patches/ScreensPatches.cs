using System.Reflection;
using ContinuousLoadAmmo.Utils;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Map;
using SPT.Reflection.Patching;

namespace ContinuousLoadAmmo.Patches;

public static class ScreensPatches
{
    private static bool _toSkip;

    private static void Pre()
    {
        if (ContinuousLoadAmmo.InventoryTabs.Value)
        {
            _toSkip = true;
        }
    }

    private static void Post()
    {
        if (ContinuousLoadAmmo.InventoryTabs.Value)
        {
            _toSkip = false;
        }
    }

    public class TasksScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TasksScreen).GetMethod(nameof(TasksScreen.Show));
        }

        [PatchPrefix]
        protected static void Prefix()
        {
            Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            Post();
        }
    }

    public class ItemsPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemsPanel).GetMethod(nameof(ItemsPanel.Show));
        }

        [PatchPrefix]
        protected static void Prefix()
        {
            Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            Post();
        }
    }

    public class MapScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MapScreen).GetMethod(nameof(MapScreen.Show));
        }

        [PatchPrefix]
        protected static void Prefix()
        {
            Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            Post();
        }
    }

    public class PlayerModelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryPlayerModelWithStatsWindow).GetMethod(
                nameof(InventoryPlayerModelWithStatsWindow.Show),
                [
                    typeof(GInterface214), typeof(int), typeof(EMemberCategory), typeof(ProfileStats), typeof(LastPlayerStateClass),
                    typeof(InventoryController), typeof(bool),
                ]
            );
        }

        [PatchPrefix]
        protected static void Prefix()
        {
            Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            Post();
        }
    }

    public class SkillsAndMasteringPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SkillsAndMasteringScreen).GetMethod(nameof(SkillsAndMasteringScreen.Show));
        }

        [PatchPrefix]
        protected static void Prefix()
        {
            Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            Post();
        }
    }

    /// <summary>
    /// Skip StopProcesses when called from Screens
    /// </summary>
    public class StopProcessesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.PlayerInventoryController).GetMethod(nameof(Player.PlayerInventoryController.StopProcesses));
        }

        [PatchPrefix]
        protected static bool Prefix()
        {
            if (_toSkip)
            {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// MultiSelect patches StopProcesses to run MultiSelect.StopLoading.
    /// Even if StopProcesses is skipped, MultiSelect's patch still runs to call StopLoading
    /// </summary>
    [IgnoreAutoPatch]
    public class MultiSelectStopLoadingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return MultiSelectInterop.StopLoadingMethod;
        }

        [PatchPrefix]
        protected static bool Prefix()
        {
            if (_toSkip)
            {
                return false;
            }
            return true;
        }
    }
}
