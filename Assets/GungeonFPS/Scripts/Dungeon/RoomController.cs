using System.Collections.Generic;
using UnityEngine;
using GungeonFPS.Core;
using GungeonFPS.Enemies;

namespace GungeonFPS.Dungeon
{
    public class RoomController : MonoBehaviour
    {
        public static RoomController ActiveCombatRoom { get; private set; }

        [Header("Room Setup")]
        [SerializeField] private string roomName = "Combat Room";
        [SerializeField] private List<DoorController> doors = new();
        [SerializeField] private List<EnemyBase> enemies = new();
        [SerializeField] private BossController boss;
        [SerializeField] private GameObject rewardPickupPrefab;
        [SerializeField] private Transform rewardSpawnPoint;

        public string RoomName => roomName;
        public bool IsCleared => _isCleared;
        public bool IsActive => _isActive;
        public bool IsBossRoom => boss != null || _wasBossRoom;
        public int RemainingEnemies { get; private set; }

        public event System.Action<RoomController> OnRoomCleared;

        private bool _isActive;
        private bool _isCleared;
        private bool _wasBossRoom;
        private BoxCollider _triggerBox;
        private Transform _player;

        public void InitializeRuntime(
            string rName,
            List<DoorController> roomDoors,
            List<EnemyBase> roomEnemies,
            BossController roomBoss,
            GameObject rewardPrefab)
        {
            roomName = rName;
            doors = roomDoors ?? new List<DoorController>();
            enemies = roomEnemies ?? new List<EnemyBase>();
            boss = roomBoss;
            _wasBossRoom = roomBoss != null;
            rewardPickupPrefab = rewardPrefab;

            foreach (var e in enemies)
                if (e != null) e.gameObject.SetActive(false);
            if (boss != null)
                boss.gameObject.SetActive(false);
        }

        private void Awake()
        {
            _triggerBox = GetComponent<BoxCollider>();
            _wasBossRoom = boss != null;
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                    enemy.gameObject.SetActive(false);
            }
            if (boss != null)
                boss.gameObject.SetActive(false);
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        private void Update()
        {
            if (_isCleared || _isActive || _triggerBox == null) return;
            if (_player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
                else return;
            }

            if (_triggerBox.bounds.Contains(_player.position))
            {
                StartCombat();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCleared || _isActive) return;
            if (!other.CompareTag("Player")) return;

            StartCombat();
        }

        private void StartCombat()
        {
            _isActive = true;
            ActiveCombatRoom = this;

            foreach (var door in doors)
            {
                if (door != null)
                    door.SetOpen(false);
            }

            RemainingEnemies = 0;
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                enemy.gameObject.SetActive(true);
                enemy.OnDied += HandleEnemyDied;
                RemainingEnemies++;
            }

            if (boss != null)
            {
                boss.gameObject.SetActive(true);
                boss.OnDied += HandleBossDied;
                RemainingEnemies++;
            }

            if (RemainingEnemies == 0)
                ClearRoom();
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            enemy.OnDied -= HandleEnemyDied;
            RemainingEnemies = Mathf.Max(0, RemainingEnemies - 1);

            if (RemainingEnemies == 0)
                ClearRoom();
        }

        private void HandleBossDied(BossController b)
        {
            b.OnDied -= HandleBossDied;
            RemainingEnemies = Mathf.Max(0, RemainingEnemies - 1);

            if (RemainingEnemies == 0)
                ClearRoom();
        }

        private void ClearRoom()
        {
            _isActive = false;
            _isCleared = true;
            if (ActiveCombatRoom == this) ActiveCombatRoom = null;

            foreach (var door in doors)
            {
                if (door != null)
                    door.SetOpen(true);
            }

            RetroAudio.Instance?.Play(SfxType.DoorOpen);
            EnemyBullet.ClearAllBullets();

            if (rewardPickupPrefab != null)
            {
                bool isChest = rewardPickupPrefab.GetComponent<ChestController>() != null;
                Vector3 pos = rewardSpawnPoint != null
                    ? rewardSpawnPoint.position
                    : transform.position + (isChest ? Vector3.zero : Vector3.up * 0.9f);
                Instantiate(rewardPickupPrefab, pos, Quaternion.identity);
            }

            OnRoomCleared?.Invoke(this);
        }
    }
}
