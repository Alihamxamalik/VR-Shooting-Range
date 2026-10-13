using RangeControl.Core;
using RangeControl.Range;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>
    /// The motorised trolley that carries the paper target down the lane. It drives to the panel's target
    /// distance as the dial moves, and comes back to the booth on Recall so the group can be read up close.
    /// </summary>
    public sealed class PaperCarrier : MonoBehaviour
    {
        [SerializeField] RangeSettings m_Settings;
        [Tooltip("Moves along local Z. Its local Z is the distance from the firing line in metres.")]
        [SerializeField] Transform m_Trolley;
        [SerializeField] PaperTarget m_Paper;
        [SerializeField] AudioSource m_Motor;
        [SerializeField] float m_RecallDistance = 1.5f;
        [SerializeField] float m_MaxSpeed = 12f;
        [SerializeField] float m_Acceleration = 14f;

        float m_Speed;
        bool m_Recalled;

        public PaperTarget Paper => m_Paper;
        public bool IsRecalled => m_Recalled;
        public bool IsMoving => !Mathf.Approximately(m_Speed, 0f);
        public float Distance => m_Trolley.localPosition.z;

        public void Recall()
        {
            m_Recalled = true;
        }

        public void Send()
        {
            m_Recalled = false;
        }

        public void ToggleRecall()
        {
            m_Recalled = !m_Recalled;
        }

        void Start()
        {
            var local = m_Trolley.localPosition;
            local.z = m_Settings.TargetDistance;
            m_Trolley.localPosition = local;
            ApplySize();
        }

        void OnEnable()
        {
            m_Settings.Changed += ApplySize;
        }

        void OnDisable()
        {
            m_Settings.Changed -= ApplySize;
        }

        void ApplySize()
        {
            m_Paper.transform.localScale = Vector3.one * m_Settings.TargetSize.Scale();
        }

        void Update()
        {
            var goal = m_Recalled ? m_RecallDistance : m_Settings.TargetDistance;
            var local = m_Trolley.localPosition;
            var remaining = goal - local.z;
            var deltaTime = Time.deltaTime;

            if (Mathf.Abs(remaining) < 0.002f && Mathf.Abs(m_Speed) < 0.05f)
            {
                if (!Mathf.Approximately(m_Speed, 0f) || !Mathf.Approximately(local.z, goal))
                {
                    m_Speed = 0f;
                    local.z = goal;
                    m_Trolley.localPosition = local;
                }
            }
            else
            {
                // The fastest speed from which the trolley can still brake to a stop exactly at the goal.
                var cruise = Mathf.Min(m_MaxSpeed, Mathf.Sqrt(2f * m_Acceleration * Mathf.Abs(remaining)));
                m_Speed = Mathf.MoveTowards(m_Speed, Mathf.Sign(remaining) * cruise, m_Acceleration * 2f * deltaTime);
                var step = m_Speed * deltaTime;
                local.z = Mathf.Abs(step) >= Mathf.Abs(remaining) ? goal : local.z + step;
                m_Trolley.localPosition = local;
            }

            UpdateMotorSound();
        }

        void UpdateMotorSound()
        {
            if (m_Motor == null)
                return;
            var amount = Mathf.Clamp01(Mathf.Abs(m_Speed) / m_MaxSpeed);
            if (amount > 0.01f)
            {
                if (!m_Motor.isPlaying)
                    m_Motor.Play();
                m_Motor.volume = Mathf.Lerp(0.15f, 0.6f, amount);
                m_Motor.pitch = Mathf.Lerp(0.7f, 1.3f, amount);
            }
            else if (m_Motor.isPlaying)
            {
                m_Motor.Stop();
            }
        }
    }
}
