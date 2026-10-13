using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>Common ground for everything on the range that scores: what it is, what it is made of and where it reports.</summary>
    public abstract class Target : MonoBehaviour
    {
        [SerializeField] protected RangeEvents m_Events;
        [SerializeField] TargetType m_Type;
        [SerializeField] SurfaceType m_Surface = SurfaceType.Wood;

        public TargetType Type
        {
            get => m_Type;
            protected set => m_Type = value;
        }

        public SurfaceType Surface => m_Surface;

        /// <summary>Starts a hit report with the fields every target fills in the same way.</summary>
        protected TargetHit CreateHit(in HitInfo hit, int basePoints)
        {
            return new TargetHit
            {
                ShotId = hit.ShotId,
                Type = m_Type,
                Surface = m_Surface,
                BasePoints = basePoints,
                Ring = -1,
                Point = hit.Point,
                Normal = hit.Normal,
                Distance = hit.Distance,
                Scale = transform.lossyScale.x,
                Weapon = hit.Weapon,
            };
        }
    }
}
