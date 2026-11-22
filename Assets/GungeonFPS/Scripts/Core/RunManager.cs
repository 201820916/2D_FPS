using System.Collections.Generic;
using UnityEngine;
using GungeonFPS.Player;
using GungeonFPS.Weapons;

namespace GungeonFPS.Core
{
    public static class RunManager
    {
        public static int CurrentFloor { get; private set; } = 1;
        public static int TotalKills { get; private set; }
        public static bool HasCarriedState { get; private set; }

        public static WeaponData CarriedMain { get; private set; }
        public static SecondaryWeaponData CarriedSecondary { get; private set; }
        public static List<InventoryItem> CarriedBag { get; private set; } = new();
        public static int CarriedCore { get; private set; }
        public static int CarriedHP { get; private set; }
        public static int CarriedBlanks { get; private set; }

        public static float EnemyHealthMultiplier => 1f + (CurrentFloor - 1) * 0.35f;
        public static int ExtraRooms => Mathf.Min(4, CurrentFloor - 1);

        public static void RegisterKill()
        {
            TotalKills++;
        }

        public static void AdvanceToNextFloor(GameObject playerObj)
        {
            if (playerObj != null)
            {
                var wc = playerObj.GetComponent<WeaponController>();
                var sc = playerObj.GetComponent<SecondaryWeaponController>();
                var inv = playerObj.GetComponent<InventorySystem>();
                var hp = playerObj.GetComponent<PlayerHealth>();
                var pc = playerObj.GetComponent<PlayerController>();

                CarriedMain = wc != null ? wc.CurrentWeapon : null;
                CarriedSecondary = sc != null ? sc.CurrentSecondary : null;
                CarriedCore = inv != null ? inv.CoreCurrency : 0;
                CarriedHP = hp != null ? hp.CurrentHealth : 6;
                CarriedBlanks = pc != null ? pc.BlanksCount : 2;

                CarriedBag.Clear();
                if (inv != null)
                {
                    foreach (var item in inv.Items)
                    {
                        CarriedBag.Add(new InventoryItem
                        {
                            mainWeapon = item.mainWeapon,
                            secondaryWeapon = item.secondaryWeapon
                        });
                    }
                }
                HasCarriedState = true;
            }

            CurrentFloor++;
        }

        public static void RestoreCarriedState(GameObject playerObj)
        {
            if (!HasCarriedState || playerObj == null) return;

            var wc = playerObj.GetComponent<WeaponController>();
            var sc = playerObj.GetComponent<SecondaryWeaponController>();
            var inv = playerObj.GetComponent<InventorySystem>();
            var hp = playerObj.GetComponent<PlayerHealth>();
            var pc = playerObj.GetComponent<PlayerController>();

            if (wc != null && CarriedMain != null) wc.EquipMainWeapon(CarriedMain);
            if (sc != null && CarriedSecondary != null) sc.EquipSecondary(CarriedSecondary);
            if (hp != null) hp.SetHealth(CarriedHP);
            if (pc != null) pc.SetBlanks(CarriedBlanks);

            if (inv != null)
            {
                inv.AddCurrency(CarriedCore);
                foreach (var item in CarriedBag)
                {
                    if (item.mainWeapon != null) inv.TryAddMain(item.mainWeapon);
                    else if (item.secondaryWeapon != null) inv.TryAddSecondary(item.secondaryWeapon);
                }
            }
        }

        public static void ResetRun()
        {
            CurrentFloor = 1;
            TotalKills = 0;
            HasCarriedState = false;
            CarriedMain = null;
            CarriedSecondary = null;
            CarriedBag.Clear();
            CarriedCore = 0;
            CarriedHP = 0;
            CarriedBlanks = 0;
        }
    }
}
