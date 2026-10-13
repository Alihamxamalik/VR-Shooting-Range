using System;
using System.Collections.Generic;
using RangeControl.Core;
using RangeControl.Range;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>
    /// Puts pooled targets on the rails at the panel's spawn interval and keeps the rails at the panel's
    /// distance. Interval, speed, pattern and size are read live, so a change on the panel shows on the next
    /// target at the latest.
    /// </summary>
    public sealed class TargetSpawner : MonoBehaviour
    {
        [SerializeField] RangeSettings m_Settings;
        [SerializeField] FlipTarget m_SilhouettePrefab;
        [SerializeField] FlipTarget m_NoShootPrefab;
        [SerializeField] TargetRail[] m_Rails = Array.Empty<TargetRail>();
        [SerializeField] int m_Prewarm = 6;
        [SerializeField] int m_MaxActive = 12;
        [Tooltip("Metres per second the rails slide when the target distance changes.")]
        [SerializeField] float m_RailTravelSpeed = 12f;
        [SerializeField] uint m_Seed = 20261013;

        readonly List<FlipTarget> m_Active = new List<FlipTarget>(16);
        PrefabPool<FlipTarget> m_Silhouettes;
        PrefabPool<FlipTarget> m_NoShoots;
        Action<FlipTarget> m_OnTargetFinished;
        Rng m_Rng;
        bool m_Running;
        float m_Timer;
        int m_Spawned;
        int m_Limit;
        float m_NoShootChance;
        int m_NextRail;

        public bool IsRunning => m_Running;
        public int Spawned => m_Spawned;
        public int ActiveCount => m_Active.Count;

        /// <summary>Targets still to come in a limited round, including those on the rails. Zero when endless.</summary>
        public int Remaining => m_Limit > 0 ? Mathf.Max(0, m_Limit - m_Spawned) + m_Active.Count : 0;

        /// <summary>True once a limited round has spawned its last target and the rails are clear.</summary>
        public bool IsExhausted => m_Running && m_Limit > 0 && m_Spawned >= m_Limit && m_Active.Count == 0;

        void Awake()
        {
            m_Rng = new Rng(m_Seed);
            m_OnTargetFinished = OnTargetFinished;
            m_Silhouettes = new PrefabPool<FlipTarget>(m_SilhouettePrefab, transform, m_Prewarm);
            if (m_NoShootPrefab != null)
                m_NoShoots = new PrefabPool<FlipTarget>(m_NoShootPrefab, transform, Mathf.Max(1, m_Prewarm / 3));
            SnapRails();
        }

        /// <summary>Starts spawning. A limit of zero spawns until <see cref="Stop"/>.</summary>
        public void Begin(int targetLimit, float noShootChance)
        {
            Stop();
            m_Running = true;
            m_Limit = Mathf.Max(0, targetLimit);
            m_NoShootChance = m_NoShoots != null ? Mathf.Clamp01(noShootChance) : 0f;
            m_Spawned = 0;
            m_Timer = 0f;
        }

        /// <summary>Stops spawning and clears the rails.</summary>
        public void Stop()
        {
            m_Running = false;
            for (var i = m_Active.Count - 1; i >= 0; i--)
                m_Active[i].Abort();
        }

        void Update()
        {
            MoveRails(Time.deltaTime);
            if (!m_Running)
                return;

            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f || m_Active.Count >= m_MaxActive || m_Rails.Length == 0)
                return;
            if (m_Limit > 0 && m_Spawned >= m_Limit)
                return;

            Spawn();
            m_Timer = m_Settings.SpawnInterval;
        }

        void Spawn()
        {
            var noShoot = m_NoShootChance > 0f && m_Rng.NextFloat() < m_NoShootChance;
            var pattern = TargetMovers.Effective(m_Settings.Pattern, m_Settings.TargetSpeed);
            var rail = m_Rails[m_NextRail];
            m_NextRail = (m_NextRail + 1) % m_Rails.Length;

            var target = (noShoot ? m_NoShoots : m_Silhouettes).Get();
            m_Active.Add(target);
            // A no-shoot does not count toward the round's target limit: it is an obstacle, not a target to hit.
            if (!noShoot)
                m_Spawned++;

            target.Launch(new TargetLaunch
            {
                Rail = rail,
                Settings = m_Settings,
                Type = noShoot ? TargetType.NoShoot : (pattern == MovementPattern.PopUp ? TargetType.PopUp : TargetType.MovingSilhouette),
                Pattern = pattern,
                Seed = m_Rng.NextUInt(),
                OnFinished = m_OnTargetFinished,
            });
        }

        void OnTargetFinished(FlipTarget target)
        {
            var index = m_Active.IndexOf(target);
            if (index < 0)
                return;
            m_Active[index] = m_Active[m_Active.Count - 1];
            m_Active.RemoveAt(m_Active.Count - 1);
            (target.Type == TargetType.NoShoot ? m_NoShoots : m_Silhouettes).Release(target);
        }

        void MoveRails(float deltaTime)
        {
            var distance = m_Settings.TargetDistance;
            foreach (var rail in m_Rails)
            {
                var local = rail.transform.localPosition;
                var goal = distance + rail.DepthOffset;
                if (Mathf.Approximately(local.z, goal))
                    continue;
                local.z = Mathf.MoveTowards(local.z, goal, m_RailTravelSpeed * deltaTime);
                rail.transform.localPosition = local;
            }
        }

        void SnapRails()
        {
            var distance = m_Settings.TargetDistance;
            foreach (var rail in m_Rails)
            {
                var local = rail.transform.localPosition;
                local.z = distance + rail.DepthOffset;
                rail.transform.localPosition = local;
            }
        }
    }
}
