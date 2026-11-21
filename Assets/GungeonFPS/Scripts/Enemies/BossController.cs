using System;
using UnityEngine;
using GungeonFPS.Core;

namespace GungeonFPS.Enemies
{
    public class BossController : MonoBehaviour, IDamageable
    {
        public static BossController ActiveBoss { get; private set; }

        [Header("Stats")]
        [SerializeField] private string bossName = "OMEGA CORE MK-I";
        [SerializeField] private float maxHealth = 550f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private GameObject enemyBulletPrefab;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public string BossName => bossName;
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(_hp / maxHealth);
        public bool IsPhaseTwo => _hp <= maxHealth * 0.5f;

        public event Action<BossController> OnDied;

        private float _hp;
        private bool _isDead;
        private int _patternIndex;
        private float _patternTimer;
        private float _attackTimer;
        private float _spiralAngle;
        private float _flashTimer;
        private Color _baseColor = Color.white;
        private Transform _player;
        private Vector3 _arenaCenter;

        private void Awake()
        {
            _hp = maxHealth;
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                _baseColor = spriteRenderer.color;
        }

        private void OnEnable()
        {
            ActiveBoss = this;
            _arenaCenter = transform.position;
            _patternTimer = 3.5f;
            _attackTimer = 0.6f;

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void OnDisable()
        {
            if (ActiveBoss == this)
                ActiveBoss = null;
        }

        private void Update()
        {
            if (_isDead) return;
            float dt = Time.deltaTime;

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f && spriteRenderer != null)
                    spriteRenderer.color = IsPhaseTwo ? new Color(1f, 0.55f, 0.65f) : _baseColor;
            }

            if (_player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
                else return;
            }

            // Orbit around arena center
            float orbitSpeed = IsPhaseTwo ? 1.15f : 0.7f;
            float t = Time.time * orbitSpeed;
            Vector3 targetPos = _arenaCenter + new Vector3(Mathf.Cos(t) * 5.5f, 0f, Mathf.Sin(t) * 5.5f);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * (IsPhaseTwo ? 1.4f : 1f) * dt);

            _patternTimer -= dt;
            if (_patternTimer <= 0f)
            {
                _patternIndex = (_patternIndex + 1) % 3;
                _patternTimer = IsPhaseTwo ? 2.8f : 3.6f;
                _attackTimer = 0.25f;
            }

            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                ExecutePattern();
            }
        }

        private void ExecutePattern()
        {
            if (enemyBulletPrefab == null) return;
            Vector3 spawnPos = transform.position + Vector3.up * 1.3f;
            Vector3 aimDir = (_player.position + Vector3.up * 0.9f - spawnPos).normalized;

            switch (_patternIndex)
            {
                case 0: // Spiral Barrage
                    int arms = IsPhaseTwo ? 6 : 4;
                    float armStep = 360f / arms;
                    for (int i = 0; i < arms; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, _spiralAngle + armStep * i, 0f) * Vector3.forward;
                        SpawnBullet(spawnPos, dir, IsPhaseTwo ? 9.5f : 8f);
                    }
                    _spiralAngle = (_spiralAngle + 17f) % 360f;
                    _attackTimer = IsPhaseTwo ? 0.22f : 0.30f;
                    break;

                case 1: // Aimed Spread Fan
                    int fanCount = IsPhaseTwo ? 9 : 7;
                    float arc = 75f;
                    float start = -arc * 0.5f;
                    float step = arc / (fanCount - 1);
                    for (int i = 0; i < fanCount; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, start + step * i, 0f) * aimDir;
                        SpawnBullet(spawnPos, dir, IsPhaseTwo ? 10.5f : 9f);
                    }
                    _attackTimer = IsPhaseTwo ? 0.75f : 1.0f;
                    break;

                case 2: // 360 Nova Ring
                    int ringCount = IsPhaseTwo ? 18 : 14;
                    float ringStep = 360f / ringCount;
                    for (int i = 0; i < ringCount; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, ringStep * i + _spiralAngle, 0f) * Vector3.forward;
                        SpawnBullet(spawnPos, dir, 8.2f);
                    }
                    _spiralAngle += 12f;
                    _attackTimer = IsPhaseTwo ? 0.85f : 1.15f;
                    break;
            }
        }

        private void SpawnBullet(Vector3 pos, Vector3 dir, float speed)
        {
            GameObject obj = ProjectilePool.Instance != null
                ? ProjectilePool.Instance.Spawn(enemyBulletPrefab, pos, Quaternion.LookRotation(dir))
                : Instantiate(enemyBulletPrefab, pos, Quaternion.LookRotation(dir));

            if (obj != null && obj.TryGetComponent<EnemyBullet>(out var bullet))
                bullet.Initialize(dir, speed, 1f);
        }

        public void TakeDamage(float amount, Vector3 hitDirection, float knockbackForce = 0f)
        {
            if (_isDead) return;
            _hp -= amount;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                _flashTimer = 0.06f;
            }

            if (_hp <= 0f)
            {
                _isDead = true;
                if (ActiveBoss == this) ActiveBoss = null;
                EnemyBullet.ClearAllBullets();
                RetroAudio.Instance?.Play(SfxType.Blank, 1.2f);
                VfxSpawner.Instance?.SpawnBurst(transform.position + Vector3.up * 1.2f, new Color(1f, 0.2f, 0.65f), 32, 12f, 0.28f);
                ScreenEffects.Instance?.AddShake(0.35f);
                OnDied?.Invoke(this);
                Destroy(gameObject);
            }
            else if (amount > 0f)
            {
                RetroAudio.Instance?.Play(SfxType.Hit, 0.6f);
            }
        }
    }
}
