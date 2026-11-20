using UnityEngine;
using GungeonFPS.Core;

namespace GungeonFPS.Enemies
{
    public enum EnemyPattern
    {
        SingleChaser,
        SpreadTurret,
        SuicideBomber,
        SpiralCore
    }

    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private EnemyPattern pattern = EnemyPattern.SingleChaser;
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private float moveSpeed = 4.2f;
        [SerializeField] private float preferredRange = 6.5f;

        [Header("Attack")]
        [SerializeField] private GameObject enemyBulletPrefab;
        [SerializeField] private float fireInterval = 1.4f;
        [SerializeField] private float bulletSpeed = 8.5f;
        [SerializeField] private float bulletDamage = 1f;
        [SerializeField] private int spreadCount = 5;
        [SerializeField] private float spreadArcAngle = 50f;
        [SerializeField] private float explodeRadius = 2.6f;
        [SerializeField] private int explodeBulletCount = 8;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float hitFlashDuration = 0.08f;

        public event System.Action<EnemyBase> OnDied;

        private float _hp;
        private float _fireTimer;
        private float _flashTimer;
        private float _spiralAngle;
        private bool _isDead;
        private Color _baseColor = Color.white;
        private Vector3 _knockbackVel;
        private Transform _player;

        private void Awake()
        {
            _hp = maxHealth;
            _fireTimer = Random.Range(0.4f, fireInterval);
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                _baseColor = spriteRenderer.color;
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f && spriteRenderer != null)
                    spriteRenderer.color = _baseColor;
            }

            if (_knockbackVel.sqrMagnitude > 0.01f)
            {
                transform.position += _knockbackVel * dt;
                _knockbackVel = Vector3.Lerp(_knockbackVel, Vector3.zero, dt * 10f);
            }

            if (_player == null) return;

            Vector3 toPlayer = _player.position - transform.position;
            Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float dist = flatToPlayer.magnitude;

            if (pattern == EnemyPattern.SingleChaser && dist > preferredRange)
            {
                Vector3 moveDir = flatToPlayer.normalized;
                transform.position += moveDir * (moveSpeed * dt);
            }
            else if (pattern == EnemyPattern.SuicideBomber)
            {
                if (dist > 0.01f)
                    transform.position += flatToPlayer.normalized * (moveSpeed * 1.55f * dt);

                if (dist <= explodeRadius)
                {
                    Detonate(true);
                    return;
                }
                return;
            }

            _fireTimer -= dt;
            if (_fireTimer <= 0f && dist < 32f)
            {
                _fireTimer = fireInterval;
                FireAtPlayer(toPlayer);
            }
        }

        private void FireAtPlayer(Vector3 toPlayer)
        {
            if (enemyBulletPrefab == null) return;

            Vector3 spawnPos = transform.position + Vector3.up * 0.9f;
            Vector3 aimDir = (_player.position + Vector3.up * 0.9f - spawnPos).normalized;

            if (pattern == EnemyPattern.SingleChaser)
            {
                SpawnBullet(spawnPos, aimDir);
            }
            else if (pattern == EnemyPattern.SpiralCore)
            {
                int arms = Mathf.Max(2, spreadCount);
                float armStep = 360f / arms;
                for (int i = 0; i < arms; i++)
                {
                    float angle = _spiralAngle + armStep * i;
                    Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    SpawnBullet(spawnPos, dir);
                }
                _spiralAngle = (_spiralAngle + 18f) % 360f;
            }
            else
            {
                int count = Mathf.Max(1, spreadCount);
                float startAngle = -spreadArcAngle * 0.5f;
                float step = count > 1 ? spreadArcAngle / (count - 1) : 0f;

                for (int i = 0; i < count; i++)
                {
                    float angle = startAngle + step * i;
                    Vector3 dir = Quaternion.Euler(0f, angle, 0f) * aimDir;
                    SpawnBullet(spawnPos, dir);
                }
            }
        }

        private void Detonate(bool hitPlayerIfClose)
        {
            if (_isDead) return;
            _isDead = true;

            Vector3 center = transform.position + Vector3.up * 0.9f;
            if (hitPlayerIfClose && _player != null && _player.TryGetComponent<IDamageable>(out var playerDmg))
            {
                Vector3 dir = (_player.position - transform.position).normalized;
                playerDmg.TakeDamage(bulletDamage, dir, 0f);
            }

            if (enemyBulletPrefab != null && explodeBulletCount > 0)
            {
                float step = 360f / explodeBulletCount;
                for (int i = 0; i < explodeBulletCount; i++)
                {
                    Vector3 dir = Quaternion.Euler(0f, step * i, 0f) * Vector3.forward;
                    SpawnBullet(center, dir);
                }
            }

            RetroAudio.Instance?.Play(SfxType.Blank, 0.85f);
            VfxSpawner.Instance?.SpawnBurst(center, new Color(1f, 0.65f, 0.1f), 20, 9f, 0.22f);
            OnDied?.Invoke(this);
            Destroy(gameObject);
        }

        private void SpawnBullet(Vector3 pos, Vector3 dir)
        {
            GameObject obj = ProjectilePool.Instance != null
                ? ProjectilePool.Instance.Spawn(enemyBulletPrefab, pos, Quaternion.LookRotation(dir))
                : Instantiate(enemyBulletPrefab, pos, Quaternion.LookRotation(dir));

            if (obj != null && obj.TryGetComponent<EnemyBullet>(out var bullet))
                bullet.Initialize(dir, bulletSpeed, bulletDamage);
        }

        public void TakeDamage(float amount, Vector3 hitDirection, float knockbackForce = 0f)
        {
            if (_isDead) return;
            _hp -= amount;

            if (knockbackForce > 0f)
            {
                Vector3 flatDir = new Vector3(hitDirection.x, 0f, hitDirection.z).normalized;
                _knockbackVel += flatDir * knockbackForce;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
                _flashTimer = hitFlashDuration;
            }

            if (_hp <= 0f)
            {
                if (pattern == EnemyPattern.SuicideBomber)
                {
                    Detonate(false);
                    return;
                }

                _isDead = true;
                RetroAudio.Instance?.Play(SfxType.EnemyDie);
                VfxSpawner.Instance?.SpawnBurst(transform.position + Vector3.up * 0.9f, new Color(1f, 0.45f, 0.15f), 14, 7.5f, 0.18f);
                OnDied?.Invoke(this);
                Destroy(gameObject);
            }
            else if (amount > 0f)
            {
                RetroAudio.Instance?.Play(SfxType.Hit, 0.7f);
            }
        }
    }
}
