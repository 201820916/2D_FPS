using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GungeonFPS.Core;
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

            bool pickMain = mainWeaponPool.Count > 0 && (secondaryWeaponPool.Count == 0 || Random.value < 0.6f);
            if (pickMain)
            {
                WeaponData chosen = mainWeaponPool[Random.Range(0, mainWeaponPool.Count)];
                pickup.SetupMain(chosen);
            }
            else if (secondaryWeaponPool.Count > 0)
            {
                SecondaryWeaponData chosen = secondaryWeaponPool[Random.Range(0, secondaryWeaponPool.Count)];
                pickup.SetupSecondary(chosen);
            }
        }
    }
}
