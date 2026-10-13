using NUnit.Framework;
using RangeControl.Core;

namespace RangeControl.Tests
{
    public class ScoreRulesTests
    {
        [TestCase(0, 1f)]
        [TestCase(1, 1f)]
        [TestCase(2, 1.1f)]
        [TestCase(3, 1.21f)]
        [TestCase(5, 1.4641f)]
        public void Combo_GrowsByTenPercentPerHitInARow(int hitsInARow, float expected)
        {
            Assert.That(ScoreRules.ComboMultiplier(hitsInARow), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(9)]
        [TestCase(20)]
        [TestCase(500)]
        public void Combo_IsCappedAtTwo(int hitsInARow)
        {
            Assert.That(ScoreRules.ComboMultiplier(hitsInARow), Is.EqualTo(2f));
        }

        [TestCase(0f, 10)]
        [TestCase(0.02f, 10)]
        [TestCase(0.03f, 9)]
        [TestCase(0.112f, 6)]
        [TestCase(0.24f, 1)]
        [TestCase(0.26f, 0)]
        [TestCase(1f, 0)]
        public void PaperRing_FallsOneRingPerRingWidth(float distanceFromCentre, int expectedRing)
        {
            Assert.That(ScoreRules.PaperRing(distanceFromCentre, 0.025f), Is.EqualTo(expectedRing));
        }

        [Test]
        public void PaperRing_OnALineTakesTheHigherRing()
        {
            Assert.That(ScoreRules.PaperRing(0.025f, 0.025f), Is.EqualTo(10));
            Assert.That(ScoreRules.PaperRing(0.25f, 0.025f), Is.EqualTo(1));
        }

        [Test]
        public void InnerX_IsTheCentralHalfOfTheTenRing()
        {
            Assert.That(ScoreRules.IsInnerX(0.012f, 0.025f), Is.True);
            Assert.That(ScoreRules.IsInnerX(0.013f, 0.025f), Is.False);
        }

        [TestCase(0f, 100)]
        [TestCase(0.25f, 75)]
        [TestCase(0.5f, 50)]
        [TestCase(1f, 0)]
        [TestCase(1.5f, 0)]
        public void PopUpBonus_FallsLinearlyOverTheExposure(float exposureFraction, int expectedBonus)
        {
            Assert.That(ScoreRules.PopUpBonus(exposureFraction), Is.EqualTo(expectedBonus));
        }

        [Test]
        public void Difficulty_IsOneAtFifteenMetresStandingNormalSize()
        {
            Assert.That(ScoreRules.DifficultyFactor(15f, 0f, TargetSize.Normal), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Difficulty_MultipliesDistanceSpeedAndSize()
        {
            // clamp(30 / 15, 0.5, 3) x (1 + 2 / 4) x 1.3 = 2 x 1.5 x 1.3
            Assert.That(ScoreRules.DifficultyFactor(30f, 2f, TargetSize.Small), Is.EqualTo(3.9f).Within(1e-4f));
        }

        [Test]
        public void Difficulty_ClampsTheDistanceTerm()
        {
            Assert.That(ScoreRules.DifficultyFactor(5f, 0f, TargetSize.Normal), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(ScoreRules.DifficultyFactor(100f, 0f, TargetSize.Normal), Is.EqualTo(3f).Within(1e-5f));
        }

        [Test]
        public void Difficulty_LargeTargetsAreWorthLess()
        {
            Assert.That(ScoreRules.DifficultyFactor(15f, 0f, TargetSize.Large), Is.EqualTo(0.8f).Within(1e-5f));
        }

        [TestCase(0, Medal.None)]
        [TestCase(399, Medal.None)]
        [TestCase(400, Medal.Bronze)]
        [TestCase(699, Medal.Bronze)]
        [TestCase(700, Medal.Silver)]
        [TestCase(900, Medal.Gold)]
        [TestCase(1000, Medal.Gold)]
        [TestCase(1001, Medal.Developer)]
        public void Medal_ThresholdsAreFortySeventyNinetyPercent(int score, Medal expected)
        {
            Assert.That(ScoreRules.MedalFor(score, 1000), Is.EqualTo(expected));
        }

        [Test]
        public void Medal_NoneUntilADeveloperBestIsSet()
        {
            Assert.That(ScoreRules.MedalFor(5000, 0), Is.EqualTo(Medal.None));
        }
    }
}
