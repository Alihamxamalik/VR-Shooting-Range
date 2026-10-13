using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>A hanging steel plate that swings back when hit and settles again after a moment.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SteelPlate : Target, IHittable
    {
        enum State
        {
            Ready,
            Down,
            Settling,
        }

        [SerializeField] Transform m_Hinge;
        [SerializeField] float m_SwingAngle = 55f;
        [SerializeField] float m_ResetDelay = 1.5f;
        [SerializeField] float m_SwingSpeed = 700f;
        [SerializeField] float m_ReturnSpeed = 160f;
        [SerializeField] float m_WobbleDegrees = 9f;
        [SerializeField] float m_WobbleSeconds = 0.7f;

        State m_State;
        float m_Angle;
        float m_Timer;

        public bool IsReady => m_State == State.Ready;

        public void OnHit(in HitInfo hit)
        {
            // A plate that is still swinging rings and sparks through the impact effects but does not score twice.
            if (m_State == State.Down)
                return;

            m_State = State.Down;
            m_Timer = m_ResetDelay;
            m_Events.RaiseTargetHit(CreateHit(hit, ScoreRules.SteelPlate));
        }

        void Update()
        {
            switch (m_State)
            {
                case State.Down:
                    m_Angle = Mathf.MoveTowards(m_Angle, m_SwingAngle, m_SwingSpeed * Time.deltaTime);
                    m_Timer -= Time.deltaTime;
                    if (m_Timer <= 0f)
                    {
                        m_State = State.Settling;
                        m_Timer = 0f;
                    }

                    break;

                case State.Settling:
                    if (m_Angle > 0f)
                    {
                        m_Angle = Mathf.MoveTowards(m_Angle, 0f, m_ReturnSpeed * Time.deltaTime);
                    }
                    else
                    {
                        m_Timer += Time.deltaTime;
                        if (m_Timer >= m_WobbleSeconds)
                        {
                            m_State = State.Ready;
                            m_Hinge.localRotation = Quaternion.identity;
                            return;
                        }
                    }

                    break;

                default:
                    return;
            }

            var wobble = 0f;
            if (m_State == State.Settling && m_Angle <= 0f)
            {
                var fade = 1f - m_Timer / m_WobbleSeconds;
                wobble = Mathf.Sin(m_Timer * 28f) * m_WobbleDegrees * fade * fade;
            }

            // A negative turn about X carries the hanging plate back, away from the shooter.
            m_Hinge.localRotation = Quaternion.Euler(-(m_Angle + wobble), 0f, 0f);
        }
    }
}
