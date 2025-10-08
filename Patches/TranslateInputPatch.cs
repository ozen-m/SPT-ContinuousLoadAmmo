using ContinuousLoadAmmo.Components;
using EFT.InputSystem;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace ContinuousLoadAmmo.Patches
{
    public class TranslateInputPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(UIInputRoot).GetMethod(nameof(UIInputRoot.TranslateCommand));
        }

        /// <summary>
        /// Blocks other input when AmmoSelector is active
        /// </summary>
        [PatchPostfix]
        protected static void Postfix(ref InputNode.ETranslateResult __result)
        {
            if (!Plugin.InRaid) return;
            if (LoadAmmo.Inst.AmmoSelectorActive || (Input.GetKey(Plugin.LoadAmmoHotkey.Value.MainKey) && Input.mouseScrollDelta.y != 0))
            {
                __result = InputNode.ETranslateResult.Block;
            }
        }
    }
}
