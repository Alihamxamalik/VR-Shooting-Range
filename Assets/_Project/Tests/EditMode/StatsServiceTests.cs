using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RangeControl.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace RangeControl.Tests
{
    public class StatsServiceTests
    {
        string m_Folder;
        string m_Path;
        DateTime m_Now;

        [SetUp]
        public void SetUp()
        {
            m_Folder = Path.Combine(Path.GetTempPath(), "RangeControlTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_Folder);
            m_Path = Path.Combine(m_Folder, StatsService.FileName);
            m_Now = new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_Folder))
                Directory.Delete(m_Folder, true);
        }

        StatsService NewService()
        {
            return new StatsService(m_Path, () => m_Now);
        }

        static RoundResult Result(int score, string boardKey = "MovingGallery/Fast", GameMode mode = GameMode.MovingGallery, int innerX = 0)
        {
            return new RoundResult { mode = mode, boardKey = boardKey, score = score, shots = 10, hits = 5, accuracy = 0.5f, innerX = innerX, weaponId = "ak74" };
        }

        [Test]
        public void Load_WithNoFile_StartsEmpty()
        {
            var stats = NewService();
            stats.Load();

            Assert.That(stats.Data.weapons, Is.Empty);
            Assert.That(stats.Data.boards, Is.Empty);
            Assert.That(stats.Data.version, Is.EqualTo(StatsService.CurrentVersion));
        }

        [Test]
        public void SaveThenLoad_RoundTripsEverything()
        {
            var stats = NewService();
            var weapons = new Dictionary<string, ScoreService.WeaponTally>
            {
                { "ak74", new ScoreService.WeaponTally { Shots = 30, Hits = 21, LongestHit = 42.5f } },
            };
            stats.RecordRound(Result(1800), weapons);
            stats.Data.tutorialCompleted = true;
            stats.Save();

            var reloaded = NewService();
            reloaded.Load();

            Assert.That(reloaded.BestScore("MovingGallery/Fast"), Is.EqualTo(1800));
            Assert.That(reloaded.FindWeapon("ak74").shots, Is.EqualTo(30));
            Assert.That(reloaded.FindWeapon("ak74").hits, Is.EqualTo(21));
            Assert.That(reloaded.FindWeapon("ak74").Accuracy, Is.EqualTo(0.7f).Within(1e-5f));
            Assert.That(reloaded.Data.longestHit, Is.EqualTo(42.5f));
            Assert.That(reloaded.Data.longestHitWeaponId, Is.EqualTo("ak74"));
            Assert.That(reloaded.Data.tutorialCompleted, Is.True);
            Assert.That(reloaded.FindBoard("MovingGallery/Fast").entries[0].date, Is.EqualTo(m_Now.ToString("o")));
        }

        [Test]
        public void WeaponTallies_AddUpAcrossRounds()
        {
            var stats = NewService();
            var weapons = new Dictionary<string, ScoreService.WeaponTally> { { "m1911", new ScoreService.WeaponTally { Shots = 7, Hits = 4 } } };
            stats.RecordRound(Result(100), weapons);
            stats.RecordRound(Result(100), weapons);

            Assert.That(stats.FindWeapon("m1911").shots, Is.EqualTo(14));
            Assert.That(stats.FindWeapon("m1911").hits, Is.EqualTo(8));
        }

        [Test]
        public void Leaderboard_KeepsTheTopFiveInOrder()
        {
            var stats = NewService();
            foreach (var score in new[] { 300, 900, 100, 700, 500, 800, 200 })
                stats.RecordRound(Result(score), null);

            var scores = stats.FindBoard("MovingGallery/Fast").entries.ConvertAll(entry => entry.score);
            Assert.That(scores, Is.EqualTo(new[] { 900, 800, 700, 500, 300 }));
        }

        [Test]
        public void Leaderboard_IsSeparatePerModeAndTier()
        {
            var stats = NewService();
            stats.RecordRound(Result(900, "MovingGallery/Fast"), null);
            stats.RecordRound(Result(400, "MovingGallery/Slow"), null);

            Assert.That(stats.BestScore("MovingGallery/Fast"), Is.EqualTo(900));
            Assert.That(stats.BestScore("MovingGallery/Slow"), Is.EqualTo(400));
            Assert.That(stats.BestScore("Precision/25 m"), Is.EqualTo(0));
        }

        [Test]
        public void InnerX_BreaksATie()
        {
            var stats = NewService();
            stats.RecordRound(Result(96, "Precision/25 m", GameMode.Precision, 2), null);
            stats.RecordRound(Result(96, "Precision/25 m", GameMode.Precision, 5), null);

            Assert.That(stats.FindBoard("Precision/25 m").entries[0].innerX, Is.EqualTo(5));
        }

        [Test]
        public void PersonalBest_IsFlaggedOnlyWhenTheScoreImproves()
        {
            var stats = NewService();
            var first = Result(500);
            stats.RecordRound(first, null);
            var worse = Result(400);
            stats.RecordRound(worse, null);
            var better = Result(650);
            stats.RecordRound(better, null);

            Assert.That(first.isPersonalBest, Is.True);
            Assert.That(worse.isPersonalBest, Is.False);
            Assert.That(worse.previousBest, Is.EqualTo(500));
            Assert.That(better.isPersonalBest, Is.True);
            Assert.That(better.previousBest, Is.EqualTo(500));
        }

        [Test]
        public void RoundWithNoShots_IsNotEnteredOnTheBoard()
        {
            var stats = NewService();
            var empty = Result(0);
            empty.shots = 0;
            stats.RecordRound(empty, null);

            Assert.That(stats.FindBoard("MovingGallery/Fast"), Is.Null);
            Assert.That(empty.isPersonalBest, Is.False);
        }

        [Test]
        public void BestGroup_KeepsTheSmallestAndOnlyFromPrecision()
        {
            var stats = NewService();
            var gallery = Result(100);
            gallery.groupSizeCm = 1f;
            stats.RecordRound(gallery, null);
            Assert.That(stats.Data.bestGroupCm, Is.EqualTo(0f));

            foreach (var group in new[] { 12.5f, 6.2f, 9f })
            {
                var precision = Result(90, "Precision/25 m", GameMode.Precision);
                precision.groupSizeCm = group;
                stats.RecordRound(precision, null);
            }

            Assert.That(stats.Data.bestGroupCm, Is.EqualTo(6.2f));
        }

        [Test]
        public void CorruptFile_IsSetAsideAndStatsStartFresh()
        {
            File.WriteAllText(m_Path, "{ this is not json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Stats file could not be read"));

            var stats = NewService();
            stats.Load();

            Assert.That(stats.Data.boards, Is.Empty);
            Assert.That(File.Exists(m_Path + ".corrupt"), Is.True);
        }

        [Test]
        public void InterruptedSave_IsRecoveredFromTheTempFile()
        {
            var stats = NewService();
            stats.RecordRound(Result(750), null);
            stats.Save();
            File.Move(m_Path, m_Path + ".tmp");

            var reloaded = NewService();
            reloaded.Load();

            Assert.That(reloaded.BestScore("MovingGallery/Fast"), Is.EqualTo(750));
        }
    }
}
