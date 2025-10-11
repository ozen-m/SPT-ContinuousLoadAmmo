using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Map;
using SPT.Reflection.Patching;
using System.Reflection;
using UIFixesInterop;

namespace ContinuousLoadAmmo.Patches
{
    public static class ScreensPatches
    {
        public static bool ToSkip;

        public static void Enable()
        {
            new TasksScreenShowPatch().Enable();
            new ItemsPanelShowPatch().Enable();
            new MapScreenPatch().Enable();
            new PlayerModelPatch().Enable();
            new SkillsAndMasteringPatch().Enable();
            new StopProcessesPatch().Enable();

            if (MultiSelect.StopLoadingMethod != null)
            {
                new MultiSelectStopLoadingPatch().Enable();
            }
        }

        public static void Pre()
        {
            if (Plugin.InventoryTabs.Value)
            {
                ToSkip = true;
            }
        }

        public static void Post()
        {
            if (Plugin.InventoryTabs.Value)
            {
                ToSkip = false;
            }
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
            ScreensPatches.Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            ScreensPatches.Post();
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
            ScreensPatches.Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            ScreensPatches.Post();
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
            ScreensPatches.Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            ScreensPatches.Post();
        }
    }

    public class PlayerModelPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryPlayerModelWithStatsWindow).GetMethod(nameof(InventoryPlayerModelWithStatsWindow.Show), [typeof(GInterface214), typeof(int), typeof(EMemberCategory), typeof(ProfileStats), typeof(LastPlayerStateClass), typeof(InventoryController), typeof(bool)]);
        }

        [PatchPrefix]
        protected static void Prefix()
        {
            ScreensPatches.Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            ScreensPatches.Post();
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
            ScreensPatches.Pre();
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            ScreensPatches.Post();
        }
    }

    /// <summary>
    /// Skip StopProcesses when called from Screens.
    /// Other option: Get callstack and identify caller to skip StopProcesses
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
            if (ScreensPatches.ToSkip)
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
    public class MultiSelectStopLoadingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return MultiSelect.StopLoadingMethod;
        }

        [PatchPrefix]
        protected static bool Prefix()
        {
            if (ScreensPatches.ToSkip)
            {
                return false;
            }
            return true;
        }
    }
}
