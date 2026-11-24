using System;
using System.Collections.Generic;
using UnityEngine;
using GungeonFPS.Core;
using GungeonFPS.Enemies;
using GungeonFPS.UI;

namespace GungeonFPS.Dungeon
{
    public enum DungeonRoomType
    {
        Start,
        Combat,
        Treasure,
        Boss
    }

    public class GeneratedRoomInfo
    {
        public Vector2Int GridPos;
        public DungeonRoomType RoomType;
        public RoomController Controller;
        public bool Visited;
    }

    public class DungeonGenerator : MonoBehaviour
    {
        public static DungeonGenerator Instance { get; private set; }

        [Header("Layout")]
        [SerializeField] private int totalRooms = 6;
        [SerializeField] private float roomSize = 24f;
        [SerializeField] private float cellSpacing = 32f;

        [Header("Materials")]
        [SerializeField] private Material floorMat;
        [SerializeField] private Material wallMat;
        [SerializeField] private Material doorMat;

        [Header("Prefabs")]
        [SerializeField] private List<GameObject> enemyPrefabs = new();
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private GameObject chestPrefab;
        [SerializeField] private GameObject portalPrefab;

        public IReadOnlyDictionary<Vector2Int, GeneratedRoomInfo> Rooms => _rooms;
        public event Action OnDungeonGenerated;

        private readonly Dictionary<Vector2Int, GeneratedRoomInfo> _rooms = new();
        private static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                RunManager.RestoreCarriedState(p);

            GenerateDungeon();
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int gx = Mathf.RoundToInt(worldPos.x / cellSpacing);
            int gy = Mathf.RoundToInt(worldPos.z / cellSpacing);
            return new Vector2Int(gx, gy);
        }

        public void GenerateDungeon()
        {
            _rooms.Clear();
            int targetCount = Mathf.Max(4, totalRooms + RunManager.ExtraRooms);
            List<Vector2Int> coords = BuildRandomGraph(targetCount);
            Dictionary<Vector2Int, int> bfsDist = ComputeBfsDistances(coords);

            // Pick Boss room: farthest BFS distance from Start (never adjacent to 0,0), preferring dead-ends
            Vector2Int bossPos = coords[coords.Count - 1];
            int bestScore = int.MinValue;
            foreach (var c in coords)
            {
                if (c == Vector2Int.zero) continue;
                int dist = bfsDist.TryGetValue(c, out int d) ? d : 0;
                if (dist < 2) continue; // never adjacent to Start room

                int neighbors = CountNeighbors(c, bfsDist);
                int score = dist * 10 - (neighbors == 1 ? 0 : 3);
                if (score > bestScore)
                {
                    bestScore = score;
                    bossPos = c;
                }
            }

            // Pick 1 Treasure room (neither Start nor Boss, preferring dead-ends)
            Vector2Int treasurePos = Vector2Int.zero;
            int bestTreasureScore = int.MinValue;
            foreach (var c in coords)
            {
                if (c == Vector2Int.zero || c == bossPos) continue;
                int neighbors = CountNeighbors(c, bfsDist);
                int score = (neighbors == 1 ? 10 : 0) + (bfsDist.TryGetValue(c, out int d) ? d : 0);
                if (score > bestTreasureScore)
                {
                    bestTreasureScore = score;
                    treasurePos = c;
                }
            }

            foreach (var c in coords)
            {
                DungeonRoomType type = DungeonRoomType.Combat;
                if (c == Vector2Int.zero) type = DungeonRoomType.Start;
                else if (c == bossPos) type = DungeonRoomType.Boss;
                else if (c == treasurePos) type = DungeonRoomType.Treasure;

                _rooms[c] = new GeneratedRoomInfo
                {
                    GridPos = c,
                    RoomType = type,
                    Visited = (type == DungeonRoomType.Start)
                };
            }

            int combatIdx = 1;
            foreach (var kv in _rooms)
            {
                BuildRoomGeometry(kv.Value, ref combatIdx);
            }

            // Build corridors between adjacent rooms (only +X and +Y to avoid duplicates)
            foreach (var kv in _rooms)
            {
                Vector2Int c = kv.Key;
                if (_rooms.ContainsKey(c + Vector2Int.up))
                    BuildCorridor(c, Vector2Int.up);
                if (_rooms.ContainsKey(c + Vector2Int.right))
                    BuildCorridor(c, Vector2Int.right);
            }

            OnDungeonGenerated?.Invoke();
        }

        private List<Vector2Int> BuildRandomGraph(int count)
        {
            var list = new List<Vector2Int> { Vector2Int.zero };
            var set = new HashSet<Vector2Int> { Vector2Int.zero };

            // Guarantee a minimum 3-room spine away from (0,0) so Boss room is never adjacent to Start
            Vector2Int cur = Vector2Int.zero;
            Vector2Int lastDir = Vector2Int.zero;
            for (int step = 0; step < 3 && list.Count < count; step++)
            {
                var validDirs = new List<Vector2Int>();
                foreach (var d in Dirs)
                {
                    if (d == -lastDir) continue;
                    Vector2Int next = cur + d;
                    if (!set.Contains(next) && (Mathf.Abs(next.x) + Mathf.Abs(next.y) > step))
                        validDirs.Add(d);
                }

                if (validDirs.Count == 0) break;
                lastDir = validDirs[UnityEngine.Random.Range(0, validDirs.Count)];
                cur += lastDir;
                list.Add(cur);
                set.Add(cur);
            }

            int safety = 0;
            while (list.Count < count && safety < 200)
            {
                safety++;
                Vector2Int baseCell = list[UnityEngine.Random.Range(0, list.Count)];
                Vector2Int dir = Dirs[UnityEngine.Random.Range(0, Dirs.Length)];
                Vector2Int next = baseCell + dir;

                if (set.Contains(next)) continue;

                // Keep layout clean by avoiding dense 2x2 clusters
                int neighborCount = 0;
                foreach (var d in Dirs)
                    if (set.Contains(next + d)) neighborCount++;

                if (neighborCount <= 1 || UnityEngine.Random.value < 0.2f)
                {
                    list.Add(next);
                    set.Add(next);
                }
            }

            return list;
        }

        private Dictionary<Vector2Int, int> ComputeBfsDistances(List<Vector2Int> coords)
        {
            var set = new HashSet<Vector2Int>(coords);
            var dist = new Dictionary<Vector2Int, int> { [Vector2Int.zero] = 0 };
            var q = new Queue<Vector2Int>();
            q.Enqueue(Vector2Int.zero);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                int nextDist = dist[cur] + 1;
                foreach (var d in Dirs)
                {
                    var nb = cur + d;
                    if (set.Contains(nb) && !dist.ContainsKey(nb))
                    {
                        dist[nb] = nextDist;
                        q.Enqueue(nb);
                    }
                }
            }
            return dist;
        }

        private int CountNeighbors(Vector2Int cell, Dictionary<Vector2Int, int> allCells)
        {
            int count = 0;
            foreach (var d in Dirs)
                if (allCells.ContainsKey(cell + d)) count++;
            return count;
        }

        private void BuildRoomGeometry(GeneratedRoomInfo info, ref int combatIdx)
        {
            Vector3 center = new Vector3(info.GridPos.x * cellSpacing, 0f, info.GridPos.y * cellSpacing);
            var roomGo = new GameObject($"Room_{info.RoomType}_{info.GridPos.x}_{info.GridPos.y}");
            roomGo.transform.SetParent(transform);
            roomGo.transform.position = center;

            CreateBox(roomGo.transform, "Floor", center + new Vector3(0f, -0.5f, 0f), new Vector3(roomSize, 1f, roomSize), floorMat);

            float half = roomSize * 0.5f;
            float wallH = 5f;
            float wallY = 2.5f;

            bool hasN = _rooms.ContainsKey(info.GridPos + Vector2Int.up);
            bool hasS = _rooms.ContainsKey(info.GridPos + Vector2Int.down);
            bool hasE = _rooms.ContainsKey(info.GridPos + Vector2Int.right);
            bool hasW = _rooms.ContainsKey(info.GridPos + Vector2Int.left);

            BuildWall(roomGo.transform, "North", center + new Vector3(0f, wallY, half + 0.5f), roomSize, wallH, true, hasN);
            BuildWall(roomGo.transform, "South", center + new Vector3(0f, wallY, -half - 0.5f), roomSize, wallH, true, hasS);
            BuildWall(roomGo.transform, "East", center + new Vector3(half + 0.5f, wallY, 0f), roomSize, wallH, false, hasE);
            BuildWall(roomGo.transform, "West", center + new Vector3(-half - 0.5f, wallY, 0f), roomSize, wallH, false, hasW);

            if (info.RoomType == DungeonRoomType.Start)
            {
                if (chestPrefab != null)
                    Instantiate(chestPrefab, center + new Vector3(4.5f, 0f, 4f), Quaternion.identity, roomGo.transform);
                return;
            }

            if (info.RoomType == DungeonRoomType.Treasure)
            {
                if (chestPrefab != null)
                    Instantiate(chestPrefab, center, Quaternion.identity, roomGo.transform);
                return;
            }

            // Doors for Combat & Boss rooms
            var doors = new List<DoorController>();
            if (hasN) doors.Add(CreateDoor(roomGo.transform, "Door_N", center + new Vector3(0f, 2.25f, half), true));
            if (hasS) doors.Add(CreateDoor(roomGo.transform, "Door_S", center + new Vector3(0f, 2.25f, -half), true));
            if (hasE) doors.Add(CreateDoor(roomGo.transform, "Door_E", center + new Vector3(half, 2.25f, 0f), false));
            if (hasW) doors.Add(CreateDoor(roomGo.transform, "Door_W", center + new Vector3(-half, 2.25f, 0f), false));

            // Cover pillars
            CreateBox(roomGo.transform, "Pillar_NW", center + new Vector3(-5.5f, 2f, 5.5f), new Vector3(2.2f, 4f, 2.2f), wallMat);
            CreateBox(roomGo.transform, "Pillar_NE", center + new Vector3(5.5f, 2f, 5.5f), new Vector3(2.2f, 4f, 2.2f), wallMat);
            CreateBox(roomGo.transform, "Pillar_SW", center + new Vector3(-5.5f, 2f, -5.5f), new Vector3(2.2f, 4f, 2.2f), wallMat);
            CreateBox(roomGo.transform, "Pillar_SE", center + new Vector3(5.5f, 2f, -5.5f), new Vector3(2.2f, 4f, 2.2f), wallMat);

            var trigger = roomGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 2f, 0f);
            trigger.size = new Vector3(roomSize - 2.5f, 4f, roomSize - 2.5f);

            var rc = roomGo.AddComponent<RoomController>();
            info.Controller = rc;

            if (info.RoomType == DungeonRoomType.Boss)
            {
                BossController spawnedBoss = null;
                if (bossPrefab != null)
                {
                    var bGo = Instantiate(bossPrefab, center, Quaternion.identity, roomGo.transform);
                    spawnedBoss = bGo.GetComponent<BossController>();
                }
                rc.InitializeRuntime($"FLOOR {RunManager.CurrentFloor} BOSS", doors, null, spawnedBoss, null);
                rc.OnRoomCleared += _ =>
                {
                    if (portalPrefab != null)
                        Instantiate(portalPrefab, center, Quaternion.identity);
                    GameHUD.Instance?.ShowFloorCleared();
                };
            }
            else
            {
                var spawnedEnemies = new List<EnemyBase>();
                int enemyCount = UnityEngine.Random.Range(3, 6);
                Vector3[] spawnOffsets =
                {
                    new Vector3(-7f, 0f, 0f),
                    new Vector3(7f, 0f, 0f),
                    new Vector3(0f, 0f, 7f),
                    new Vector3(0f, 0f, -7f),
                    new Vector3(0f, 0f, 0f)
                };

                for (int i = 0; i < enemyCount && enemyPrefabs.Count > 0; i++)
                {
                    GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Count)];
                    var eGo = Instantiate(prefab, center + spawnOffsets[i % spawnOffsets.Length], Quaternion.identity, roomGo.transform);
                    if (eGo.TryGetComponent<EnemyBase>(out var eb))
                        spawnedEnemies.Add(eb);
                }

                rc.InitializeRuntime($"SECTOR {combatIdx:00}", doors, spawnedEnemies, null, chestPrefab);
                combatIdx++;
            }
        }

        private void BuildWall(Transform parent, string side, Vector3 pos, float length, float height, bool horizontal, bool hasDoorway)
        {
            if (!hasDoorway)
            {
                Vector3 scale = horizontal ? new Vector3(length + 2f, height, 1f) : new Vector3(1f, height, length + 2f);
                CreateBox(parent, $"Wall_{side}", pos, scale, wallMat);
                return;
            }

            float doorW = 6f;
            float segLen = (length - doorW) * 0.5f;
            float offset = doorW * 0.5f + segLen * 0.5f;

            if (horizontal)
            {
                CreateBox(parent, $"Wall_{side}_L", pos + new Vector3(-offset, 0f, 0f), new Vector3(segLen + 1f, height, 1f), wallMat);
                CreateBox(parent, $"Wall_{side}_R", pos + new Vector3(offset, 0f, 0f), new Vector3(segLen + 1f, height, 1f), wallMat);
            }
            else
            {
                CreateBox(parent, $"Wall_{side}_L", pos + new Vector3(0f, 0f, -offset), new Vector3(1f, height, segLen + 1f), wallMat);
                CreateBox(parent, $"Wall_{side}_R", pos + new Vector3(0f, 0f, offset), new Vector3(1f, height, segLen + 1f), wallMat);
            }
        }

        private void BuildCorridor(Vector2Int from, Vector2Int dir)
        {
            float corLen = cellSpacing - roomSize;
            float corW = 6f;
            Vector3 pA = new Vector3(from.x * cellSpacing, 0f, from.y * cellSpacing);
            Vector3 pB = new Vector3((from.x + dir.x) * cellSpacing, 0f, (from.y + dir.y) * cellSpacing);
            Vector3 mid = (pA + pB) * 0.5f;

            bool vertical = dir.y != 0;
            var cor = new GameObject($"Corridor_{from.x}_{from.y}_{(vertical ? "N" : "E")}");
            cor.transform.SetParent(transform);

            if (vertical)
            {
                CreateBox(cor.transform, "Floor", mid + new Vector3(0f, -0.5f, 0f), new Vector3(corW, 1f, corLen + 2f), floorMat);
                CreateBox(cor.transform, "Wall_W", mid + new Vector3(-corW * 0.5f - 0.5f, 2.5f, 0f), new Vector3(1f, 5f, corLen + 2f), wallMat);
                CreateBox(cor.transform, "Wall_E", mid + new Vector3(corW * 0.5f + 0.5f, 2.5f, 0f), new Vector3(1f, 5f, corLen + 2f), wallMat);
            }
            else
            {
                CreateBox(cor.transform, "Floor", mid + new Vector3(0f, -0.5f, 0f), new Vector3(corLen + 2f, 1f, corW), floorMat);
                CreateBox(cor.transform, "Wall_S", mid + new Vector3(0f, 2.5f, -corW * 0.5f - 0.5f), new Vector3(corLen + 2f, 5f, 1f), wallMat);
                CreateBox(cor.transform, "Wall_N", mid + new Vector3(0f, 2.5f, corW * 0.5f + 0.5f), new Vector3(corLen + 2f, 5f, 1f), wallMat);
            }
        }

        private DoorController CreateDoor(Transform parent, string name, Vector3 pos, bool horizontal)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = horizontal ? new Vector3(6f, 4.5f, 0.8f) : new Vector3(0.8f, 4.5f, 6f);
            if (doorMat != null) go.GetComponent<Renderer>().sharedMaterial = doorMat;
            return go.AddComponent<DoorController>();
        }

        private static void CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
