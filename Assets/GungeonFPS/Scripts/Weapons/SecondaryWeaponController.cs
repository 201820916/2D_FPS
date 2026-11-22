using UnityEngine;
using UnityEngine.InputSystem;
using GungeonFPS.Core;
using GungeonFPS.Enemies;
using GungeonFPS.UI;

namespace GungeonFPS.Weapons
{
    public class SecondaryWeaponController : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private SecondaryWeaponData equippedSecondary;
        [SerializeField] private Camera playerCamera;

        [Header("3D Viewmodels")]
        [SerializeField] private Transform bladeViewmodel;
        [SerializeField] private Transform shieldViewmodel;
        [SerializeField] private ShieldBlocker shieldBlocker;

        public SecondaryWeaponData CurrentSecondary => equippedSecondary;
        public float CooldownOrEnergyNormalized
        {
            get
            {
                if (equippedSecondary == null) return 1f;
                if (equippedSecondary.secondaryType == SecondaryWeaponType.EnergyBlade)
                    return equippedSecondary.cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(_cooldownTimer / equippedSecondary.cooldown);
                return equippedSecondary.maxEnergy <= 0f ? 1f : Mathf.Clamp01(_shieldEnergy / equippedSecondary.maxEnergy);
            }
        }
        public bool IsShieldActive => equippedSecondary != null &&
                                      equippedSecondary.secondaryType == SecondaryWeaponType.AegisShield &&
                                      shieldBlocker != null && shieldBlocker.IsBlocking;
        public bool IsShieldDepleted => _shieldDepleted;

        private float _cooldownTimer;
        private float _swingTimer;
        private float _shieldEnergy;
        private bool _shieldDepleted;
        private Vector3 _bladeRestPos;
        private Quaternion _bladeRestRot;
        private Vector3 _shieldRestPos;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (bladeViewmodel != null)
            {
                _bladeRestPos = bladeViewmodel.localPosition;
                _bladeRestRot = bladeViewmodel.localRotation;
            }
            if (shieldViewmodel != null)
                _shieldRestPos = shieldViewmodel.localPosition;

            if (equippedSecondary != null)
                _shieldEnergy = equippedSecondary.maxEnergy;

            RefreshViewmodels();
        }

        public SecondaryWeaponData EquipSecondary(SecondaryWeaponData newSecondary)
        {
            SecondaryWeaponData previous = equippedSecondary;
            equippedSecondary = newSecondary;
            _cooldownTimer = 0f;
            _swingTimer = 0f;
            _shieldDepleted = false;
            if (equippedSecondary != null)
                _shieldEnergy = equippedSecondary.maxEnergy;
            RefreshViewmodels();
            return previous;
        }

        private void RefreshViewmodels()
        {
            // Hide secondary viewmodels at idle; only show during active swing or shield hold
            if (bladeViewmodel != null) bladeViewmodel.gameObject.SetActive(false);
            if (shieldViewmodel != null) shieldViewmodel.gameObject.SetActive(false);
            if (shieldBlocker != null) shieldBlocker.IsBlocking = false;
        }

        private void Update()
        {
            if (InventoryUI.IsOpen || GameOverUI.IsOpen) return;
            if (equippedSecondary == null || Mouse.current == null) return;

            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;

            if (equippedSecondary.secondaryType == SecondaryWeaponType.EnergyBlade)
                UpdateBlade();
            else
                UpdateShield();
        }

        private void UpdateBlade()
        {
            bool rmb = Cursor.lockState == CursorLockMode.Locked && Mouse.current.rightButton.wasPressedThisFrame;
            if (rmb && _cooldownTimer <= 0f)
            {
                TriggerBladeSlash();
            }

            if (bladeViewmodel != null)
            {
                if (_swingTimer > 0f)
                {
                    if (!bladeViewmodel.gameObject.activeSelf)
                        bladeViewmodel.gameObject.SetActive(true);

                    _swingTimer -= Time.deltaTime;
                    float t = 1f - Mathf.Clamp01(_swingTimer / 0.18f);
                    bladeViewmodel.localPosition = Vector3.Lerp(
                        _bladeRestPos + new Vector3(0.35f, 0.1f, 0.25f),
                        _bladeRestPos + new Vector3(-0.45f, -0.15f, 0.25f),
                        t);
                    bladeViewmodel.localRotation = Quaternion.Euler(25f, Mathf.Lerp(-45f, 75f, t), Mathf.Lerp(20f, -65f, t));
                }
                else if (bladeViewmodel.gameObject.activeSelf)
                {
                    bladeViewmodel.gameObject.SetActive(false);
                }
            }
        }

        private void TriggerBladeSlash()
        {
            _cooldownTimer = equippedSecondary.cooldown;
            _swingTimer = 0.18f;

            RetroAudio.Instance?.Play(SfxType.Dash, 1.1f);
            ScreenEffects.Instance?.AddShake(0.08f);

            Transform cam = playerCamera != null ? playerCamera.transform : transform;
            Vector3 strikeCenter = cam.position + cam.forward * (equippedSecondary.range * 0.65f);

            int cleared = EnemyBullet.ClearBulletsInSphere(strikeCenter, equippedSecondary.bulletClearRadius);
            if (cleared > 0)
                RetroAudio.Instance?.Play(SfxType.Bounce, 0.8f);

            var hits = Physics.OverlapSphere(strikeCenter, equippedSecondary.range * 0.6f, ~0, QueryTriggerInteraction.Ignore);
            bool hitEnemy = false;
            foreach (var col in hits)
            {
                if (col.CompareTag("Player")) continue;
                if (col.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(equippedSecondary.damage, cam.forward, equippedSecondary.knockback);
                    VfxSpawner.Instance?.SpawnBurst(col.ClosestPoint(strikeCenter), equippedSecondary.accentColor, 8, 6f, 0.14f);
                    hitEnemy = true;
                }
            }

            if (hitEnemy)
                GameHUD.Instance?.TriggerHitmarker();
        }

        private void UpdateShield()
        {
            bool wantsBlock = Cursor.lockState == CursorLockMode.Locked &&
                              Mouse.current.rightButton.isPressed;

            bool holding = wantsBlock && !_shieldDepleted && _shieldEnergy > 0f;

            if (holding)
            {
                _shieldEnergy -= equippedSecondary.drainPerSecond * Time.deltaTime;
                if (_shieldEnergy <= 0f)
                {
                    _shieldEnergy = 0f;
                    _shieldDepleted = true;
                    holding = false;
                }
            }
            else
            {
                _shieldEnergy = Mathf.Min(equippedSecondary.maxEnergy, _shieldEnergy + equippedSecondary.regenPerSecond * Time.deltaTime);
                if (_shieldDepleted && _shieldEnergy >= equippedSecondary.maxEnergy)
                {
                    _shieldDepleted = false;
                }
            }

            if (shieldBlocker != null)
                shieldBlocker.IsBlocking = holding;

            if (shieldViewmodel != null)
            {
                if (shieldViewmodel.gameObject.activeSelf != holding)
                    shieldViewmodel.gameObject.SetActive(holding);

                if (holding)
                {
                    Vector3 targetPos = new Vector3(0f, -0.05f, 0.58f);
                    shieldViewmodel.localPosition = Vector3.Lerp(shieldViewmodel.localPosition, targetPos, Time.deltaTime * 16f);
                }
            }
        }
    }
}
