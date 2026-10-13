using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>A collider on a target that is worth a fixed number of points, such as the body or the head.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ScoreZone : MonoBehaviour, IHittable
    {
        [SerializeField] FlipTarget m_Target;
        [SerializeField] int m_Points = ScoreRules.SilhouetteBody;

        public int Points => m_Points;

        public void OnHit(in HitInfo hit)
        {
            m_Target.OnZoneHit(this, hit);
        }
    }
}
