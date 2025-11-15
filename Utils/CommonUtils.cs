using System;
using EFT.InputSystem;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete

namespace ContinuousLoadAmmo.Utils;

public static class CommonUtils
{
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

    public static bool InRaid => GClass2340.InRaid;
}
