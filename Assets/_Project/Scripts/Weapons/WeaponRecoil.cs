using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Weapons
{
    /// <summary>
    /// Procedural kick and muzzle climb. Each shot adds an offset to the pivot that holds the model and the
    /// muzzle, and the offset then decays back to rest. Only the gun moves: the camera is never touched.
    /// </summary>
    public sealed class WeaponRecoil : MonoBehaviour
    {
        const float k_MaxClimb = 30f;
        const float k_MaxKickBack = 0.15f;

        [Tooltip("Parent of the model and the muzzle, placed at the grip so the gun rotates in the hand.")]
        [SerializeField] Transform m_Pivot;
        [SerializeField] uint m_Seed = 7;

        Vector3 m_BasePosition;
        Quaternion m_BaseRotation;
        Vector3 m_PositionOffset;
        Vector3 m_RotationOffset;
        float m_ReturnSpeed = 8f;
        Rng m_Rng;

        public Vector3 RotationOffset => m_RotationOffset;
        public Vector3 PositionOffset => m_PositionOffset;

        void Awake()
        {
            m_Rng = new Rng(m_Seed);
            m_BasePosition = m_Pivot.localPosition;
            m_BaseRotation = m_Pivot.localRotation;
        }

        /// <summary>Adds one shot's worth of recoil. <paramref name="scale"/> is below one with a second hand on the gun.</summary>
        public void Kick(WeaponData weapon, float scale)
        {
            m_ReturnSpeed = weapon.RecoilReturnSpeed;
            m_PositionOffset.z = Mathf.Max(-k_MaxKickBack, m_PositionOffset.z - weapon.KickBack * scale);
            m_RotationOffset.x = Mathf.Max(-k_MaxClimb, m_RotationOffset.x - weapon.MuzzleClimb * scale);
            m_RotationOffset.y += m_Rng.Range(-weapon.YawJitter, weapon.YawJitter) * scale;
            Apply();
        }

        /// <summary>Lets the gun settle. Exponential decay, so the feel is the same at 72, 90 and 120 Hz.</summary>
        public void Tick(float deltaTime)
        {
            if (m_PositionOffset == Vector3.zero && m_RotationOffset == Vector3.zero)
                return;

            var decay = Mathf.Exp(-m_ReturnSpeed * deltaTime);
            m_PositionOffset *= decay;
            m_RotationOffset *= decay;
            if (m_PositionOffset.sqrMagnitude < 1e-10f && m_RotationOffset.sqrMagnitude < 1e-6f)
            {
                m_PositionOffset = Vector3.zero;
                m_RotationOffset = Vector3.zero;
            }

            Apply();
        }

        void Apply()
        {
            m_Pivot.localPosition = m_BasePosition + m_PositionOffset;
            m_Pivot.localRotation = m_BaseRotation * Quaternion.Euler(m_RotationOffset);
        }
    }
}
