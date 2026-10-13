using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>The points table from the design document, as pure functions so every rule has a test.</summary>
    public static class ScoreRules
    {
        public const int SilhouetteBody = 100;
        public const int SilhouetteHead = 150;
        public const int PopUpBase = 100;
        public const int PopUpMaxBonus = 100;
        public const int SteelPlate = 100;
        public const int NoShootPenalty = -250;

        public const float ComboStep = 1.1f;
        public const float ComboCap = 2f;

        public const int BronzePercent = 40;
        public const int SilverPercent = 70;
        public const int GoldPercent = 90;

        /// <summary>
        /// Multiplier for the n-th hit in a row. The first hit is worth x1.0 and each further consecutive
        /// hit multiplies it by 1.1, up to x2.0.
        /// </summary>
        public static float ComboMultiplier(int hitsInARow)
        {
            if (hitsInARow <= 1)
                return 1f;
            return Mathf.Min(ComboCap, Mathf.Pow(ComboStep, hitsInARow - 1));
        }

        /// <summary>
        /// Ring value for a hit on a ten-ring paper target whose rings are all <paramref name="ringWidth"/> wide.
        /// The centre scores 10, the outermost ring 1 and anything beyond it 0. A hit exactly on a line takes
        /// the higher ring, as on a real target.
        /// </summary>
        public static int PaperRing(float distanceFromCentre, float ringWidth)
        {
            if (ringWidth <= 0f)
                return 0;
            return Mathf.Clamp(11 - Mathf.CeilToInt(distanceFromCentre / ringWidth), 0, 10);
        }

        /// <summary>The inner X is the central half of the 10 ring. It still scores 10 and only breaks ties.</summary>
        public static bool IsInnerX(float distanceFromCentre, float ringWidth)
        {
            return distanceFromCentre <= ringWidth * 0.5f;
        }

        /// <summary>
        /// Reaction bonus for a pop-up: the full bonus the instant it is up, falling linearly to zero by the
        /// end of its exposure time.
        /// </summary>
        public static int PopUpBonus(float exposureFraction)
        {
            return Mathf.RoundToInt(PopUpMaxBonus * (1f - Mathf.Clamp01(exposureFraction)));
        }

        /// <summary>
        /// Free Practice multiplier: clamp(d / 15 m, 0.5, 3) x (1 + v / 4 m/s) x s, where s is 1.3 for small
        /// targets, 1.0 for normal and 0.8 for large.
        /// </summary>
        public static float DifficultyFactor(float distanceMetres, float speedMetresPerSecond, TargetSize size)
        {
            var distance = Mathf.Clamp(distanceMetres / 15f, 0.5f, 3f);
            var speed = 1f + Mathf.Max(0f, speedMetresPerSecond) / 4f;
            return distance * speed * SizeFactor(size);
        }

        public static float SizeFactor(TargetSize size)
        {
            switch (size)
            {
                case TargetSize.Small: return 1.3f;
                case TargetSize.Large: return 0.8f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Medal for a score against the developer's own best: Bronze, Silver and Gold at 40%, 70% and 90% of
        /// it, and the Developer medal for beating it outright. No medals until a developer best is set.
        /// </summary>
        public static Medal MedalFor(int score, int developerBest)
        {
            if (developerBest <= 0)
                return Medal.None;
            if (score > developerBest)
                return Medal.Developer;
            // Whole-number arithmetic, so a score exactly on a threshold always earns the medal.
            var scaled = score * 100L;
            if (scaled >= developerBest * (long)GoldPercent)
                return Medal.Gold;
            if (scaled >= developerBest * (long)SilverPercent)
                return Medal.Silver;
            if (scaled >= developerBest * (long)BronzePercent)
                return Medal.Bronze;
            return Medal.None;
        }
    }
}
