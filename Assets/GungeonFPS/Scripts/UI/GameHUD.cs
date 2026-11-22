using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GungeonFPS.Core;
using GungeonFPS.Dungeon;
using GungeonFPS.Enemies;
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
        [SerializeField] private InventorySystem inventorySystem;

        [SerializeField] private Text statusText;
        [SerializeField] private Text interactPromptText;
        [SerializeField] private Image dashFillBar;
        [SerializeField] private Image hitmarkerImage;

        [Header("Boss & Floor Clear UI")]
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image bossBarFill;
        [SerializeField] private Text bossNameText;
        [SerializeField] private Text victoryBannerText;

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
            if (bossBarRoot != null)
                bossBarRoot.SetActive(false);
            if (victoryBannerText != null)
                victoryBannerText.text = "";

            EnsureFilledSprite(bossBarFill);
            EnsureFilledSprite(dashFillBar);
        }

        private static void EnsureFilledSprite(Image img)
        {
            if (img == null || img.sprite != null) return;
            var tex = Texture2D.whiteTexture;
            img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        public void ShowFloorCleared()
        {
            if (victoryBannerText != null)
                victoryBannerText.text = $"FLOOR {RunManager.CurrentFloor} CLEARED!\nENTER THE PORTAL FOR FLOOR {RunManager.CurrentFloor + 1}";
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
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                RunManager.ResetRun();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (bossBarRoot != null)
            {
                var boss = BossController.ActiveBoss;
                bool showBoss = boss != null;
                if (bossBarRoot.activeSelf != showBoss)
                    bossBarRoot.SetActive(showBoss);

                if (showBoss)
                {
                    if (bossBarFill != null) bossBarFill.fillAmount = boss.HealthNormalized;
                    if (bossNameText != null) bossNameText.text = boss.IsPhaseTwo ? $"{boss.BossName} [OVERDRIVE]" : boss.BossName;
                }
            }

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
                string floorStr = $"FLOOR {RunManager.CurrentFloor}  |  ";
                string hpStr = playerHealth != null ? $"HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}" : "";
                string blankStr = playerController != null ? $"  |  BLANKS [Q]: {playerController.BlanksCount}" : "";
                string coreStr = inventorySystem != null ? $"  |  CORE: {inventorySystem.CoreCurrency}" : "";

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
                    string state = secondaryController.IsShieldDepleted ? " [RECHARGING]" : "";
                    secStr = $"   |   [SEC / RMB] {s.weaponName} ({pct}%){state}   |   [SHIFT] DASH   |   [TAB] BAG";
                }

                statusText.text = floorStr + hpStr + blankStr + coreStr + roomStr + mainStr + secStr;
            }
        }
    }
}
