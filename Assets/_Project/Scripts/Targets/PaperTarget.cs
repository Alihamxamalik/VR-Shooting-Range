using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Targets
{
    /// <summary>
    /// A ten-ring paper bullseye. The ring is worked out from where the bullet lands rather than from ten
    /// colliders, and every hole stays on the paper so the group can be inspected when the target is recalled.
    /// This object's origin is the bullseye, with X to the shooter's right and Y up in the plane of the paper.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PaperTarget : Target, IHittable
    {
        public const int MaxHoles = 64;

        [Tooltip("The visible sheet. Holes are parented to it and it flutters when hit.")]
        [SerializeField] Transform m_Sheet;
        [Tooltip("Width of one scoring ring in metres at scale 1. The rings reach out to ten times this.")]
        [SerializeField] float m_RingWidth = 0.025f;
        [SerializeField] Transform m_HolePrefab;
        [Tooltip("Metres a hole sits in front of the paper so it does not z-fight.")]
        [SerializeField] float m_HoleOffset = 0.002f;
        [SerializeField] float m_FlutterDegrees = 4f;

        readonly Vector2[] m_Holes = new Vector2[MaxHoles];
        readonly Transform[] m_HoleVisuals = new Transform[MaxHoles];
        int m_HoleCount;
        int m_NextHole;
        float m_Flutter;

        public int HoleCount => m_HoleCount;
        public float RingWidth => m_RingWidth;

        /// <summary>Widest distance between any two holes, in real-world centimetres.</summary>
        public float GroupSizeCm => GroupSize.Compute(m_Holes, m_HoleCount) * transform.lossyScale.x * 100f;

        void Awake()
        {
            // All hole markers are made up front so that shooting never instantiates anything.
            if (m_HolePrefab == null)
                return;
            for (var i = 0; i < MaxHoles; i++)
            {
                m_HoleVisuals[i] = Instantiate(m_HolePrefab, m_Sheet);
                m_HoleVisuals[i].gameObject.SetActive(false);
            }
        }

        /// <summary>Hangs a fresh sheet: removes every hole.</summary>
        public void Clear()
        {
            foreach (var visual in m_HoleVisuals)
            {
                if (visual != null)
                    visual.gameObject.SetActive(false);
            }

            m_HoleCount = 0;
            m_NextHole = 0;
            if (m_Events != null)
                m_Events.RaisePaperCleared();
        }

        public void OnHit(in HitInfo hit)
        {
            var local = transform.InverseTransformPoint(hit.Point);
            var facePoint = new Vector2(local.x, local.y);
            var distanceFromCentre = facePoint.magnitude;
            var ring = ScoreRules.PaperRing(distanceFromCentre, m_RingWidth);

            AddHole(facePoint);
            m_Flutter = 1f;

            var report = CreateHit(hit, ring);
            report.Ring = ring;
            report.InnerX = ring == 10 && ScoreRules.IsInnerX(distanceFromCentre, m_RingWidth);
            report.FacePoint = facePoint;
            report.FaceRadius = m_RingWidth * 10f;
            m_Events.RaiseTargetHit(report);
        }

        void AddHole(Vector2 facePoint)
        {
            // Past the cap the oldest hole is reused, so a long Free Practice session never grows the paper.
            var index = m_NextHole;
            m_NextHole = (m_NextHole + 1) % MaxHoles;
            if (m_HoleCount < MaxHoles)
                m_HoleCount++;

            m_Holes[index] = facePoint;
            var visual = m_HoleVisuals[index];
            if (visual == null)
                return;
            visual.localPosition = new Vector3(facePoint.x, facePoint.y, -m_HoleOffset);
            visual.gameObject.SetActive(true);
        }

        void Update()
        {
            if (m_Flutter <= 0f)
                return;
            m_Flutter = Mathf.Max(0f, m_Flutter - Time.deltaTime * 4f);
            var angle = Mathf.Sin(Time.time * 55f) * m_FlutterDegrees * m_Flutter;
            m_Sheet.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
}
