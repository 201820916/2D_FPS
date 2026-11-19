using UnityEngine;
using UnityEngine.InputSystem;
using GungeonFPS.Core;
using GungeonFPS.Player;
using GungeonFPS.UI;
using GungeonFPS.Weapons;

namespace GungeonFPS.Dungeon
{
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponData mainWeapon;
        [SerializeField] private SecondaryWeaponData secondaryWeapon;
        [SerializeField] private float interactRadius = 2.6f;
        [SerializeField] private Renderer visualRenderer;

        private Vector3 _basePos;
        private float _bobTimer;
        private Transform _player;

        public void SetupMain(WeaponData data)
        {
            mainWeapon = data;
            secondaryWeapon = null;
            RefreshColor();
        }

        public void SetupSecondary(SecondaryWeaponData data)
        {
            secondaryWeapon = data;
            mainWeapon = null;
            RefreshColor();
        }

        private void Start()
        {
            _basePos = transform.position;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            RefreshColor();
        }

        private void RefreshColor()
        {
            if (visualRenderer == null) visualRenderer = GetComponentInChildren<Renderer>();
            if (visualRenderer == null) return;

            Color c = mainWeapon != null
                ? mainWeapon.projectileColor
                : (secondaryWeapon != null ? secondaryWeapon.accentColor : Color.cyan);

            visualRenderer.material.color = c;
            if (visualRenderer.material.HasProperty("_EmissionColor"))
                visualRenderer.material.SetColor("_EmissionColor", c * 2f);
        }

        private void Update()
        {
            _bobTimer += Time.deltaTime * 3f;
            transform.position = _basePos + Vector3.up * (Mathf.Sin(_bobTimer) * 0.16f);
            transform.Rotate(Vector3.up, 75f * Time.deltaTime, Space.World);

            if (_player == null || Keyboard.current == null) return;

            float distSq = (_player.position - transform.position).sqrMagnitude;
            if (distSq <= interactRadius * interactRadius)
            {
                bool hasBagSpace = _player.TryGetComponent<InventorySystem>(out var inv) && inv.HasSpace;
                string actionWord = hasBagSpace ? "STORE IN BAG" : "SWAP";
                string label = mainWeapon != null
                    ? $"[E] {actionWord} (MAIN): {mainWeapon.weaponName}"
                    : (secondaryWeapon != null ? $"[E] {actionWord} (SEC): {secondaryWeapon.weaponName}" : "");

                if (!string.IsNullOrEmpty(label))
                    GameHUD.Instance?.RequestInteractPrompt(label);

                if (Keyboard.current.eKey.wasPressedThisFrame)
                    TryPickupOrSwap(inv);
            }
        }

        private void TryPickupOrSwap(InventorySystem inv)
        {
            if (mainWeapon != null)
            {
                if (inv != null && inv.TryAddMain(mainWeapon))
                {
                    RetroAudio.Instance?.Play(SfxType.Pickup);
                    VfxSpawner.Instance?.SpawnBurst(transform.position, mainWeapon.projectileColor, 12, 6f, 0.15f);
                    Destroy(gameObject);
                    return;
                }

                if (_player.TryGetComponent<WeaponController>(out var wc))
                {
                    WeaponData prev = wc.EquipMainWeapon(mainWeapon);
                    RetroAudio.Instance?.Play(SfxType.Pickup);
                    VfxSpawner.Instance?.SpawnBurst(transform.position, mainWeapon.projectileColor, 12, 6f, 0.15f);

                    if (prev != null && prev != mainWeapon)
                    {
                        SetupMain(prev);
                        return;
                    }
                    Destroy(gameObject);
                }
            }
            else if (secondaryWeapon != null)
            {
                if (inv != null && inv.TryAddSecondary(secondaryWeapon))
                {
                    RetroAudio.Instance?.Play(SfxType.Pickup);
                    VfxSpawner.Instance?.SpawnBurst(transform.position, secondaryWeapon.accentColor, 12, 6f, 0.15f);
                    Destroy(gameObject);
                    return;
                }

                if (_player.TryGetComponent<SecondaryWeaponController>(out var sc))
                {
                    SecondaryWeaponData prev = sc.EquipSecondary(secondaryWeapon);
                    RetroAudio.Instance?.Play(SfxType.Pickup);
                    VfxSpawner.Instance?.SpawnBurst(transform.position, secondaryWeapon.accentColor, 12, 6f, 0.15f);

                    if (prev != null && prev != secondaryWeapon)
                    {
                        SetupSecondary(prev);
                        return;
                    }
                    Destroy(gameObject);
                }
            }
        }
    }
}
