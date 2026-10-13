using System;
using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>One full set of control panel values. Game mode tiers and preset slots store one of these.</summary>
    [Serializable]
    public struct RangeSettingsData
    {
        [Tooltip("Metres from the firing line to where targets stand or travel.")]
        public float targetDistance;
        [Tooltip("Metres per second along the rail.")]
        public float targetSpeed;
        public MovementPattern pattern;
        [Tooltip("Seconds between targets.")]
        public float spawnInterval;
        [Tooltip("Seconds a pop-up target stays up.")]
        public float popUpExposure;
        [Tooltip("Targets in a round. Zero means endless.")]
        public int targetsPerRound;
        public TargetSize targetSize;
        [Tooltip("Percent of the gun's base rate of fire, 50 to 150.")]
        public int fireRatePercent;
        public bool infiniteAmmo;
        public bool laserSight;
        [Tooltip("Round length in seconds: 0 (off), 30, 60 or 90.")]
        public int roundTimer;

        public static RangeSettingsData Default => new RangeSettingsData
        {
            targetDistance = 15f,
            targetSpeed = 1.5f,
            pattern = MovementPattern.LeftToRight,
            spawnInterval = 2f,
            popUpExposure = 1.5f,
            targetsPerRound = 20,
            targetSize = TargetSize.Normal,
            fireRatePercent = 100,
            infiniteAmmo = false,
            laserSight = false,
            roundTimer = 0,
        };
    }

    /// <summary>
    /// The single source of truth for everything the control panel adjusts. The panel writes to it; the
    /// spawner, the paper carrier, the guns and the scoring read from it and react to <see cref="Changed"/>.
    /// The asset stores the defaults and the live values stay in memory, so play mode never edits the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Range Control/Range Settings", fileName = "RangeSettings")]
    public sealed class RangeSettings : ScriptableObject
    {
        public const float MinDistance = 5f;
        public const float MaxDistance = 50f;
        public const float LongRangeMaxDistance = 100f;
        public const float DistanceStep = 5f;

        public const float MinSpeed = 0f;
        public const float MaxSpeed = 6f;
        public const float SpeedStep = 0.5f;
        public const float SlowSpeed = 1f;
        public const float MediumSpeed = 2.5f;
        public const float FastSpeed = 4f;

        public const float MinSpawnInterval = 0.5f;
        public const float MaxSpawnInterval = 5f;
        public const float SpawnIntervalStep = 0.5f;

        public const float MinPopUpExposure = 0.5f;
        public const float MaxPopUpExposure = 3f;
        public const float PopUpExposureStep = 0.25f;

        public const int MinTargetsPerRound = 5;
        public const int MaxTargetsPerRound = 50;
        public const int TargetsPerRoundStep = 5;
        public const int Endless = 0;

        public const int MinFireRatePercent = 50;
        public const int MaxFireRatePercent = 150;
        public const int FireRateStep = 10;

        public static readonly int[] RoundTimerOptions = { 0, 30, 60, 90 };

        [SerializeField] RangeSettingsData m_Defaults = RangeSettingsData.Default;

        [NonSerialized] RangeSettingsData m_Current;
        [NonSerialized] bool m_Initialised;
        [NonSerialized] bool m_Locked;
        [NonSerialized] bool m_LongRange;

        /// <summary>Raised after any value changes, including the lock and the distance limit.</summary>
        public event Action Changed;

        public RangeSettingsData Current
        {
            get
            {
                EnsureInitialised();
                return m_Current;
            }
        }

        /// <summary>
        /// True in challenge modes, where the panel is fixed so scores stay comparable. Player-facing setters
        /// do nothing while locked; <see cref="Apply"/> still works for the round controller.
        /// </summary>
        public bool Locked
        {
            get => m_Locked;
            set
            {
                if (m_Locked == value)
                    return;
                m_Locked = value;
                Changed?.Invoke();
            }
        }

        /// <summary>True while a scoped rifle is in hand, which opens the range out to 100 m.</summary>
        public bool LongRange
        {
            get => m_LongRange;
            set
            {
                EnsureInitialised();
                if (m_LongRange == value)
                    return;
                m_LongRange = value;
                if (!m_Locked)
                    m_Current.targetDistance = Mathf.Min(m_Current.targetDistance, DistanceLimit);
                Changed?.Invoke();
            }
        }

        public float DistanceLimit => m_LongRange ? LongRangeMaxDistance : MaxDistance;

        public float TargetDistance
        {
            get => Current.targetDistance;
            set => Set(ref m_Current.targetDistance, Mathf.Clamp(Snap(value, DistanceStep), MinDistance, DistanceLimit));
        }

        public float TargetSpeed
        {
            get => Current.targetSpeed;
            set => Set(ref m_Current.targetSpeed, Mathf.Clamp(Snap(value, SpeedStep), MinSpeed, MaxSpeed));
        }

        public MovementPattern Pattern
        {
            get => Current.pattern;
            set
            {
                if (CanEdit() && m_Current.pattern != value)
                {
                    m_Current.pattern = value;
                    Changed?.Invoke();
                }
            }
        }

        public float SpawnInterval
        {
            get => Current.spawnInterval;
            set => Set(ref m_Current.spawnInterval, Mathf.Clamp(Snap(value, SpawnIntervalStep), MinSpawnInterval, MaxSpawnInterval));
        }

        public float PopUpExposure
        {
            get => Current.popUpExposure;
            set => Set(ref m_Current.popUpExposure, Mathf.Clamp(Snap(value, PopUpExposureStep), MinPopUpExposure, MaxPopUpExposure));
        }

        /// <summary>Targets in a round, or <see cref="Endless"/>.</summary>
        public int TargetsPerRound
        {
            get => Current.targetsPerRound;
            set
            {
                var clamped = value <= Endless ? Endless : Mathf.Clamp(value, MinTargetsPerRound, MaxTargetsPerRound);
                if (CanEdit() && m_Current.targetsPerRound != clamped)
                {
                    m_Current.targetsPerRound = clamped;
                    Changed?.Invoke();
                }
            }
        }

        public TargetSize TargetSize
        {
            get => Current.targetSize;
            set
            {
                if (CanEdit() && m_Current.targetSize != value)
                {
                    m_Current.targetSize = value;
                    Changed?.Invoke();
                }
            }
        }

        public int FireRatePercent
        {
            get => Current.fireRatePercent;
            set
            {
                var clamped = Mathf.Clamp(value, MinFireRatePercent, MaxFireRatePercent);
                if (CanEdit() && m_Current.fireRatePercent != clamped)
                {
                    m_Current.fireRatePercent = clamped;
                    Changed?.Invoke();
                }
            }
        }

        public bool InfiniteAmmo
        {
            get => Current.infiniteAmmo;
            set
            {
                if (CanEdit() && m_Current.infiniteAmmo != value)
                {
                    m_Current.infiniteAmmo = value;
                    Changed?.Invoke();
                }
            }
        }

        public bool LaserSight
        {
            get => Current.laserSight;
            set
            {
                if (CanEdit() && m_Current.laserSight != value)
                {
                    m_Current.laserSight = value;
                    Changed?.Invoke();
                }
            }
        }

        /// <summary>Round length in seconds; zero means no timer.</summary>
        public int RoundTimer
        {
            get => Current.roundTimer;
            set
            {
                var clamped = Mathf.Max(0, value);
                if (CanEdit() && m_Current.roundTimer != clamped)
                {
                    m_Current.roundTimer = clamped;
                    Changed?.Invoke();
                }
            }
        }

        /// <summary>The live Free Practice score multiplier for the current distance, speed and size.</summary>
        public float DifficultyFactor => ScoreRules.DifficultyFactor(Current.targetDistance, Current.targetSpeed, Current.targetSize);

        /// <summary>Steps the targets-per-round value through 5, 10 ... 50, Endless.</summary>
        public void StepTargetsPerRound(int direction)
        {
            var current = TargetsPerRound;
            if (direction > 0)
                TargetsPerRound = current == Endless ? Endless : (current >= MaxTargetsPerRound ? Endless : current + TargetsPerRoundStep);
            else if (direction < 0)
                TargetsPerRound = current == Endless ? MaxTargetsPerRound : Mathf.Max(MinTargetsPerRound, current - TargetsPerRoundStep);
        }

        /// <summary>Steps the round timer through Off, 30, 60, 90 seconds without wrapping.</summary>
        public void StepRoundTimer(int direction)
        {
            var index = Array.IndexOf(RoundTimerOptions, RoundTimer);
            if (index < 0)
                index = 0;
            RoundTimer = RoundTimerOptions[Mathf.Clamp(index + Math.Sign(direction), 0, RoundTimerOptions.Length - 1)];
        }

        /// <summary>Replaces every value at once, ignoring the lock. Used by mode tiers and preset slots.</summary>
        public void Apply(in RangeSettingsData data)
        {
            m_Current = data;
            m_Initialised = true;
            Changed?.Invoke();
        }

        public void ResetToDefaults()
        {
            m_Current = m_Defaults;
            m_Initialised = true;
            m_Locked = false;
            m_LongRange = false;
            Changed?.Invoke();
        }

        void EnsureInitialised()
        {
            if (m_Initialised)
                return;
            m_Current = m_Defaults;
            m_Initialised = true;
        }

        bool CanEdit()
        {
            EnsureInitialised();
            return !m_Locked;
        }

        void Set(ref float field, float value)
        {
            if (!CanEdit() || Mathf.Approximately(field, value))
                return;
            field = value;
            Changed?.Invoke();
        }

        static float Snap(float value, float step)
        {
            return Mathf.Round(value / step) * step;
        }
    }
}
