using Comfort.Common;
using ContinuousLoadAmmo.Controllers;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
                var playerInventoryController = InventoryController as Player.PlayerInventoryController;
                if (playerInventoryController.HasAnyHandsAction() || LoadAmmo.IsLoadingAmmo)
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
            EquipmentSlot[] reachableSlots = GetReachableSlots();
            AmmoItemClass chosenAmmo = null;
            ammo = null;
            magazine = null;

            // TODO: fallbacks
            // Get Ammo
            // Priority is: Ammo of current weapon magazine
            Weapon lastWeapon = MainPlayer.LastEquippedWeaponOrKnifeItem as Weapon;
            //var lastWeapon = MainPlayer.Inventory.;
            if (lastWeapon != null)
            {
                MagazineItemClass currMagazine = lastWeapon.GetCurrentMagazine();
                //lastWeapon.CompatibleAmmo

                List<AmmoItemClass> reachableAmmos = new();
                InventoryController.GetAcceptableItemsNonAlloc(reachableSlots, reachableAmmos,
                    item => currMagazine.CheckCompatibility(item)
                    );
                if (reachableAmmos.Count > 0)
                {
                    foreach (var currAmmo in reachableAmmos)
                    {
                        AmmoItemClass ammoInsideMag = (AmmoItemClass)currMagazine.FirstRealAmmo();
                        if (ammoInsideMag != null && ammoInsideMag.Name == currAmmo.Name)
                        {
                            ammo = currAmmo;
                            chosenAmmo = currAmmo;
                            break;
                        }
                    }
                    ammo ??= reachableAmmos[0];
                    chosenAmmo ??= reachableAmmos[0];
                }
                else return false;
            }

            // Get Magazine
            List<MagazineItemClass> reachableMagazines = new List<MagazineItemClass>();
            InventoryController.GetAcceptableItemsNonAlloc(reachableSlots, reachableMagazines,
                item => item.CheckCompatibility(chosenAmmo) && item.Count != item.MaxCount
                );
            if (reachableMagazines.Count > 0)
            {
                // Do almost filled magazine
                reachableMagazines.Sort((a, b) => a.Count.CompareTo(b.Count));
                magazine = reachableMagazines[^1];
                return true;
            }
            return false;
        }

        public static EquipmentSlot[] GetReachableSlots() => Plugin.ReachableOnly.Value ? ReachableOnly : ReachableAll;
        private static readonly EquipmentSlot[] ReachableOnly = Inventory.FastAccessSlots.AddToArray(EquipmentSlot.SecuredContainer);
        private static readonly EquipmentSlot[] ReachableAll = Inventory.FastAccessSlots.AddRangeToArray([EquipmentSlot.ArmorVest, EquipmentSlot.Backpack, EquipmentSlot.SecuredContainer]);
    }
}
