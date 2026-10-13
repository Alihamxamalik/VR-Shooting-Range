using System.Collections.Generic;
using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>
    /// Keeps the score for one round. Plain C# with no scene dependencies: the round controller feeds it
    /// hits and shots from the event channel, and the edit-mode tests feed it the same calls directly.
    /// </summary>
    public sealed class ScoreService
    {
        public struct WeaponTally
        {
            public int Shots;
            public int Hits;
            public float LongestHit;
        }

        readonly Dictionary<string, WeaponTally> m_Weapons = new Dictionary<string, WeaponTally>();
        int m_LastScoringShotId = -1;
        int m_StreakShotId = -1;

        /// <summary>Precision turns the combo off so a ring total stays an honest 0-100.</summary>
        public bool UseCombo { get; set; } = true;

        /// <summary>Mode multiplier: the speed tier in Moving Gallery, the live difficulty factor in Free Practice.</summary>
        public float Multiplier { get; set; } = 1f;

        public int Score { get; private set; }
        /// <summary>Consecutive shots that scored.</summary>
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int Shots { get; private set; }
        /// <summary>Shots that scored. A shotgun blast counts once however many pellets land.</summary>
        public int Hits { get; private set; }
        public int TargetsHit { get; private set; }
        public int TargetsMissed { get; private set; }
        public int Penalties { get; private set; }
        public int InnerXCount { get; private set; }
        public float LongestHit { get; private set; }

        public float Accuracy => Shots > 0 ? (float)Hits / Shots : 0f;
        public float Combo => UseCombo ? ScoreRules.ComboMultiplier(Streak) : 1f;
        public IReadOnlyDictionary<string, WeaponTally> Weapons => m_Weapons;

        public void Begin(bool useCombo, float multiplier)
        {
            UseCombo = useCombo;
            Multiplier = multiplier;
            Score = 0;
            Streak = 0;
            BestStreak = 0;
            Shots = 0;
            Hits = 0;
            TargetsHit = 0;
            TargetsMissed = 0;
            Penalties = 0;
            InnerXCount = 0;
            LongestHit = 0f;
            m_LastScoringShotId = -1;
            m_StreakShotId = -1;
            m_Weapons.Clear();
        }

        /// <summary>
        /// Scores one bullet landing on a target and returns the points awarded, which are negative for a
        /// no-shoot. Call this before <see cref="RegisterShot"/> for the same shot.
        /// </summary>
        public int RegisterHit(in TargetHit hit)
        {
            if (hit.BasePoints < 0)
            {
                Penalties++;
                Score += hit.BasePoints;
                Streak = 0;
                return hit.BasePoints;
            }

            if (hit.BasePoints == 0)
                return 0;

            // The streak counts trigger pulls, so the pellets of one blast share a single step of combo.
            if (hit.ShotId != m_StreakShotId)
            {
                m_StreakShotId = hit.ShotId;
                Streak++;
                if (Streak > BestStreak)
                    BestStreak = Streak;
            }

            m_LastScoringShotId = hit.ShotId;
            var points = Mathf.RoundToInt(hit.BasePoints * Combo * Multiplier);
            Score += points;
            TargetsHit++;
            if (hit.InnerX)
                InnerXCount++;
            if (hit.Distance > LongestHit)
                LongestHit = hit.Distance;

            if (hit.Weapon != null)
            {
                m_Weapons.TryGetValue(hit.Weapon.Id, out var tally);
                if (hit.Distance > tally.LongestHit)
                    tally.LongestHit = hit.Distance;
                m_Weapons[hit.Weapon.Id] = tally;
            }

            return points;
        }

        /// <summary>
        /// Counts one trigger pull once all of its hits are registered. Returns true if it scored; a shot
        /// that did not score is a miss and resets the combo.
        /// </summary>
        public bool RegisterShot(in ShotInfo shot)
        {
            Shots++;
            var scored = shot.ShotId == m_LastScoringShotId;
            if (scored)
                Hits++;
            else
                Streak = 0;

            if (shot.Weapon != null)
            {
                m_Weapons.TryGetValue(shot.Weapon.Id, out var tally);
                tally.Shots++;
                if (scored)
                    tally.Hits++;
                m_Weapons[shot.Weapon.Id] = tally;
            }

            return scored;
        }

        public void RegisterExpiry(in TargetExpiry expiry)
        {
            if (expiry.Type != TargetType.NoShoot)
                TargetsMissed++;
        }
    }
}
