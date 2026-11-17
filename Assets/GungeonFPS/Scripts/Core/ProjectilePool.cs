using System.Collections.Generic;
using UnityEngine;

namespace GungeonFPS.Core
{
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
            }

            GameObject obj = null;
            while (queue.Count > 0 && obj == null)
            {
                obj = queue.Dequeue();
            }

            if (obj == null)
            {
                obj = Instantiate(prefab, position, rotation, transform);
                _instanceToPrefab[obj] = prefab;
            }
            else
            {
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }

            return obj;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            if (!instance.activeSelf) return;

            instance.SetActive(false);
            if (_instanceToPrefab.TryGetValue(instance, out var prefab) && _pools.TryGetValue(prefab, out var queue))
            {
                queue.Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }
    }
}
