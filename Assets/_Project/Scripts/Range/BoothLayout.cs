using System;
using RangeControl.Core;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>
    /// Fits the booth to the player. Left-handed mode swaps the rack and the panel to the other side, seated
    /// mode brings a seated player up to standing height so the bench, rack and panel sit where their hands
    /// are, and snap turn is switched on only for players who ask for it.
    /// </summary>
    public sealed class BoothLayout : MonoBehaviour
    {
        [SerializeField] ComfortSettings m_Comfort;
        [SerializeField] XROrigin m_Origin;
        [Tooltip("Where the player stands: on the floor, facing down the lanes.")]
        [SerializeField] Transform m_StandingSpot;
        [Tooltip("Pieces that swap sides in left-handed mode, such as the rack and the control panel.")]
        [SerializeField] Transform[] m_SidePieces = Array.Empty<Transform>();
        [Tooltip("Metres the rig is raised for a seated player.")]
        [SerializeField] float m_SeatedLift = 0.4f;
        [SerializeField] Behaviour m_SnapTurn;

        Vector3[] m_Positions;
        Quaternion[] m_Rotations;
        float m_FloorHeight;

        void Awake()
        {
            m_Positions = new Vector3[m_SidePieces.Length];
            m_Rotations = new Quaternion[m_SidePieces.Length];
            for (var i = 0; i < m_SidePieces.Length; i++)
            {
                m_Positions[i] = m_SidePieces[i].localPosition;
                m_Rotations[i] = m_SidePieces[i].localRotation;
            }

            if (m_Origin != null)
                m_FloorHeight = m_Origin.transform.position.y;
        }

        void OnEnable()
        {
            m_Comfort.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            m_Comfort.Changed -= Apply;
        }

        /// <summary>Puts the player back on the standing spot, facing down the lanes, wherever they have drifted.</summary>
        public void Recenter()
        {
            if (m_Origin == null || m_StandingSpot == null || m_Origin.Camera == null)
                return;
            m_Origin.MatchOriginUpCameraForward(Vector3.up, m_StandingSpot.forward);
            var eye = m_Origin.Camera.transform.position;
            m_Origin.MoveCameraToWorldLocation(new Vector3(m_StandingSpot.position.x, eye.y, m_StandingSpot.position.z));
        }

        void Apply()
        {
            var mirrored = m_Comfort.LeftHanded;
            for (var i = 0; i < m_SidePieces.Length; i++)
            {
                // Reflect across the booth's centre line: x flips, and so does the turn about the vertical.
                var position = m_Positions[i];
                var rotation = m_Rotations[i];
                m_SidePieces[i].SetLocalPositionAndRotation(
                    mirrored ? new Vector3(-position.x, position.y, position.z) : position,
                    mirrored ? new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w) : rotation);
            }

            if (m_Origin != null)
            {
                var rig = m_Origin.transform.position;
                rig.y = m_FloorHeight + (m_Comfort.Seated ? m_SeatedLift : 0f);
                m_Origin.transform.position = rig;
            }

            if (m_SnapTurn != null)
                m_SnapTurn.enabled = m_Comfort.SnapTurn;
        }
    }
}
