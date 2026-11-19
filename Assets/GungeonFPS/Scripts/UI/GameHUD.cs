using UnityEngine;
using UnityEngine.UI;
using GungeonFPS.Dungeon;
using GungeonFPS.Player;
using GungeonFPS.Weapons;

namespace GungeonFPS.UI
{
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private SecondaryWeaponController secondaryController;

        [SerializeField] private Text statusText;
        [SerializeField] private Text interactPromptText;
        [SerializeField] private Image dashFillBar;
        [SerializeField] private Image hitmarkerImage;

        private float _hitmarkerTimer;
        private string _pendingPrompt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (hitmarkerImage != null)
                hitmarkerImage.enabled = false;
            if (interactPromptText != null)
                interactPromptText.text = "";
        }

        public void TriggerHitmarker()
        {
            if (hitmarkerImage == null) return;
            hitmarkerImage.enabled = true;
            _hitmarkerTimer = 0.1f;
        }

        public void RequestInteractPrompt(string prompt)
        {
            _pendingPrompt = prompt;
        }

        private void LateUpdate()
        {
            if (interactPromptText != null)
            {
                interactPromptText.text = _pendingPrompt ?? "";
            }
            _pendingPrompt = null;
        }

        private void Update()
        {
            if (_hitmarkerTimer > 0f)
            {
                _hitmarkerTimer -= Time.deltaTime;
                if (_hitmarkerTimer <= 0f && hitmarkerImage != null)
                    hitmarkerImage.enabled = false;
            }

            if (dashFillBar != null && playerController != null)
                dashFillBar.fillAmount = playerController.DashCooldownProgress;

            if (statusText != null)
            {
                string hpStr = playerHealth != null ? $"HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}" : "";
                string blankStr = playerController != null ? $"  |  BLANKS [Q]: {playerController.BlanksCount}" : "";

                string roomStr = "";
                if (RoomController.ActiveCombatRoom != null)
                {
                    var r = RoomController.ActiveCombatRoom;
                    roomStr = $"  |  [{r.RoomName}] ENEMIES: {r.RemainingEnemies}";
                }

                string mainStr = "";
                if (weaponController != null && weaponController.CurrentWeapon != null)
                {
                    var w = weaponController.CurrentWeapon;
                    string ammo = w.maxTotalAmmo < 0
                        ? $"{weaponController.CurrentMagAmmo}/{w.magazineSize} (INF)"
                        : $"{weaponController.CurrentMagAmmo}/{weaponController.CurrentReserveAmmo}";
                    string reload = weaponController.IsReloading ? " [RELOADING]" : "";
                    mainStr = $"\n[MAIN / LMB] {w.weaponName}  {ammo}{reload}";
                }

                string secStr = "";
                if (secondaryController != null && secondaryController.CurrentSecondary != null)
                {
                    var s = secondaryController.CurrentSecondary;
                    int pct = Mathf.RoundToInt(secondaryController.CooldownOrEnergyNormalized * 100f);
                    secStr = $"   |   [SEC / RMB] {s.weaponName} ({pct}%)   |   [SHIFT] DASH   |   [TAB] BAG";
                }

                statusText.text = hpStr + blankStr + roomStr + mainStr + secStr;
            }
        }
    }
}
