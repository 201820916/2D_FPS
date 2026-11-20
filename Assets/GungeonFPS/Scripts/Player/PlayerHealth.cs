using UnityEngine;
using UnityEngine.SceneManagement;
using GungeonFPS.Core;
using GungeonFPS.UI;

namespace GungeonFPS.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 6; // 6 half-hearts (3 full hearts)
        [SerializeField] private float hitInvulnDuration = 0.75f;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsInvulnerable => _hitInvulnTimer > 0f || (_playerController != null && _playerController.IsDashInvincible);

        private float _hitInvulnTimer;
        private PlayerController _playerController;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            _playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_hitInvulnTimer > 0f)
                _hitInvulnTimer -= Time.deltaTime;
        }

        public void TakeDamage(float amount, Vector3 hitDirection, float knockbackForce = 0f)
        {
            if (IsInvulnerable) return;

            int dmg = Mathf.Max(1, Mathf.RoundToInt(amount));
            CurrentHealth = Mathf.Max(0, CurrentHealth - dmg);
            _hitInvulnTimer = hitInvulnDuration;

            ScreenEffects.Instance?.AddShake(0.22f);
            ScreenEffects.Instance?.TriggerFlash(new Color(1f, 0.15f, 0.15f), 0.5f);
            DamageIndicatorUI.Instance?.ShowDamageFrom(hitDirection);
            RetroAudio.Instance?.Play(SfxType.Hurt);

            if (CurrentHealth <= 0)
            {
                // Quick prototype reset: reload current scene
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }
    }
}
