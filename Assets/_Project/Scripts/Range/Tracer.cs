using System;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>
    /// A short streak that flies from the muzzle to where the bullet landed. It is drawn after the shot has
    /// already been resolved and decides nothing: it only sells the shot.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Tracer : MonoBehaviour
    {
        [SerializeField] float m_Speed = 350f;
        [SerializeField] float m_Length = 3f;

        LineRenderer m_Line;
        Action<Tracer> m_OnFinished;
        Vector3 m_Origin;
        Vector3 m_Direction;
        float m_Distance;
        float m_Travelled;

        void Awake()
        {
            m_Line = GetComponent<LineRenderer>();
            m_Line.useWorldSpace = true;
            m_Line.positionCount = 2;
        }

        public void Fire(Vector3 origin, Vector3 end, Action<Tracer> onFinished)
        {
            m_Origin = origin;
            m_OnFinished = onFinished;
            var path = end - origin;
            m_Distance = path.magnitude;
            m_Direction = m_Distance > 0f ? path / m_Distance : Vector3.forward;
            // Start a little way out so the streak does not begin inside the barrel.
            m_Travelled = Mathf.Min(m_Distance, m_Length * 0.5f);
            Draw();
        }

        void Update()
        {
            m_Travelled += m_Speed * Time.deltaTime;
            if (m_Travelled - m_Length >= m_Distance)
            {
                var onFinished = m_OnFinished;
                m_OnFinished = null;
                onFinished?.Invoke(this);
                return;
            }

            Draw();
        }

        void Draw()
        {
            var head = Mathf.Min(m_Travelled, m_Distance);
            var tail = Mathf.Clamp(m_Travelled - m_Length, 0f, m_Distance);
            m_Line.SetPosition(0, m_Origin + m_Direction * tail);
            m_Line.SetPosition(1, m_Origin + m_Direction * head);
        }
    }
}
