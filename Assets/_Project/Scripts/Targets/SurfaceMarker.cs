using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>Tells the impact effects what a collider is made of. A collider without one is treated as dirt.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SurfaceMarker : MonoBehaviour
    {
        [SerializeField] SurfaceType m_Surface = SurfaceType.Dirt;
        [Tooltip("Leave a bullet hole here. Off for anything that moves or keeps its own holes.")]
        [SerializeField] bool m_LeavesDecal = true;

        public SurfaceType Surface => m_Surface;
        public bool LeavesDecal => m_LeavesDecal;
    }
}
