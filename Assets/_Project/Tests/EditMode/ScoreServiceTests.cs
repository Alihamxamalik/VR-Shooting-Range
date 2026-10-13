using NUnit.Framework;
using RangeControl.Core;

namespace RangeControl.Tests
{
    public class ScoreServiceTests
    {
        ScoreService m_Score;

        [SetUp]
        public void SetUp()
        {
            m_Score = new ScoreService();
            m_Score.Begin(true, 1f);
        }

        void HitAndShot(int shotId, int basePoints)
        {
            m_Score.RegisterHit(TestData.Hit(shotId, basePoints));
            m_Score.RegisterShot(TestData.Shot(shotId));
        }

        [Test]
        public void FirstHit_ScoresBasePoints()
        {
            Assert.That(m_Score.RegisterHit(TestData.Hit(1, 100)), Is.EqualTo(100));
            Assert.That(m_Score.Score, Is.EqualTo(100));
        }

        [Test]
        public void ConsecutiveHits_BuildTheCombo()
        {
            HitAndShot(1, 100);
            HitAndShot(2, 100);
            HitAndShot(3, 100);

            // 100 + 110 + 121
            Assert.That(m_Score.Score, Is.EqualTo(331));
            Assert.That(m_Score.Streak, Is.EqualTo(3));
        }

        [Test]
        public void Miss_ResetsTheCombo()
        {
            HitAndShot(1, 100);
            HitAndShot(2, 100);
            Assert.That(m_Score.RegisterShot(TestData.Shot(3)), Is.False);
            Assert.That(m_Score.Streak, Is.EqualTo(0));

            HitAndShot(4, 100);
            Assert.That(m_Score.Score, Is.EqualTo(100 + 110 + 100));
        }

        [Test]
        public void NoShoot_TakesPointsAndResetsTheCombo()
        {
            HitAndShot(1, 100);
            var points = m_Score.RegisterHit(TestData.Hit(2, ScoreRules.NoShootPenalty));
            m_Score.RegisterShot(TestData.Shot(2));

            Assert.That(points, Is.EqualTo(-250));
            Assert.That(m_Score.Score, Is.EqualTo(-150));
            Assert.That(m_Score.Streak, Is.EqualTo(0));
            Assert.That(m_Score.Penalties, Is.EqualTo(1));
        }

        [Test]
        public void NoShoot_DoesNotCountAsAHit()
        {
            m_Score.RegisterHit(TestData.Hit(1, ScoreRules.NoShootPenalty));
            m_Score.RegisterShot(TestData.Shot(1));

            Assert.That(m_Score.Hits, Is.EqualTo(0));
            Assert.That(m_Score.Accuracy, Is.EqualTo(0f));
        }

        [Test]
        public void ShotgunPellets_ShareOneStepOfComboAndCountAsOneHit()
        {
            HitAndShot(1, 100);
            for (var pellet = 0; pellet < 8; pellet++)
                m_Score.RegisterHit(TestData.Hit(2, 10));
            m_Score.RegisterShot(TestData.Shot(2));

            Assert.That(m_Score.Streak, Is.EqualTo(2));
            Assert.That(m_Score.Hits, Is.EqualTo(2));
            Assert.That(m_Score.Shots, Is.EqualTo(2));
            Assert.That(m_Score.Score, Is.EqualTo(100 + 8 * 11));
        }

        [Test]
        public void Multiplier_ScalesEveryHit()
        {
            m_Score.Begin(true, 2f);
            HitAndShot(1, 100);
            HitAndShot(2, 150);

            // 100 x 2, then 150 x 1.1 x 2
            Assert.That(m_Score.Score, Is.EqualTo(200 + 330));
        }

        [Test]
        public void WithoutCombo_RingTotalsStayHonest()
        {
            m_Score.Begin(false, 1f);
            for (var shot = 1; shot <= 10; shot++)
                HitAndShot(shot, 10);

            Assert.That(m_Score.Score, Is.EqualTo(100));
            Assert.That(m_Score.Combo, Is.EqualTo(1f));
        }

        [Test]
        public void PaperOutsideTheRings_ScoresNothingAndIsAMiss()
        {
            HitAndShot(1, 100);
            Assert.That(m_Score.RegisterHit(TestData.Hit(2, 0)), Is.EqualTo(0));
            Assert.That(m_Score.RegisterShot(TestData.Shot(2)), Is.False);
            Assert.That(m_Score.Streak, Is.EqualTo(0));
        }

        [Test]
        public void Accuracy_IsScoringShotsOverShots()
        {
            HitAndShot(1, 100);
            m_Score.RegisterShot(TestData.Shot(2));
            m_Score.RegisterShot(TestData.Shot(3));
            HitAndShot(4, 100);

            Assert.That(m_Score.Accuracy, Is.EqualTo(0.5f));
        }

        [Test]
        public void TracksInnerXLongestHitAndBestStreak()
        {
            m_Score.RegisterHit(TestData.Hit(1, 10, null, 25f, true));
            m_Score.RegisterShot(TestData.Shot(1));
            m_Score.RegisterHit(TestData.Hit(2, 10, null, 48f));
            m_Score.RegisterShot(TestData.Shot(2));
            m_Score.RegisterShot(TestData.Shot(3));

            Assert.That(m_Score.InnerXCount, Is.EqualTo(1));
            Assert.That(m_Score.LongestHit, Is.EqualTo(48f));
            Assert.That(m_Score.BestStreak, Is.EqualTo(2));
            Assert.That(m_Score.Streak, Is.EqualTo(0));
        }

        [Test]
        public void TalliesShotsAndHitsPerWeapon()
        {
            var pistol = TestData.Weapon("m1911");
            var rifle = TestData.Weapon("ak74");

            m_Score.RegisterHit(TestData.Hit(1, 100, pistol, 12f));
            m_Score.RegisterShot(TestData.Shot(1, pistol));
            m_Score.RegisterShot(TestData.Shot(2, pistol));
            m_Score.RegisterHit(TestData.Hit(3, 100, rifle, 40f));
            m_Score.RegisterShot(TestData.Shot(3, rifle));

            Assert.That(m_Score.Weapons["m1911"].Shots, Is.EqualTo(2));
            Assert.That(m_Score.Weapons["m1911"].Hits, Is.EqualTo(1));
            Assert.That(m_Score.Weapons["ak74"].Shots, Is.EqualTo(1));
            Assert.That(m_Score.Weapons["ak74"].LongestHit, Is.EqualTo(40f));
        }

        [Test]
        public void ExpiredTargetsAreCountedButNoShootsAreNot()
        {
            m_Score.RegisterExpiry(new TargetExpiry(TargetType.MovingSilhouette, default));
            m_Score.RegisterExpiry(new TargetExpiry(TargetType.NoShoot, default));

            Assert.That(m_Score.TargetsMissed, Is.EqualTo(1));
        }

        [Test]
        public void Begin_ClearsThePreviousRound()
        {
            HitAndShot(1, 100);
            m_Score.Begin(true, 1f);

            Assert.That(m_Score.Score, Is.EqualTo(0));
            Assert.That(m_Score.Shots, Is.EqualTo(0));
            Assert.That(m_Score.Weapons.Count, Is.EqualTo(0));
        }
    }
}
