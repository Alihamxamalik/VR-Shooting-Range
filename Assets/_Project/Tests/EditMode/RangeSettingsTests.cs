using NUnit.Framework;
using RangeControl.Core;
using RangeControl.Range;
using UnityEngine;

namespace RangeControl.Tests
{
    public class RangeSettingsTests
    {
        RangeSettings m_Settings;
        int m_Changes;

        [SetUp]
        public void SetUp()
        {
            m_Settings = ScriptableObject.CreateInstance<RangeSettings>();
            m_Changes = 0;
            m_Settings.Changed += () => m_Changes++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Settings);
        }

        [Test]
        public void Defaults_MatchTheDesignDocument()
        {
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(15f));
            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(1.5f));
            Assert.That(m_Settings.Pattern, Is.EqualTo(MovementPattern.LeftToRight));
            Assert.That(m_Settings.SpawnInterval, Is.EqualTo(2f));
            Assert.That(m_Settings.PopUpExposure, Is.EqualTo(1.5f));
            Assert.That(m_Settings.TargetsPerRound, Is.EqualTo(20));
            Assert.That(m_Settings.TargetSize, Is.EqualTo(TargetSize.Normal));
            Assert.That(m_Settings.FireRatePercent, Is.EqualTo(100));
            Assert.That(m_Settings.InfiniteAmmo, Is.False);
            Assert.That(m_Settings.LaserSight, Is.False);
            Assert.That(m_Settings.RoundTimer, Is.EqualTo(0));
        }

        [Test]
        public void Distance_ClampsToFiveAndFiftyAndSnapsToFiveMetres()
        {
            m_Settings.TargetDistance = 2f;
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(5f));
            m_Settings.TargetDistance = 75f;
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(50f));
            m_Settings.TargetDistance = 23f;
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(25f));
        }

        [Test]
        public void LongRange_OpensOutToOneHundredMetresAndClosesAgain()
        {
            m_Settings.LongRange = true;
            m_Settings.TargetDistance = 100f;
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(100f));

            m_Settings.LongRange = false;
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(50f));
        }

        [Test]
        public void Speed_ClampsToZeroAndSixAndSnapsToHalfSteps()
        {
            m_Settings.TargetSpeed = -1f;
            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(0f));
            m_Settings.TargetSpeed = 9f;
            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(6f));
            m_Settings.TargetSpeed = 2.3f;
            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(2.5f));
        }

        [Test]
        public void IntervalExposureAndFireRate_Clamp()
        {
            m_Settings.SpawnInterval = 0.1f;
            Assert.That(m_Settings.SpawnInterval, Is.EqualTo(0.5f));
            m_Settings.SpawnInterval = 99f;
            Assert.That(m_Settings.SpawnInterval, Is.EqualTo(5f));
            m_Settings.PopUpExposure = 0.6f;
            Assert.That(m_Settings.PopUpExposure, Is.EqualTo(0.5f));
            m_Settings.PopUpExposure = 10f;
            Assert.That(m_Settings.PopUpExposure, Is.EqualTo(3f));
            m_Settings.FireRatePercent = 10;
            Assert.That(m_Settings.FireRatePercent, Is.EqualTo(50));
            m_Settings.FireRatePercent = 400;
            Assert.That(m_Settings.FireRatePercent, Is.EqualTo(150));
        }

        [Test]
        public void TargetsPerRound_StepsFromFiveToFiftyThenEndless()
        {
            m_Settings.TargetsPerRound = 45;
            m_Settings.StepTargetsPerRound(1);
            Assert.That(m_Settings.TargetsPerRound, Is.EqualTo(50));
            m_Settings.StepTargetsPerRound(1);
            Assert.That(m_Settings.TargetsPerRound, Is.EqualTo(RangeSettings.Endless));
            m_Settings.StepTargetsPerRound(1);
            Assert.That(m_Settings.TargetsPerRound, Is.EqualTo(RangeSettings.Endless));
            m_Settings.StepTargetsPerRound(-1);
            Assert.That(m_Settings.TargetsPerRound, Is.EqualTo(50));

            m_Settings.TargetsPerRound = 5;
            m_Settings.StepTargetsPerRound(-1);
            Assert.That(m_Settings.TargetsPerRound, Is.EqualTo(5));
        }

        [Test]
        public void RoundTimer_StepsThroughOffThirtySixtyNinety()
        {
            m_Settings.StepRoundTimer(-1);
            Assert.That(m_Settings.RoundTimer, Is.EqualTo(0));
            m_Settings.StepRoundTimer(1);
            Assert.That(m_Settings.RoundTimer, Is.EqualTo(30));
            m_Settings.StepRoundTimer(1);
            m_Settings.StepRoundTimer(1);
            m_Settings.StepRoundTimer(1);
            Assert.That(m_Settings.RoundTimer, Is.EqualTo(90));
        }

        [Test]
        public void Changed_IsRaisedOncePerRealChange()
        {
            m_Settings.TargetSpeed = 3f;
            m_Settings.TargetSpeed = 3f;
            m_Settings.TargetSpeed = 3.1f;
            m_Settings.InfiniteAmmo = true;
            m_Settings.InfiniteAmmo = true;

            Assert.That(m_Changes, Is.EqualTo(2));
        }

        [Test]
        public void Locked_BlocksThePlayerButNotTheRoundController()
        {
            m_Settings.Locked = true;
            m_Settings.TargetDistance = 40f;
            m_Settings.Pattern = MovementPattern.Strafe;
            m_Settings.InfiniteAmmo = true;
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(15f));
            Assert.That(m_Settings.Pattern, Is.EqualTo(MovementPattern.LeftToRight));
            Assert.That(m_Settings.InfiniteAmmo, Is.False);

            var tier = RangeSettingsData.Default;
            tier.targetDistance = 25f;
            tier.targetSpeed = 4f;
            m_Settings.Apply(tier);
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(25f));
            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(4f));
        }

        [Test]
        public void DifficultyFactor_FollowsTheLiveValues()
        {
            Assert.That(m_Settings.DifficultyFactor, Is.EqualTo(1.375f).Within(1e-4f));
            m_Settings.TargetDistance = 30f;
            m_Settings.TargetSpeed = 4f;
            m_Settings.TargetSize = TargetSize.Small;
            Assert.That(m_Settings.DifficultyFactor, Is.EqualTo(2f * 2f * 1.3f).Within(1e-4f));
        }

        [Test]
        public void ResetToDefaults_UndoesEverything()
        {
            m_Settings.TargetDistance = 40f;
            m_Settings.Locked = true;
            m_Settings.LongRange = true;
            m_Settings.ResetToDefaults();

            Assert.That(m_Settings.TargetDistance, Is.EqualTo(15f));
            Assert.That(m_Settings.Locked, Is.False);
            Assert.That(m_Settings.DistanceLimit, Is.EqualTo(RangeSettings.MaxDistance));
        }
    }
}
