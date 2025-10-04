using Comfort.Common;
using ContinuousLoadAmmo.Controllers;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static EFT.Player;
using static EFT.Player.PlayerInventoryController;

namespace ContinuousLoadAmmo.Components
{
    internal class LoadAmmoComponent : MonoBehaviour
    {
        public static Player MainPlayer;
        private InventoryController InventoryController;

        private static readonly FieldInfo interfaceFieldInfo = typeof(PlayerInventoryController).GetField("interface17_0", BindingFlags.Instance | BindingFlags.NonPublic);

        protected void Awake()
        {
            MainPlayer = (Player)Singleton<GameWorld>.Instance.MainPlayer;
            InventoryController = MainPlayer.InventoryController;
            if (MainPlayer == null)
            {
                Plugin.LogSource.LogError("Unable to find Player, destroying component");
                Destroy(this);
            }

            if (!MainPlayer.IsYourPlayer)
            {
                Plugin.LogSource.LogError("MainPlayer is not your player, destroying component");
                Destroy(this);
            }
        }

        protected void Update()
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if (MainPlayer == null)
            {
                return;
            }

            if (!MainPlayer.IsInventoryOpened && Input.GetKeyDown(Plugin.LoadAmmoHotkey.Value.MainKey))
            {
                TryLoadAmmo();
            }
        }

        public async void TryLoadAmmo()
        {
            try
            {
                var playerInventoryController = InventoryController as PlayerInventoryController;
                if (LoadAmmo.IsLoadingAmmo || playerInventoryController.HasAnyHandsAction())
                {
                    return;
                }
                if (FindMagAmmoFromEquipment(out AmmoItemClass ammo, out MagazineItemClass magazine))
                {
                    int loadCount = Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);

                    float loadSpeedModifier = 100f - MainPlayer.Profile.Skills.MagDrillsLoadSpeed + magazine.LoadUnloadModifier;
                    float loadTime = Singleton<BackendConfigSettingsClass>.Instance.BaseLoadTime * loadSpeedModifier / 100f;
                    var loadAmmoTask = NewLoadAmmoProcess(ammo, magazine, loadCount, loadTime, false);
                    if (loadAmmoTask != null)
                    {
                        interfaceFieldInfo.SetValue(playerInventoryController, loadAmmoTask);

                        var startLoadAmmoTask = loadAmmoTask.Start();
                        LoadAmmo.SetPlayerState(true);
                        LoadAmmoUI.Show();
                        LoadAmmo.ListenForCancel();

                        await startLoadAmmoTask;
                        interfaceFieldInfo.SetValue(playerInventoryController, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError(ex);
            }
        }

        public Class1085 NewLoadAmmoProcess(AmmoItemClass sourceAmmo, MagazineItemClass magazine, int loadCount, float loadTime, bool ignoreRestrictions)
        {
            if (loadCount <= 0)
            {
                Plugin.LogSource.LogError("Cannot load 0 bullets");
                return null;
            }
            InventoryController.StopProcesses();

            GStruct454 simulate = ignoreRestrictions ? magazine.ApplyWithoutRestrictions(InventoryController, sourceAmmo, 1, true) : magazine.Apply(InventoryController, sourceAmmo, 1, true);
            if (simulate.Failed && !InventoryController.CanExecute(simulate.Value))
            {
                Plugin.LogSource.LogError("Simulation to load ammo failed");
                return null;
            }
            //this is what fails in PlayerInventoryController.LoadMagazine(), next process is locked
            //var readyResult = await inventoryController.method_30();
            //if (readyResult.Failed)
            //{
            //    Plugin.LogSource.LogError($"[ContinuousLoadAmmo] Ready check: {readyResult.Error}");
            //}

            return new(InventoryController, magazine, sourceAmmo, loadCount, MainPlayer.Profile.Skills.MagDrillsLoadProgression, loadTime);
        }

        public bool FindMagAmmoFromEquipment(out AmmoItemClass ammo, out MagazineItemClass magazine)
        {
            ammo = null;
            magazine = null;
            StringBuilder sb = new();

            // Get Ammo
            MagazineItemClass currentMagazine = null;
            List<AmmoItemClass> reachableAmmos = new();
            AmmoItemClass chosenAmmo = null;
            if (MainPlayer.LastEquippedWeaponOrKnifeItem is Weapon weapon)
            {
                sb.Append($"Weapon: {weapon}. ");
                currentMagazine = weapon.GetCurrentMagazine();
                if (currentMagazine != null)
                {
                    InventoryController.GetAcceptableItemsNonAlloc(
                        ReachableSlots,
                        reachableAmmos,
                        item => item is AmmoItemClass ammo && currentMagazine.CheckCompatibility(ammo)
                        );
                }
                else
                {   // fallback if no magazine
                    string ammoCaliber = weapon.AmmoCaliber;
                    InventoryController.GetAcceptableItemsNonAlloc(
                        ReachableSlots,
                        reachableAmmos,
                        item => item is AmmoItemClass ammo && ammo.Caliber == ammoCaliber
                        );
                }
            }
            if (reachableAmmos.Count > 0)
            {
                reachableAmmos.Sort((a, b) =>
                {
                    int result = b.PenetrationPower.CompareTo(a.PenetrationPower);
                    if (result == 0)
                    {
                        result = b.StackObjectsCount.CompareTo(a.StackObjectsCount);
                    }
                    return result;
                }); // sort penetration power highest to lowest
                if (!Plugin.PrioritizeHighestPenetration.Value && currentMagazine != null)
                {
                    foreach (var currAmmo in reachableAmmos)
                    {
                        if (currentMagazine.FirstRealAmmo() is AmmoItemClass ammoInsideMag && ammoInsideMag.Name == currAmmo.Name)
                        {
                            sb.Append("Found same ammo. ");
                            ammo = chosenAmmo = currAmmo;
                            break;
                        }
                    }
                    if (ammo == null)
                    {
                        sb.Append("No same ammo from magazine found, fallback to ammo with highest penetration. ");
                        ammo = chosenAmmo = reachableAmmos[0];
                    }
                }
                else
                {
                    sb.Append("Choosing ammo with highest penetration. ");
                    ammo = chosenAmmo = reachableAmmos[0];
                }
            }
            else
            {
                sb.Append("No ammo found.");
                Plugin.LogSource.LogDebug(sb.ToString());
                return false;
            }
            sb.Append($"Ammo {ammo.LocalizedShortName()}. ");

            // Get Magazine
            List<MagazineItemClass> reachableMagazines = new();
            InventoryController.GetAcceptableItemsNonAlloc(ReachableSlots, reachableMagazines,
                item => item is MagazineItemClass mag && mag.Count != mag.MaxCount && mag.CheckCompatibility(chosenAmmo)
                );
            if (reachableMagazines.Count > 0)
            {
                // Sort by almost full
                reachableMagazines.Sort((a, b) =>
                    (b.MaxCount - b.Count).CompareTo(a.MaxCount - a.Count)
                    );
                magazine = reachableMagazines[0];
                sb.Append($"Magazine {magazine.LocalizedShortName()}");
                Plugin.LogSource.LogDebug(sb.ToString());
                return true;
            }
            sb.Append("No magazine found.");
            Plugin.LogSource.LogDebug(sb.ToString());
            return false;
        }

        public static EquipmentSlot[] ReachableSlots => Plugin.ReachableOnly.Value ? ReachableOnly : ReachableAll;
        private static readonly EquipmentSlot[] ReachableOnly = Inventory.FastAccessSlots.AddToArray(EquipmentSlot.SecuredContainer);
        private static readonly EquipmentSlot[] ReachableAll = Inventory.FastAccessSlots.AddRangeToArray([EquipmentSlot.ArmorVest, EquipmentSlot.Backpack, EquipmentSlot.SecuredContainer]);
    }
}
