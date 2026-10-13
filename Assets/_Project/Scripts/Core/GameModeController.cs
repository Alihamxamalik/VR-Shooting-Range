using System;
using RangeControl.Range;
using RangeControl.Targets;
using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>
    /// Runs a round: Idle, Countdown, Running, Results. It owns the score and the saved stats, starts and
    /// stops the target spawner, decides when a round is over and announces every change on the event
    /// channel. Nothing else in the project knows what state the round is in except by listening.
    /// </summary>
    public sealed class GameModeController : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [SerializeField] RangeSettings m_Settings;
        [SerializeField] GameModeDefinition[] m_Modes = Array.Empty<GameModeDefinition>();
        [SerializeField] TargetSpawner m_Spawner;
        [SerializeField] PaperCarrier m_Carrier;
        [SerializeField] int m_CountdownSeconds = 3;

        readonly ScoreService m_Score = new ScoreService();
        StatsService m_Stats;
        RangeSettingsData m_PracticeSettings;
        bool m_HasPracticeSettings;
        RoundState m_State = RoundState.Idle;
        RoundResult m_LastResult;
        string m_LastWeaponId;
        int m_ModeIndex;
        int m_TierIndex;
        int m_LastTick;
        int m_ShotLimit;
        float m_Countdown;
        float m_Elapsed;
        float m_Duration;

        public RoundState State => m_State;
        public int ModeCount => m_Modes.Length;
        public int ModeIndex => m_ModeIndex;
        public int TierIndex => m_TierIndex;
        public GameModeDefinition Mode => m_Modes[m_ModeIndex];
        /// <summary>The selected tier, or null in a mode without tiers.</summary>
        public GameModeDefinition.Tier Tier => Mode.GetTier(m_TierIndex);
        public string BoardKey => Mode.BoardKey(m_TierIndex);
        public ScoreService Score => m_Score;
        public StatsService Stats => m_Stats;
        public PaperCarrier Carrier => m_Carrier;
        /// <summary>The result of the round that just ended, or null before the first one.</summary>
        public RoundResult LastResult => m_LastResult;
        public int BestScore => m_Stats.BestScore(BoardKey);
        public float Elapsed => m_Elapsed;

        /// <summary>Seconds left in a timed round, or -1 when the round has no timer.</summary>
        public float TimeRemaining => m_Duration > 0f ? Mathf.Max(0f, m_Duration - m_Elapsed) : -1f;

        /// <summary>Shots left in a shot-limited round, or -1 when there is no limit.</summary>
        public int ShotsRemaining => m_ShotLimit > 0 ? Mathf.Max(0, m_ShotLimit - m_Score.Shots) : -1;

        /// <summary>The multiplier a hit earns right now, before combo.</summary>
        public float Multiplier
        {
            get
            {
                if (Mode.UseDifficultyFactor)
                    return m_Settings.DifficultyFactor;
                var tier = Tier;
                return tier != null ? tier.scoreMultiplier : 1f;
            }
        }

        void Awake()
        {
            m_Stats ??= new StatsService(StatsService.DefaultPath);
            m_Stats.Load();
            m_Settings.ResetToDefaults();
            ApplyModeSettings();
        }

        void OnEnable()
        {
            m_Events.ShotFired += OnShotFired;
            m_Events.TargetHit += OnTargetHit;
            m_Events.TargetExpired += OnTargetExpired;
            m_Settings.Changed += OnSettingsChanged;
        }

        void OnDisable()
        {
            m_Events.ShotFired -= OnShotFired;
            m_Events.TargetHit -= OnTargetHit;
            m_Events.TargetExpired -= OnTargetExpired;
            m_Settings.Changed -= OnSettingsChanged;
        }

        /// <summary>Swaps in a stats service, so tests can keep the save file out of the real data folder.</summary>
        public void UseStats(StatsService stats)
        {
            m_Stats = stats;
        }

        /// <summary>Picks a mode and tier. Ignored while a round is counting down or running.</summary>
        public void SelectMode(int modeIndex, int tierIndex)
        {
            if (m_State == RoundState.Countdown || m_State == RoundState.Running || m_Modes.Length == 0)
                return;

            // Keep the player's own setup safe while a challenge mode borrows the panel.
            if (!Mode.LocksPanel)
            {
                m_PracticeSettings = m_Settings.Current;
                m_HasPracticeSettings = true;
            }

            m_ModeIndex = (modeIndex % m_Modes.Length + m_Modes.Length) % m_Modes.Length;
            var tierCount = Mathf.Max(1, Mode.TierCount);
            m_TierIndex = (tierIndex % tierCount + tierCount) % tierCount;
            ApplyModeSettings();

            if (m_State == RoundState.Results)
                SetState(RoundState.Idle);
            m_Events.RaiseModeChanged();
        }

        public void StepMode(int direction)
        {
            SelectMode(m_ModeIndex + Math.Sign(direction), 0);
        }

        public void StepTier(int direction)
        {
            SelectMode(m_ModeIndex, m_TierIndex + Math.Sign(direction));
        }

        /// <summary>The Start / Stop lever.</summary>
        public void ToggleRound()
        {
            if (m_State == RoundState.Countdown || m_State == RoundState.Running)
                StopRound();
            else
                StartRound();
        }

        public void StartRound()
        {
            if (m_State == RoundState.Countdown || m_State == RoundState.Running)
                return;

            if (Mode.UsesPaper && m_Carrier != null)
            {
                m_Carrier.Paper.Clear();
                m_Carrier.Send();
            }

            m_Countdown = m_CountdownSeconds;
            m_LastTick = -1;
            SetState(RoundState.Countdown);
        }

        /// <summary>Ends a running round with its results, or cancels a countdown.</summary>
        public void StopRound()
        {
            if (m_State == RoundState.Countdown)
                SetState(RoundState.Idle);
            else if (m_State == RoundState.Running)
                EndRound();
        }

        /// <summary>Hangs a fresh paper target. Not allowed in the middle of a challenge round.</summary>
        public void NewPaper()
        {
            if (m_Carrier == null || (m_State == RoundState.Running && Mode.LocksPanel))
                return;
            m_Carrier.Paper.Clear();
        }

        public void CompleteTutorial()
        {
            if (m_Stats.Data.tutorialCompleted)
                return;
            m_Stats.Data.tutorialCompleted = true;
            m_Stats.Save();
        }

        void Update()
        {
            switch (m_State)
            {
                case RoundState.Countdown:
                    var secondsLeft = Mathf.CeilToInt(m_Countdown);
                    if (secondsLeft != m_LastTick && secondsLeft > 0)
                    {
                        m_LastTick = secondsLeft;
                        m_Events.RaiseCountdownTick(secondsLeft);
                    }

                    m_Countdown -= Time.deltaTime;
                    if (m_Countdown <= 0f)
                        BeginRunning();
                    break;

                case RoundState.Running:
                    m_Elapsed += Time.deltaTime;
                    if ((m_Duration > 0f && m_Elapsed >= m_Duration) || (m_Spawner != null && m_Spawner.IsExhausted))
                        EndRound();
                    break;
            }
        }

        void ApplyModeSettings()
        {
            if (m_Modes.Length == 0)
                return;

            var tier = Tier;
            if (Mode.LocksPanel && tier != null)
            {
                m_Settings.Apply(tier.settings);
                m_Settings.Locked = true;
            }
            else
            {
                m_Settings.Locked = false;
                if (m_HasPracticeSettings)
                    m_Settings.Apply(m_PracticeSettings);
            }
        }

        void BeginRunning()
        {
            var mode = Mode;
            m_Duration = mode.Duration > 0f ? mode.Duration : m_Settings.RoundTimer;
            m_ShotLimit = mode.ShotLimit;
            m_Elapsed = 0f;
            m_Score.Begin(mode.UseCombo, Multiplier);
            if (mode.SpawnsTargets && m_Spawner != null)
                m_Spawner.Begin(m_Settings.TargetsPerRound, mode.NoShootChance);
            SetState(RoundState.Running);
        }

        void EndRound()
        {
            if (m_State != RoundState.Running)
                return;
            if (m_Spawner != null)
                m_Spawner.Stop();

            var mode = Mode;
            var tier = Tier;
            var result = new RoundResult
            {
                mode = mode.Mode,
                modeName = mode.DisplayName,
                tierName = tier != null ? tier.name : string.Empty,
                boardKey = BoardKey,
                score = m_Score.Score,
                shots = m_Score.Shots,
                hits = m_Score.Hits,
                accuracy = m_Score.Accuracy,
                bestStreak = m_Score.BestStreak,
                targetsHit = m_Score.TargetsHit,
                targetsMissed = m_Score.TargetsMissed,
                penalties = m_Score.Penalties,
                innerX = m_Score.InnerXCount,
                longestHit = m_Score.LongestHit,
                duration = m_Elapsed,
                weaponId = m_LastWeaponId,
                groupSizeCm = mode.UsesPaper && m_Carrier != null ? m_Carrier.Paper.GroupSizeCm : 0f,
            };
            result.hasMedals = tier != null && tier.developerBest > 0;
            result.medal = ScoreRules.MedalFor(result.score, tier != null ? tier.developerBest : 0);

            m_Stats.RecordRound(result, m_Score.Weapons);
            m_Stats.Save();
            m_LastResult = result;
            SetState(RoundState.Results);
            m_Events.RaiseRoundEnded(result);
        }

        void SetState(RoundState state)
        {
            m_State = state;
            m_Events.RaiseRoundStateChanged(state);
        }

        void OnShotFired(ShotInfo shot)
        {
            if (shot.Weapon != null)
                m_LastWeaponId = shot.Weapon.Id;
            if (m_State != RoundState.Running)
                return;

            var hadCombo = m_Score.Streak > 0;
            if (!m_Score.RegisterShot(shot) && hadCombo)
                m_Events.RaiseComboBroken();
            if (m_ShotLimit > 0 && m_Score.Shots >= m_ShotLimit)
                EndRound();
        }

        void OnTargetHit(TargetHit hit)
        {
            if (m_State != RoundState.Running)
            {
                // Outside a round a hit still shows what it is worth; it just is not added to anything.
                m_Events.RaiseScoreAwarded(new ScoreAward(hit.BasePoints, 1f, 0, hit.Point, hit.Distance, false));
                return;
            }

            var hadCombo = m_Score.Streak > 0;
            var points = m_Score.RegisterHit(hit);
            if (hit.BasePoints < 0 && hadCombo)
                m_Events.RaiseComboBroken();
            m_Events.RaiseScoreAwarded(new ScoreAward(points, m_Score.Combo, m_Score.Score, hit.Point, hit.Distance, true));
        }

        void OnTargetExpired(TargetExpiry expiry)
        {
            if (m_State == RoundState.Running)
                m_Score.RegisterExpiry(expiry);
        }

        void OnSettingsChanged()
        {
            if (m_State == RoundState.Running && Mode.UseDifficultyFactor)
                m_Score.Multiplier = m_Settings.DifficultyFactor;
        }
    }
}
