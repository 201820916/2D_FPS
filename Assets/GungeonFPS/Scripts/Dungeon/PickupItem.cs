using UnityEngine;
using GungeonFPS.Core;
using GungeonFPS.Player;
using GungeonFPS.Weapons;

namespace GungeonFPS.Dungeon
{
    public enum PickupType
    {
        Health,
        Blank,
        Ammo
    }

    public class PickupItem : MonoBehaviour
    {
        [SerializeField] private PickupType pickupType = PickupType.Health;
        [SerializeField] private int amount = 2;
        [SerializeField] private float pickupRadius = 1.4f;

        private Vector3 _startPos;
        private float _bobTimer;
        private Transform _player;

        private void Start()
        {
            _startPos = transform.position;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void Update()
        {
            _bobTimer += Time.deltaTime * 3.5f;
            transform.position = _startPos + Vector3.up * (Mathf.Sin(_bobTimer) * 0.16f);

            if (_player != null)
            {
                Vector3 diff = _player.position - transform.position;
                diff.y = 0f;
                if (diff.sqrMagnitude <= pickupRadius * pickupRadius)
                    TryConsume(_player.gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            TryConsume(other.gameObject);
        }

        private void TryConsume(GameObject playerObj)
        {
            bool consumed = false;
            switch (pickupType)
            {
                case PickupType.Health:
                    if (playerObj.TryGetComponent<PlayerHealth>(out var hp) && hp.CurrentHealth < hp.MaxHealth)
                    {
                        hp.Heal(amount);
                        consumed = true;
                    }
                    break;
                case PickupType.Blank:
                    if (playerObj.TryGetComponent<PlayerController>(out var pc))
                    {
                        pc.AddBlank(amount);
                        consumed = true;
                    }
                    break;
                case PickupType.Ammo:
                    if (playerObj.TryGetComponent<WeaponController>(out var wc))
                    {
                        wc.RefillAllAmmo();
                        consumed = true;
                    }
                    break;
            }

            if (consumed)
            {
                RetroAudio.Instance?.Play(SfxType.Pickup);
                VfxSpawner.Instance?.SpawnBurst(transform.position, Color.yellow, 10, 5f, 0.14f);
                Destroy(gameObject);
            }
        }
    }
}
