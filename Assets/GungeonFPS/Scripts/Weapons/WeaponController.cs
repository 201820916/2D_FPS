using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GungeonFPS.Core;
using GungeonFPS.Player;

namespace GungeonFPS.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private List<WeaponData> weapons = new();
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform fireOrigin;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerController playerController;

        [Header("2D HUD Weapon (Optional)")]
        [SerializeField] private Image hudWeaponImage;
        [SerializeField] private RectTransform hudWeaponRect;
        [SerializeField] private Image hudMuzzleFlash;

        [Header("3D Viewmodel Weapon (Optional)")]
        [SerializeField] private Transform viewmodelRoot;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private Material gunCoreMaterial;

        public WeaponData CurrentWeapon => weapons.Count > 0 ? weapons[_currentIndex] : null;
        public int CurrentMagAmmo => _magAmmo != null && _magAmmo.Length > _currentIndex ? _magAmmo[_currentIndex] : 0;
        public int CurrentReserveAmmo => _reserveAmmo != null && _reserveAmmo.Length > _currentIndex ? _reserveAmmo[_currentIndex] : 0;
        public bool IsReloading => _reloadTimer > 0f;

        private int _currentIndex;
        private int[] _magAmmo;
        private int[] _reserveAmmo;
        private float _nextFireTime;
        private float _reloadTimer;
        private float _recoilOffset;
        private float _recoilAngle;
        private float _muzzleFlashTimer;
        private float _bobTimer;
        private Vector2 _baseHudPos;
        private Vector3 _baseViewmodelPos;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerController == null) playerController = GetComponentInParent<PlayerController>();
            if (hudWeaponRect != null) _baseHudPos = hudWeaponRect.anchoredPosition;
            if (viewmodelRoot != null) _baseViewmodelPos = viewmodelRoot.localPosition;
            if (muzzleLight != null) muzzleLight.enabled = false;

            InitAmmo();
            ApplyWeaponVisual();
        }

        private void InitAmmo()
        {
            _magAmmo = new int[weapons.Count];
            _reserveAmmo = new int[weapons.Count];
            for (int i = 0; i < weapons.Count; i++)
            {
                _magAmmo[i] = weapons[i].magazineSize;
                _reserveAmmo[i] = weapons[i].maxTotalAmmo;
            }
        }

        private void Update()
        {
            if (weapons.Count == 0 || Keyboard.current == null || Mouse.current == null) return;

            HandleWeaponSwitch();
            HandleReload();
            HandleFire();
            UpdateHudWeaponAnimation();
        }

        private void HandleWeaponSwitch()
        {
            int newIndex = _currentIndex;
            if (Keyboard.current.digit1Key.wasPressedThisFrame && weapons.Count >= 1) newIndex = 0;
            if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Count >= 2) newIndex = 1;
            if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Count >= 3) newIndex = 2;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.1f && weapons.Count > 1)
            {
                newIndex = (_currentIndex + (scroll > 0 ? -1 : 1) + weapons.Count) % weapons.Count;
            }

            if (newIndex != _currentIndex)
            {
                _currentIndex = newIndex;
                _reloadTimer = 0f;
                ApplyWeaponVisual();
            }
        }

        private void HandleReload()
        {
            WeaponData weapon = CurrentWeapon;
            if (weapon == null) return;

            if (_reloadTimer > 0f)
            {
                _reloadTimer -= Time.deltaTime;
                if (_reloadTimer <= 0f)
                    CompleteReload();
                return;
            }

            bool wantReload = Keyboard.current.rKey.wasPressedThisFrame || _magAmmo[_currentIndex] <= 0;
            if (wantReload && _magAmmo[_currentIndex] < weapon.magazineSize)
            {
                if (weapon.maxTotalAmmo < 0 || _reserveAmmo[_currentIndex] > 0)
                {
                    _reloadTimer = weapon.reloadTime;
                }
            }
        }

        private void CompleteReload()
        {
            WeaponData weapon = CurrentWeapon;
            if (weapon.maxTotalAmmo < 0)
            {
                _magAmmo[_currentIndex] = weapon.magazineSize;
            }
            else
            {
                int needed = weapon.magazineSize - _magAmmo[_currentIndex];
                int take = Mathf.Min(needed, _reserveAmmo[_currentIndex]);
                _magAmmo[_currentIndex] += take;
                _reserveAmmo[_currentIndex] -= take;
            }
        }

        private void HandleFire()
        {
            if (Cursor.lockState != CursorLockMode.Locked || _reloadTimer > 0f) return;

            WeaponData weapon = CurrentWeapon;
            if (weapon == null) return;

            bool trigger = weapon.automatic
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame;

            if (!trigger || Time.time < _nextFireTime) return;
            if (_magAmmo[_currentIndex] <= 0) return;

            _nextFireTime = Time.time + (1f / Mathf.Max(0.1f, weapon.fireRate));
            _magAmmo[_currentIndex]--;

            Vector3 aimPoint = GetAimPoint();
            Vector3 spawnPos = fireOrigin != null ? fireOrigin.position : playerCamera.transform.position + playerCamera.transform.forward * 0.6f;
            Vector3 baseDir = (aimPoint - spawnPos).normalized;

            for (int i = 0; i < weapon.pelletsPerShot; i++)
            {
                Vector3 spreadDir = ApplySpread(baseDir, weapon.spreadAngle);
                GameObject obj = ProjectilePool.Instance != null
                    ? ProjectilePool.Instance.Spawn(projectilePrefab, spawnPos, Quaternion.LookRotation(spreadDir))
                    : Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(spreadDir));

                if (obj != null && obj.TryGetComponent<PlayerProjectile>(out var proj))
                {
                    proj.Initialize(
                        spreadDir,
                        weapon.projectileSpeed,
                        weapon.damage,
                        weapon.knockbackForce,
                        weapon.maxBounces,
                        weapon.projectileScale,
                        weapon.projectileColor);
                }
            }

            _recoilOffset = weapon.recoilKickPixels;
            _recoilAngle = Random.Range(-7f, -3f);
            _muzzleFlashTimer = 0.055f;
            if (hudMuzzleFlash != null)
            {
                hudMuzzleFlash.enabled = true;
                hudMuzzleFlash.color = weapon.projectileColor;
                hudMuzzleFlash.rectTransform.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
            }
            if (muzzleLight != null)
            {
                muzzleLight.color = weapon.projectileColor;
                muzzleLight.enabled = true;
            }

            ScreenEffects.Instance?.AddShake(weapon.screenShake);

            SfxType sfx = weapon.pelletsPerShot > 1
                ? SfxType.Shotgun
                : (weapon.maxBounces > 0 ? SfxType.Bounce : SfxType.Pistol);
            RetroAudio.Instance?.Play(sfx);
        }

        public void RefillAllAmmo()
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                _magAmmo[i] = weapons[i].magazineSize;
                _reserveAmmo[i] = weapons[i].maxTotalAmmo;
            }
        }

        private Vector3 GetAimPoint()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player"))
                    return hit.point;
            }
            return ray.GetPoint(50f);
        }

        private Vector3 ApplySpread(Vector3 dir, float angleDeg)
        {
            if (angleDeg <= 0.001f) return dir;
            float yaw = Random.Range(-angleDeg, angleDeg);
            float pitch = Random.Range(-angleDeg, angleDeg);
            return Quaternion.Euler(pitch, yaw, 0f) * dir;
        }

        public WeaponData EquipMainWeapon(WeaponData newWeapon)
        {
            if (newWeapon == null) return null;
            WeaponData previous = CurrentWeapon;
            if (weapons.Count == 0)
            {
                weapons.Add(newWeapon);
                _currentIndex = 0;
            }
            else
            {
                weapons[_currentIndex] = newWeapon;
            }

            _reloadTimer = 0f;
            _nextFireTime = 0f;
            InitAmmo();
            ApplyWeaponVisual();
            return previous;
        }

        private void ApplyWeaponVisual()
        {
            if (hudWeaponImage != null && CurrentWeapon != null && CurrentWeapon.hudSprite != null)
            {
                hudWeaponImage.sprite = CurrentWeapon.hudSprite;
                hudWeaponImage.preserveAspect = true;
            }
            if (gunCoreMaterial != null && CurrentWeapon != null)
            {
                gunCoreMaterial.color = CurrentWeapon.projectileColor;
                gunCoreMaterial.SetColor("_EmissionColor", CurrentWeapon.projectileColor * 2.4f);
            }
        }

        private void UpdateHudWeaponAnimation()
        {
            if (_muzzleFlashTimer > 0f)
            {
                _muzzleFlashTimer -= Time.deltaTime;
                if (_muzzleFlashTimer <= 0f)
                {
                    if (hudMuzzleFlash != null) hudMuzzleFlash.enabled = false;
                    if (muzzleLight != null) muzzleLight.enabled = false;
                }
            }

            _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, Time.deltaTime * 16f);
            _recoilAngle = Mathf.Lerp(_recoilAngle, 0f, Time.deltaTime * 16f);

            float speed = playerController != null ? playerController.CurrentHorizontalSpeed : 0f;
            if (speed > 0.5f)
                _bobTimer += Time.deltaTime * 12f;
            else
                _bobTimer = 0f;

            float bobX = Mathf.Cos(_bobTimer * 0.5f) * (speed > 0.5f ? 1f : 0f);
            float bobY = Mathf.Abs(Mathf.Sin(_bobTimer)) * (speed > 0.5f ? 1f : 0f);

            if (viewmodelRoot != null)
            {
                float kickBack = (_recoilOffset / 48f) * 0.14f;
                float reloadDip3D = _reloadTimer > 0f ? -0.18f : 0f;
                float reloadRoll = _reloadTimer > 0f ? 32f : 0f;

                viewmodelRoot.localPosition = _baseViewmodelPos + new Vector3(bobX * 0.014f, -bobY * 0.014f + reloadDip3D, -kickBack);
                viewmodelRoot.localRotation = Quaternion.Euler(_recoilAngle * 2.2f, 0f, reloadRoll);
            }

            if (hudWeaponRect == null) return;

            float reloadDip = _reloadTimer > 0f ? -90f : 0f;
            hudWeaponRect.anchoredPosition = _baseHudPos + new Vector2(bobX * 8f, -bobY * 8f - _recoilOffset + reloadDip);
            hudWeaponRect.localEulerAngles = new Vector3(0f, 0f, _recoilAngle);
        }
    }
}
