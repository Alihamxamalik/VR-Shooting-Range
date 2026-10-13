using UnityEngine;

namespace RangeControl.Weapons
{
    /// <summary>A beam from the muzzle to whatever it points at, for learning to aim. Toggled from the control panel.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LaserSight : MonoBehaviour
    {
        [SerializeField] LayerMask m_Mask = ~0;
        [SerializeField] float m_MaxLength = 100f;

        LineRenderer m_Line;
        bool m_Visible;

        void Awake()
        {
            m_Line = GetComponent<LineRenderer>();
            m_Line.useWorldSpace = true;
            m_Line.positionCount = 2;
            m_Line.enabled = false;
        }

        public void Show(bool visible)
        {
            if (m_Visible == visible)
                return;
            m_Visible = visible;
            m_Line.enabled = visible;
        }

        void LateUpdate()
        {
            if (!m_Visible)
                return;
            var origin = transform.position;
            var direction = transform.forward;
            var length = Physics.Raycast(origin, direction, out var hit, m_MaxLength, m_Mask, QueryTriggerInteraction.Ignore)
                ? hit.distance
                : m_MaxLength;
            m_Line.SetPosition(0, origin);
            m_Line.SetPosition(1, origin + direction * length);
        }
    }
}
