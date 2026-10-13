using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RangeControl.Core;
using RangeControl.Range;
using RangeControl.Targets;
using RangeControl.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RangeControl.Tests
{
    /// <summary>
    /// Shared set-up for tests that run in the real range scene. Each test gets the scene loaded fresh, the
    /// stats pointed at a temporary file, comfort settings back at their defaults and a record of every event
    /// raised on the channel.
    /// </summary>
    public abstract class RangeSceneFixture
    {
        const string k_Scene = "Assets/_Project/Scenes/Range.unity";
        const string k_Data = "Assets/_Project/Data/";

        protected GameModeController m_Controller;
        protected RangeEvents m_Events;
        protected RangeSettings m_Settings;
        protected ComfortSettings m_Comfort;
        protected Dictionary<string, Weapon> m_Weapons;
        protected string m_StatsFolder;

        protected readonly List<TargetHit> m_Hits = new List<TargetHit>();
        protected readonly List<ScoreAward> m_Awards = new List<ScoreAward>();
        protected readonly List<RoundResult> m_Results = new List<RoundResult>();
        protected int m_Shots;
        protected int m_Traces;
        protected int m_Tracers;
        protected int m_DryFires;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(k_Scene, LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Controller = Object.FindFirstObjectByType<GameModeController>();
            m_StatsFolder = Path.Combine(Path.GetTempPath(), "RangeControlPlayTests-" + Guid.NewGuid().ToString("N"));
            var stats = new StatsService(Path.Combine(m_StatsFolder, StatsService.FileName));
            stats.Load();
            m_Controller.UseStats(stats);

            m_Events = Load<RangeEvents>("RangeEvents.asset");
            m_Settings = Load<RangeSettings>("RangeSettings.asset");
            m_Comfort = Load<ComfortSettings>("ComfortSettings.asset");
            m_Comfort.ResetToDefaults();
            m_Weapons = Object.FindObjectsByType<Weapon>(FindObjectsSortMode.None).ToDictionary(weapon => weapon.Data.Id);

            m_Hits.Clear();
            m_Awards.Clear();
            m_Results.Clear();
            m_Shots = m_Traces = m_Tracers = m_DryFires = 0;
            m_Events.TargetHit += OnTargetHit;
            m_Events.ScoreAwarded += OnScoreAwarded;
            m_Events.RoundEnded += OnRoundEnded;
            m_Events.ShotFired += OnShotFired;
            m_Events.BulletTraced += OnBulletTraced;
            m_Events.WeaponDryFired += OnDryFired;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            m_Events.TargetHit -= OnTargetHit;
            m_Events.ScoreAwarded -= OnScoreAwarded;
            m_Events.RoundEnded -= OnRoundEnded;
            m_Events.ShotFired -= OnShotFired;
            m_Events.BulletTraced -= OnBulletTraced;
            m_Events.WeaponDryFired -= OnDryFired;
            // Comfort settings are saved as they change; put back anything a test switched on.
            if (m_Comfort.QuickReload)
                m_Comfort.QuickReload = false;
            m_Comfort.ResetToDefaults();
            Time.timeScale = 1f;
            if (Directory.Exists(m_StatsFolder))
                Directory.Delete(m_StatsFolder, true);
            yield return null;
        }

        void OnTargetHit(TargetHit hit) => m_Hits.Add(hit);
        void OnScoreAwarded(ScoreAward award) => m_Awards.Add(award);
        void OnRoundEnded(RoundResult result) => m_Results.Add(result);
        void OnShotFired(ShotInfo shot) => m_Shots++;
        void OnDryFired(Weapon weapon) => m_DryFires++;

        void OnBulletTraced(BulletTrace trace)
        {
            m_Traces++;
            if (trace.ShowTracer)
                m_Tracers++;
        }

        protected static T Load<T>(string name) where T : Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(k_Data + name);
#else
            return null;
#endif
        }

        /// <summary>Puts the gun's muzzle at <paramref name="from"/>, pointing dead at <paramref name="target"/>.</summary>
        protected static void Aim(Weapon weapon, Vector3 from, Vector3 target)
        {
            var rotation = Quaternion.LookRotation(target - from);
            var muzzleLocal = weapon.transform.InverseTransformPoint(weapon.Muzzle.position);
            weapon.transform.SetPositionAndRotation(from - rotation * muzzleLocal, rotation);
        }

        /// <summary>One full pull and release of the trigger, then long enough for recoil and the rate cap to clear.</summary>
        protected static IEnumerator Fire(Weapon weapon, float settle = 0.75f)
        {
            weapon.SetTriggerOverride(1f);
            yield return null;
            yield return null;
            weapon.SetTriggerOverride(0f);
            yield return new WaitForSeconds(settle);
        }

        protected static IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
        {
            var deadline = Time.time + timeout;
            while (!condition())
            {
                if (Time.time > deadline)
                    Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        protected static readonly Vector3 s_FiringPoint = new Vector3(0f, 1.45f, 1.2f);

        protected static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }
    }
}
