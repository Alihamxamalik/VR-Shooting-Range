using UnityEngine;
using UnityEngine.Pool;

namespace RangeControl.Core
{
    /// <summary>
    /// A pool of instances of one prefab, built on <see cref="ObjectPool{T}"/>. Everything that would otherwise
    /// be instantiated and destroyed per shot or per target comes from one of these, so a round allocates nothing.
    /// </summary>
    public sealed class PrefabPool<T> where T : Component
    {
        readonly T m_Prefab;
        readonly Transform m_Parent;
        readonly ObjectPool<T> m_Pool;

        public PrefabPool(T prefab, Transform parent, int prewarm = 0, int maxSize = 256)
        {
            m_Prefab = prefab;
            m_Parent = parent;
            m_Pool = new ObjectPool<T>(Create, OnGet, OnRelease, OnDestroy, true, Mathf.Max(1, prewarm), maxSize);

            if (prewarm <= 0)
                return;
            var warm = ListPool<T>.Get();
            for (var i = 0; i < prewarm; i++)
                warm.Add(m_Pool.Get());
            foreach (var item in warm)
                m_Pool.Release(item);
            ListPool<T>.Release(warm);
        }

        public int CountActive => m_Pool.CountActive;
        public int CountInactive => m_Pool.CountInactive;

        public T Get()
        {
            return m_Pool.Get();
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var item = m_Pool.Get();
            item.transform.SetPositionAndRotation(position, rotation);
            return item;
        }

        public void Release(T item)
        {
            m_Pool.Release(item);
        }

        public void Clear()
        {
            m_Pool.Clear();
        }

        T Create()
        {
            var item = Object.Instantiate(m_Prefab, m_Parent);
            item.gameObject.SetActive(false);
            return item;
        }

        static void OnGet(T item)
        {
            item.gameObject.SetActive(true);
        }

        void OnRelease(T item)
        {
            item.gameObject.SetActive(false);
            if (item.transform.parent != m_Parent)
                item.transform.SetParent(m_Parent, false);
        }

        static void OnDestroy(T item)
        {
            if (item != null)
                Object.Destroy(item.gameObject);
        }
    }
}
