using System;
using System.Collections.Generic;
using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Weapons
{
    /// <summary>
    /// Hands out pooled magazines for whichever gun was picked up last. The bench tray and the hip pouch both
    /// draw from it, and a magazine that times out on the floor comes back to it.
    /// </summary>
    public sealed class AmmoSupply : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;

        readonly Dictionary<WeaponData, PrefabPool<Magazine>> m_Pools = new Dictionary<WeaponData, PrefabPool<Magazine>>();
        Action<Magazine> m_Release;

        /// <summary>The gun the player last picked up, or null before the first one.</summary>
        public WeaponData ActiveWeapon { get; private set; }

        public event Action ActiveWeaponChanged;

        void Awake()
        {
            m_Release = Release;
        }

        void OnEnable()
        {
            m_Events.WeaponGrabbed += OnWeaponGrabbed;
        }

        void OnDisable()
        {
            m_Events.WeaponGrabbed -= OnWeaponGrabbed;
        }

        void OnWeaponGrabbed(Weapon weapon)
        {
            if (ActiveWeapon == weapon.Data)
                return;
            ActiveWeapon = weapon.Data;
            ActiveWeaponChanged?.Invoke();
        }

        /// <summary>A full magazine (or one shell) for <paramref name="weapon"/>, or null if it has no magazine prefab.</summary>
        public Magazine Spawn(WeaponData weapon, Vector3 position, Quaternion rotation)
        {
            if (weapon == null || weapon.MagazinePrefab == null)
                return null;

            if (!m_Pools.TryGetValue(weapon, out var pool))
            {
                pool = new PrefabPool<Magazine>(weapon.MagazinePrefab, transform, 0, 24);
                m_Pools.Add(weapon, pool);
            }

            var magazine = pool.Get(position, rotation);
            magazine.Prepare(weapon.ShellFed ? 1 : weapon.MagazineSize, m_Release);
            return magazine;
        }

        void Release(Magazine magazine)
        {
            if (magazine.Weapon != null && m_Pools.TryGetValue(magazine.Weapon, out var pool))
                pool.Release(magazine);
            else
                Destroy(magazine.gameObject);
        }
    }
}
