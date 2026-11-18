using UnityEngine;
using GungeonFPS.Core;
using GungeonFPS.UI;

namespace GungeonFPS.Weapons
{
    public class PlayerProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 3.5f;
        [SerializeField] private float hitRadius = 0.2f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _knockback;
        private int _bouncesRemaining;
        private float _timer;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(Vector3 direction, float speed, float damage, float knockback, int bounces, float scale, Color color)
        {
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _knockback = knockback;
            _bouncesRemaining = bounces;
            _timer = lifetime;

            transform.localScale = Vector3.one * scale;
            if (spriteRenderer != null)
                spriteRenderer.color = color;
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
                if (hit.collider.CompareTag("Player") || hit.collider.GetComponent<ShieldBlocker>() != null)
                {
                    transform.position += _direction * step;
                    return;
                }

                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(_damage, _direction, _knockback);
                    VfxSpawner.Instance?.SpawnBurst(hit.point, new Color(1f, 0.9f, 0.3f), 6, 5f, 0.12f);
                    GameHUD.Instance?.TriggerHitmarker();
                    Despawn();
                    return;
                }

                if (_bouncesRemaining > 0)
                {
                    _bouncesRemaining--;
                    _direction = Vector3.Reflect(_direction, hit.normal).normalized;
                    transform.position = hit.point + hit.normal * (hitRadius + 0.05f);
                    VfxSpawner.Instance?.SpawnBurst(hit.point, new Color(0.2f, 0.95f, 1f), 5, 4f, 0.1f);
                    RetroAudio.Instance?.Play(SfxType.Bounce, 0.5f);
                    return;
                }

                VfxSpawner.Instance?.SpawnBurst(hit.point, Color.white, 4, 3.5f, 0.09f);
                Despawn();
                return;
            }

            transform.position += _direction * step;
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
