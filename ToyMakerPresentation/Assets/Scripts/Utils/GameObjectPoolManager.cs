using System.Collections.Generic;
using UnityEngine;

namespace ToyProject.Utils
{
    /// <summary>
    /// 프리팹 string key 기반의 제너럴 Unity 오브젝트 풀 매니저
    /// </summary>
    public class GameObjectPoolManager : MonoBehaviour
    {
        private class Pool
        {
            public Queue<GameObject> objects = new();
            public GameObject prefab;
            public Transform parent;
            public int capacity;
        }

        private readonly Dictionary<string, Pool> pools = new();

        /// <summary>
        /// 풀 생성 및 초기화
        /// </summary>
        public void CreatePool(string key, GameObject prefab, int initialSize, Transform parent = null)
        {
            if (pools.ContainsKey(key))
                return;
            var pool = new Pool { prefab = prefab, parent = parent, capacity = initialSize };
            for (int i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(prefab, parent);
                obj.SetActive(false);
                pool.objects.Enqueue(obj);
            }
            pools[key] = pool;
        }

        /// <summary>
        /// 풀에서 오브젝트 꺼내기 (없으면 새로 생성)
        /// </summary>
        public GameObject Get(string key)
        {
            if (!pools.TryGetValue(key, out var pool))
                return null;
            if (pool.objects.Count > 0)
            {
                var obj = pool.objects.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            // 풀에 남은 게 없으면 새로 생성
            var newObj = Instantiate(pool.prefab, pool.parent);
            newObj.SetActive(true);
            return newObj;
        }

        /// <summary>
        /// 오브젝트를 풀에 반환
        /// </summary>
        public void Return(string key, GameObject obj)
        {
            if (!pools.TryGetValue(key, out var pool))
            {
                Destroy(obj);
                return;
            }
            obj.SetActive(false);
            pool.objects.Enqueue(obj);
        }

        /// <summary>
        /// 풀의 크기를 늘리거나 줄임 (capacity 변경)
        /// </summary>
        public void ResizePool(string key, int newSize)
        {
            if (!pools.TryGetValue(key, out var pool))
                return;
            int diff = newSize - pool.objects.Count;
            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    var obj = Instantiate(pool.prefab, pool.parent);
                    obj.SetActive(false);
                    pool.objects.Enqueue(obj);
                }
            }
            else if (diff < 0)
            {
                for (int i = 0; i < -diff; i++)
                {
                    if (pool.objects.Count > 0)
                    {
                        var obj = pool.objects.Dequeue();
                        Destroy(obj);
                    }
                }
            }
            pool.capacity = newSize;
        }

        /// <summary>
        /// 특정 풀 완전 삭제
        /// </summary>
        public void DisposePool(string key)
        {
            if (!pools.TryGetValue(key, out var pool))
                return;
            while (pool.objects.Count > 0)
            {
                var obj = pool.objects.Dequeue();
                Destroy(obj);
            }
            pools.Remove(key);
        }

        /// <summary>
        /// 모든 풀 완전 삭제
        /// </summary>
        public void DisposeAll()
        {
            foreach (var key in new List<string>(pools.Keys))
            {
                DisposePool(key);
            }
        }
    }
}
