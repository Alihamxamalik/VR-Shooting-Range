using System;
using RangeControl.Core;
using RangeControl.Range;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>Everything a pooled target needs to start one run along a rail.</summary>
    public struct TargetLaunch
    {
        public TargetRail Rail;
        public RangeSettings Settings;
        public TargetType Type;
        public MovementPattern Pattern;
        public uint Seed;
        /// <summary>Called when the target has dropped and can go back to its pool.</summary>
        public Action<FlipTarget> OnFinished;
    }

    /// <summary>
    /// A board on a hinge that rises, moves along a rail by one of the movement patterns and falls flat when
    /// hit or when its time is up. Moving silhouettes, pop-ups and no-shoots are all this component; the
    /// launch decides which.
    /// </summary>
    public sealed class FlipTarget : Target
    {
        enum State
        {
            Idle,
            Rising,
            Active,
            Falling,
        }

        const float k_FlatAngle = 90f;
        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color s_MissColor = new Color(0.9f, 0.1f, 0.1f);

        [SerializeField] Transform m_Hinge;
        [SerializeField] Collider[] m_ZoneColliders = Array.Empty<Collider>();
        [Tooltip("Renderers tinted red when a pop-up drops without being hit.")]
        [SerializeField] Renderer[] m_FlashRenderers = Array.Empty<Renderer>();
        [SerializeField] float m_FlipSpeed = 540f;

        State m_State;
        float m_Angle = k_FlatAngle;
        float m_ActiveTime;
        MotionState m_Motion;
        ITargetMover m_Mover;
        TargetRail m_Rail;
        RangeSettings m_Settings;
        Action<FlipTarget> m_OnFinished;
        MaterialPropertyBlock m_Block;
        bool m_Flashing;

        public bool IsActive => m_State == State.Active;

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
        }

        public void Launch(in TargetLaunch launch)
        {
            m_Rail = launch.Rail;
            m_Settings = launch.Settings;
            m_OnFinished = launch.OnFinished;
            Type = launch.Type;

            m_Mover = TargetMovers.For(launch.Pattern);
            m_Motion = new MotionState
            {
                RailLength = m_Rail.Length,
                Direction = 1f,
                Exposure = launch.Pattern == MovementPattern.PopUp ? m_Settings.PopUpExposure : TargetMovers.StaticLifetime,
                Rng = new Rng(launch.Seed),
            };
            m_Mover.Begin(ref m_Motion);

            transform.localScale = Vector3.one * m_Settings.TargetSize.Scale();
            transform.SetPositionAndRotation(m_Rail.Evaluate(m_Motion.Position), m_Rail.transform.rotation);
            m_Angle = k_FlatAngle;
            m_ActiveTime = 0f;
            m_State = State.Rising;
            SetHittable(false);
            ClearFlash();
            ApplyAngle();
        }

        /// <summary>Drops the target at once without scoring, for when a round stops.</summary>
        public void Abort()
        {
            if (m_State == State.Idle)
                return;
            SetHittable(false);
            Finish();
        }

        public void OnZoneHit(ScoreZone zone, in HitInfo hit)
        {
            if (m_State != State.Active)
                return;

            int points;
            switch (Type)
            {
                case TargetType.NoShoot:
                    points = ScoreRules.NoShootPenalty;
                    break;
                case TargetType.PopUp:
                    points = ScoreRules.PopUpBase + ScoreRules.PopUpBonus(m_ActiveTime / Mathf.Max(0.01f, m_Motion.Exposure));
                    break;
                default:
                    points = zone.Points;
                    break;
            }

            SetHittable(false);
            m_State = State.Falling;
            m_Events.RaiseTargetHit(CreateHit(hit, points));
        }

        void Update()
        {
            if (m_State == State.Idle)
                return;

            var deltaTime = Time.deltaTime;
            switch (m_State)
            {
                case State.Rising:
                    m_Angle = Mathf.MoveTowards(m_Angle, 0f, m_FlipSpeed * deltaTime);
                    if (m_Angle <= 0f)
                    {
                        m_State = State.Active;
                        SetHittable(true);
                    }

                    break;

                case State.Active:
                    m_ActiveTime += deltaTime;
                    if (!m_Mover.Tick(ref m_Motion, m_Settings.TargetSpeed, deltaTime))
                        Expire();
                    break;

                case State.Falling:
                    m_Angle = Mathf.MoveTowards(m_Angle, k_FlatAngle, m_FlipSpeed * deltaTime);
                    if (m_Angle >= k_FlatAngle)
                    {
                        Finish();
                        return;
                    }

                    break;
            }

            // The rail itself slides when the panel distance changes, so the position is refreshed every frame.
            transform.position = m_Rail.Evaluate(m_Motion.Position);
            ApplyAngle();
        }

        void Expire()
        {
            SetHittable(false);
            m_State = State.Falling;
            if (Type == TargetType.PopUp)
                Flash(s_MissColor);
            m_Events.RaiseTargetExpired(new TargetExpiry(Type, transform.position));
        }

        void Finish()
        {
            m_State = State.Idle;
            ClearFlash();
            var onFinished = m_OnFinished;
            m_OnFinished = null;
            onFinished?.Invoke(this);
        }

        void ApplyAngle()
        {
            m_Hinge.localRotation = Quaternion.Euler(m_Angle, 0f, 0f);
        }

        void SetHittable(bool hittable)
        {
            foreach (var zone in m_ZoneColliders)
                zone.enabled = hittable;
        }

        void Flash(Color color)
        {
            m_Flashing = true;
            m_Block.SetColor(s_BaseColor, color);
            foreach (var flashRenderer in m_FlashRenderers)
                flashRenderer.SetPropertyBlock(m_Block);
        }

        void ClearFlash()
        {
            if (!m_Flashing)
                return;
            m_Flashing = false;
            foreach (var flashRenderer in m_FlashRenderers)
                flashRenderer.SetPropertyBlock(null);
        }
    }
}
