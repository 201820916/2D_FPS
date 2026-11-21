using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GungeonFPS.Dungeon;

namespace GungeonFPS.UI
{
    public class MinimapUI : MonoBehaviour
    {
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private float cellSize = 26f;
        [SerializeField] private float cellGap = 6f;

        private readonly Dictionary<Vector2Int, Image> _cellImages = new();
        private Transform _player;

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;

            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.OnDungeonGenerated += RebuildMinimap;
                if (DungeonGenerator.Instance.Rooms.Count > 0)
                    RebuildMinimap();
            }
        }

        private void OnDestroy()
        {
            if (DungeonGenerator.Instance != null)
                DungeonGenerator.Instance.OnDungeonGenerated -= RebuildMinimap;
        }

        private void RebuildMinimap()
        {
            if (mapContainer == null || DungeonGenerator.Instance == null) return;

            foreach (var img in _cellImages.Values)
                if (img != null) Destroy(img.gameObject);
            _cellImages.Clear();

            var rooms = DungeonGenerator.Instance.Rooms;
            if (rooms.Count == 0) return;

            Vector2 sum = Vector2.zero;
            foreach (var pos in rooms.Keys) sum += pos;
            Vector2 center = sum / rooms.Count;

            float step = cellSize + cellGap;
            foreach (var kv in rooms)
            {
                var go = new GameObject($"Cell_{kv.Key.x}_{kv.Key.y}");
                go.transform.SetParent(mapContainer, false);
                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                var rect = img.rectTransform;
                rect.sizeDelta = new Vector2(cellSize, cellSize);
                rect.anchoredPosition = (kv.Key - center) * step;
                _cellImages[kv.Key] = img;
            }
        }

        private void Update()
        {
            if (DungeonGenerator.Instance == null || _cellImages.Count == 0) return;

            if (_player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
                else return;
            }

            Vector2Int playerGrid = DungeonGenerator.Instance.WorldToGrid(_player.position);
            var rooms = DungeonGenerator.Instance.Rooms;

            foreach (var kv in _cellImages)
            {
                if (!rooms.TryGetValue(kv.Key, out var info)) continue;

                if (kv.Key == playerGrid)
                {
                    info.Visited = true;
                    kv.Value.color = new Color(0.25f, 0.95f, 1f, 1f); // Current room (Cyan)
                }
                else if (info.RoomType == DungeonRoomType.Boss)
                {
                    kv.Value.color = (info.Controller != null && info.Controller.IsCleared)
                        ? new Color(0.35f, 0.4f, 0.45f, 0.9f)
                        : new Color(1f, 0.18f, 0.35f, 0.95f); // Boss (Red)
                }
                else if (info.RoomType == DungeonRoomType.Treasure)
                {
                    kv.Value.color = new Color(1f, 0.82f, 0.2f, 0.95f); // Treasure (Gold)
                }
                else if (info.RoomType == DungeonRoomType.Start || (info.Controller != null && info.Controller.IsCleared))
                {
                    kv.Value.color = new Color(0.45f, 0.52f, 0.6f, 0.85f); // Cleared / Start
                }
                else
                {
                    kv.Value.color = new Color(0.22f, 0.26f, 0.34f, 0.85f); // Uncleared combat
                }
            }
        }
    }
}
