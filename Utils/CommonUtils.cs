using System;
using Comfort.Common;
using EFT.InputSystem;
using EFT.UI;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete

namespace ContinuousLoadAmmo.Utils;

public static class CommonUtils
{
    private static Transform _eftBattleUIScreenTransform;

    public static Transform EftBattleUIScreenTransform
    {
        get
        {
            if (_eftBattleUIScreenTransform != null) return _eftBattleUIScreenTransform;

            _eftBattleUIScreenTransform = Singleton<CommonUI>.Instance.EftBattleUIScreen.transform;
            return _eftBattleUIScreenTransform;
        }
    }

    private static InputTree _inputTree;

    public static InputTree InputTree
    {
        get
        {
            // Thanks Lacyway!
            if (_inputTree != null) return _inputTree;

            var inputObj = GameObject.Find("___Input");
            if (inputObj == null)
            {
                throw new NullReferenceException("Could not find InputTree object!");
            }

            _inputTree = inputObj.GetComponent<InputTree>();
            return _inputTree;
        }
    }

    public static bool CheckIfAnyDifferentCaliber(this MagazineItemClass magazine, AmmoItemClass ammo)
    {
        foreach (var cartridge in magazine.Cartridges.Items_1)
        {
            if (cartridge is not AmmoItemClass cartridgeAmmo) continue;
            if (cartridgeAmmo.Caliber != ammo.Caliber) return true;
        }
        return false;
    }

    public static bool InRaid => GClass2340.InRaid;
}
