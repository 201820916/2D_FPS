using System.Collections.Generic;
using UnityEngine;
using GungeonFPS.Core;

namespace GungeonFPS.Enemies
{
    public class EnemyBullet : MonoBehaviour
    {
        private static readonly HashSet<EnemyBullet> ActiveBullets = new();

        [SerializeField] private float defaultSpeed = 9f;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float hitRadius = 0.35f;

        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _timer;

        private void OnEnable()
        {
            ActiveBullets.Add(this);
        }

        private void OnDisable()
        {
            ActiveBullets.Remove(this);
        }

        public void Initialize(Vector3 direction, float speed, float damage)
        {
            _direction = direction.normalized;
            _speed = speed > 0f ? speed : defaultSpeed;
            _damage = damage;
            _timer = lifetime;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Despawn();
                return;
            }

            float step = _speed * Time.deltaTime;
            if (Physics.SphereCast(transform.position, hitRadius, _direction, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<GungeonFPS.Weapons.ShieldBlocker>(out var shield) && shield.IsBlocking)
                {
                    VfxSpawner.Instance?.SpawnBurst(hit.point, new Color(0.25f, 0.95f, 1f), 5, 4f, 0.1f);
                    RetroAudio.Instance?.Play(SfxType.Bounce, 0.5f);
                    Despawn();
                    return;
                }

                if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent<IDamageable>(out var playerDmg))
                {
                    playerDmg.TakeDamage(_damage, _direction, 0f);
                    Despawn();
                    return;
                }

                if (!hit.collider.CompareTag("Enemy"))
                {
                    Despawn();
                    return;
                }
            }

            transform.position += _direction * step;
        }

        public static void ClearAllBullets()
        {
            var snapshot = new List<EnemyBullet>(ActiveBullets);
            foreach (var bullet in snapshot)
            {
                if (bullet != null && bullet.gameObject.activeSelf)
                    bullet.Despawn();
            }
        }

        public static int ClearBulletsInSphere(Vector3 center, float radius)
        {
            int cleared = 0;
            float rSq = radius * radius;
            var snapshot = new List<EnemyBullet>(ActiveBullets);
            foreach (var bullet in snapshot)
            {
                if (bullet == null || !bullet.gameObject.activeSelf) continue;
                if ((bullet.transform.position - center).sqrMagnitude <= rSq)
                {
                    VfxSpawner.Instance?.SpawnBurst(bullet.transform.position, new Color(0.25f, 0.95f, 1f), 4, 4f, 0.1f);
                    bullet.Despawn();
                    cleared++;
                }
            }
            return cleared;
        }

        private void Despawn()
        {
            if (ProjectilePool.Instance != null)
                ProjectilePool.Instance.Despawn(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
