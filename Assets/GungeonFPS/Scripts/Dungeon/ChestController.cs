using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GungeonFPS.Core;
using GungeonFPS.Player;
using GungeonFPS.UI;
using GungeonFPS.Weapons;

namespace GungeonFPS.Dungeon
{
    public class ChestController : MonoBehaviour
    {
        [Header("Loot Pool")]
        [SerializeField] private List<WeaponData> mainWeaponPool = new();
        [SerializeField] private List<SecondaryWeaponData> secondaryWeaponPool = new();
        [SerializeField] private GameObject weaponPickupPrefab;

        [Header("Visuals & Interaction")]
        [SerializeField] private Transform lidTransform;
        [SerializeField] private float interactRadius = 3.0f;

        private bool _isOpened;
        private Transform _player;

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void Update()
        {
            if (_isOpened || _player == null || Keyboard.current == null) return;

            float distSq = (_player.position - transform.position).sqrMagnitude;
            if (distSq <= interactRadius * interactRadius)
            {
                GameHUD.Instance?.RequestInteractPrompt("[E] OPEN NEON CHEST");
                if (Keyboard.current.eKey.wasPressedThisFrame)
                    OpenChest();
            }
        }

        private void OpenChest()
        {
            _isOpened = true;
            if (lidTransform != null)
                lidTransform.localRotation = Quaternion.Euler(-55f, 0f, 0f);

            RetroAudio.Instance?.Play(SfxType.DoorOpen);
            VfxSpawner.Instance?.SpawnBurst(transform.position + Vector3.up * 0.8f, new Color(0.25f, 0.95f, 1f), 18, 7f, 0.16f);

            if (weaponPickupPrefab == null) return;

            Vector3 spawnPos = transform.position + Vector3.up * 1.1f;
            GameObject go = Instantiate(weaponPickupPrefab, spawnPos, Quaternion.identity);
            if (!go.TryGetComponent<WeaponPickup>(out var pickup)) return;

            var unownedMain = GetUnownedMainList();
            var unownedSec = GetUnownedSecondaryList();
            int totalUnowned = unownedMain.Count + unownedSec.Count;

            if (totalUnowned > 0)
            {
                int roll = Random.Range(0, totalUnowned);
                if (roll < unownedMain.Count)
                    pickup.SetupMain(unownedMain[roll]);
                else
                    pickup.SetupSecondary(unownedSec[roll - unownedMain.Count]);
                return;
            }

            int totalAll = mainWeaponPool.Count + secondaryWeaponPool.Count;
            if (totalAll > 0)
            {
                int roll = Random.Range(0, totalAll);
                if (roll < mainWeaponPool.Count)
                    pickup.SetupMain(mainWeaponPool[roll]);
                else
                    pickup.SetupSecondary(secondaryWeaponPool[roll - mainWeaponPool.Count]);
            }
        }

        private List<WeaponData> GetUnownedMainList()
        {
            var result = new List<WeaponData>();
            var wc = _player != null ? _player.GetComponent<WeaponController>() : null;
            var inv = _player != null ? _player.GetComponent<InventorySystem>() : null;

            foreach (var w in mainWeaponPool)
            {
                if (w == null) continue;
                if (wc != null && wc.CurrentWeapon == w) continue;
                bool inBag = false;
                if (inv != null)
                {
                    foreach (var item in inv.Items)
                        if (item.mainWeapon == w) { inBag = true; break; }
                }
                if (!inBag) result.Add(w);
            }
            return result;
        }

        private List<SecondaryWeaponData> GetUnownedSecondaryList()
        {
            var result = new List<SecondaryWeaponData>();
            var sc = _player != null ? _player.GetComponent<SecondaryWeaponController>() : null;
            var inv = _player != null ? _player.GetComponent<InventorySystem>() : null;

            foreach (var s in secondaryWeaponPool)
            {
                if (s == null) continue;
                if (sc != null && sc.CurrentSecondary == s) continue;
                bool inBag = false;
                if (inv != null)
                {
                    foreach (var item in inv.Items)
                        if (item.secondaryWeapon == s) { inBag = true; break; }
                }
                if (!inBag) result.Add(s);
            }
            return result;
        }
    }
}
