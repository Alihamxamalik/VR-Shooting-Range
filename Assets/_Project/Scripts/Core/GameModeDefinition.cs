using System;
using RangeControl.Range;
using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>
    /// The rules of one game mode as data: how a round ends, what it spawns, whether the panel is locked and
    /// the fixed settings and medal thresholds of each difficulty tier.
    /// </summary>
    [CreateAssetMenu(menuName = "Range Control/Game Mode", fileName = "GameMode")]
    public sealed class GameModeDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Tier
        {
            public string name = "Standard";
            [Tooltip("Panel values this tier locks in.")]
            public RangeSettingsData settings = RangeSettingsData.Default;
            [Tooltip("Score multiplier for every hit in this tier.")]
            public float scoreMultiplier = 1f;
            [Tooltip("Your own best playtest score. Bronze, Silver and Gold sit at 40%, 70% and 90% of it. Zero means no medals yet.")]
            public int developerBest;
        }

        [SerializeField] GameMode m_Mode;
        [SerializeField] string m_DisplayName = "Free Practice";
        [SerializeField, TextArea] string m_Goal;

        [Header("Rules")]
        [Tooltip("Challenge modes fix the panel so scores stay comparable.")]
        [SerializeField] bool m_LocksPanel;
        [SerializeField] bool m_UseCombo = true;
        [Tooltip("Multiply score by the live difficulty factor from the panel (Free Practice).")]
        [SerializeField] bool m_UseDifficultyFactor;
        [Tooltip("Round length in seconds. Zero uses the panel's round timer.")]
        [SerializeField] float m_Duration;
        [Tooltip("The round ends after this many shots. Zero means no limit.")]
        [SerializeField] int m_ShotLimit;

        [Header("Targets")]
        [SerializeField] bool m_SpawnsTargets = true;
        [SerializeField, Range(0f, 1f)] float m_NoShootChance;
        [Tooltip("Hangs a fresh paper target and sends the carrier out when the round starts.")]
        [SerializeField] bool m_UsesPaper;

        [SerializeField] Tier[] m_Tiers = Array.Empty<Tier>();

        public GameMode Mode => m_Mode;
        public string DisplayName => m_DisplayName;
        public string Goal => m_Goal;
        public bool LocksPanel => m_LocksPanel;
        public bool UseCombo => m_UseCombo;
        public bool UseDifficultyFactor => m_UseDifficultyFactor;
        public float Duration => m_Duration;
        public int ShotLimit => m_ShotLimit;
        public bool SpawnsTargets => m_SpawnsTargets;
        public float NoShootChance => m_NoShootChance;
        public bool UsesPaper => m_UsesPaper;
        public int TierCount => m_Tiers.Length;

        /// <summary>The tier at <paramref name="index"/>, or null for a mode without tiers.</summary>
        public Tier GetTier(int index)
        {
            return m_Tiers.Length == 0 ? null : m_Tiers[Mathf.Clamp(index, 0, m_Tiers.Length - 1)];
        }

        /// <summary>Leaderboard key for a tier, for example "MovingGallery/Fast".</summary>
        public string BoardKey(int tierIndex)
        {
            var tier = GetTier(tierIndex);
            return tier == null ? m_Mode.ToString() : m_Mode + "/" + tier.name;
        }
    }
}
