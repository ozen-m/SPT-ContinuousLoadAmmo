using Comfort.Common;
using ContinuousLoadAmmo.Controllers;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using static EFT.Player;
using static EFT.Player.PlayerInventoryController;

namespace ContinuousLoadAmmo.Components
{
    internal class LoadAmmoComponent : MonoBehaviour
    {
        private Player MainPlayer;

        protected void Awake()
        {
            MainPlayer = (Player)Singleton<GameWorld>.Instance.MainPlayer;

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

            if (Plugin.LoadAmmoHotkey.Value.IsDown())
            {
                Plugin.LogSource.LogError("Hotkey down");
                TryLoadAmmo();
            }
        }

        public async void TryLoadAmmo()
        {
            try
            {
                var playerInventoryController = MainPlayer.InventoryController as Player.PlayerInventoryController;
                if (playerInventoryController.HasAnyHandsAction() || LoadAmmo.IsLoadingAmmo)
                {
                    return;
                }
                if (FindMagAmmoFromEquipment(out AmmoItemClass ammo, out MagazineItemClass magazine))
                {
                    //Plugin.LogSource.LogError($"ammo {ammo.Name}, magazine {magazine.Name}");
                    int loadCount = Mathf.Min(ammo.StackObjectsCount, magazine.MaxCount - magazine.Count);

                    float loadSpeedModifier = 100f - MainPlayer.Profile.Skills.MagDrillsLoadSpeed + magazine.LoadUnloadModifier;
                    float loadTime = Singleton<BackendConfigSettingsClass>.Instance.BaseLoadTime * loadSpeedModifier / 100f;
                    var loadAmmoTask = await LoadAmmoIntoMagazine(playerInventoryController, ammo, magazine, loadCount, loadTime, false);
                    if (loadAmmoTask != null)
                    {
                        FieldInfo interfaceFieldInfo = typeof(PlayerInventoryController).GetField("interface17_0", BindingFlags.Instance | BindingFlags.NonPublic);
                        interfaceFieldInfo.SetValue(playerInventoryController, loadAmmoTask);

                        var startLoadAmmoTask = loadAmmoTask.Start();
                        LoadAmmo.IsOutsideInventory = true;
                        LoadAmmo.SetPlayerState(true);
                        GEventArgs7 activeEvent = new(ammo, magazine, loadCount, loadTime, CommandStatus.Begin, playerInventoryController);
                        LoadAmmoUI.CreateUI(playerInventoryController, activeEvent);
                        LoadAmmoUI.Show();
                        LoadAmmo.ListenForCancel(playerInventoryController);

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

        public async Task<Class1085> LoadAmmoIntoMagazine(Player.PlayerInventoryController inventoryController, AmmoItemClass sourceAmmo, MagazineItemClass magazine, int loadCount, float loadTime, bool ignoreRestrictions)
        {
            if (loadCount <= 0)
            {
                Plugin.LogSource.LogError("[ContinuousLoadAmmo] Cannot load 0 bullets");
                return null;
            }
            inventoryController.StopProcesses();

            GStruct454 simulate = ignoreRestrictions ? magazine.ApplyWithoutRestrictions(inventoryController, sourceAmmo, 1, true) : magazine.Apply(inventoryController, sourceAmmo, 1, true);
            if (simulate.Failed && !inventoryController.CanExecute(simulate.Value))
            {
                Plugin.LogSource.LogError("[ContinuousLoadAmmo] Simulation to load ammo failed");
                return null;
            }
            var readyResult = await inventoryController.method_30();
            if (readyResult.Failed)
            {
                Plugin.LogSource.LogError($"[ContinuousLoadAmmo] Ready check: {readyResult.Error}");
            }

            return new(inventoryController, magazine, sourceAmmo, loadCount, MainPlayer.Profile.Skills.MagDrillsLoadProgression, loadTime);
        }

        public bool FindMagAmmoFromEquipment(out AmmoItemClass ammo, out MagazineItemClass magazine)
        {
            var playerInventoryController = MainPlayer.InventoryController;
            EquipmentSlot[] reachableSlots = GetReachableSlots();
            AmmoItemClass chosenAmmo = null;
            ammo = null;
            magazine = null;

            // TODO: fallbacks
            // Get Ammo
            var lastWeapon = MainPlayer.LastEquippedWeaponOrKnifeItem;
            if (lastWeapon is Weapon)
            {
                MagazineItemClass currMagazine = lastWeapon.GetCurrentMagazine();
                AmmoItemClass ammoInsideMag = (AmmoItemClass)currMagazine.FirstRealAmmo();
                if (ammoInsideMag != null)
                {
                    List<AmmoItemClass> reachableAmmos = new();
                    playerInventoryController.GetAcceptableItemsNonAlloc(reachableSlots, reachableAmmos,
                        item => currMagazine.CheckCompatibility(item)
                        );
                    if (reachableAmmos.Count > 0)
                    {
                        foreach (var currAmmo in reachableAmmos)
                        {
                            if (ammoInsideMag.Name == currAmmo.Name)
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
            }

            // Get Magazine
            List<MagazineItemClass> reachableMagazines = new List<MagazineItemClass>();
            playerInventoryController.GetAcceptableItemsNonAlloc(reachableSlots, reachableMagazines,
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
