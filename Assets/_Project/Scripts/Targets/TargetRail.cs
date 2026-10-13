using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>A straight rail across the lane that targets stand on or travel along, from its start to its end.</summary>
    public sealed class TargetRail : MonoBehaviour
    {
        [SerializeField] Transform m_Start;
        [SerializeField] Transform m_End;
        [Tooltip("Metres behind the panel's target distance that this rail sits.")]
        [SerializeField] float m_DepthOffset;

        public float DepthOffset => m_DepthOffset;
        public float Length => Vector3.Distance(m_Start.position, m_End.position);

        /// <summary>World position at <paramref name="metres"/> along the rail from its start.</summary>
        public Vector3 Evaluate(float metres)
        {
            var start = m_Start.position;
            var end = m_End.position;
            var length = Vector3.Distance(start, end);
            return length <= 0f ? start : Vector3.Lerp(start, end, metres / length);
        }
    }
}
