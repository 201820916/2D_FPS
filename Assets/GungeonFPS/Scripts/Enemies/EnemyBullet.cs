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

        private void Despawn()
        {
            if (ProjectilePool.Instance != null)
                ProjectilePool.Instance.Despawn(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
