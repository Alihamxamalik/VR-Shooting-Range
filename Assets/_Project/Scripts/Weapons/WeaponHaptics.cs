using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// Plays a shot's two-pulse haptic pattern: a sharp hit, then a softer tail once the first pulse ends.
    /// The supporting hand gets a lighter copy. The tail is timed here rather than in a coroutine, so firing
    /// allocates nothing.
    /// </summary>
    public sealed class WeaponHaptics : MonoBehaviour
    {
        XRBaseInputInteractor m_Main;
        XRBaseInputInteractor m_Support;
        float m_TailDelay = -1f;
        float m_TailAmplitude;
        float m_TailDuration;
        float m_SupportScale;

        /// <summary><paramref name="strength"/> is the player's haptic setting from 0 to 1.</summary>
        public void Play(WeaponData weapon, XRBaseInputInteractor main, XRBaseInputInteractor support, float strength)
        {
            if (strength <= 0f)
                return;

            m_Main = main;
            m_Support = support;
            m_SupportScale = weapon.SupportHandHaptics;
            m_TailDelay = weapon.HapticDuration;
            m_TailAmplitude = weapon.HapticTailAmplitude * strength;
            m_TailDuration = weapon.HapticTailDuration;
            Send(weapon.HapticAmplitude * strength, weapon.HapticDuration);
        }

        public void Tick(float deltaTime)
        {
            if (m_TailDelay < 0f)
                return;
            m_TailDelay -= deltaTime;
            if (m_TailDelay > 0f)
                return;
            m_TailDelay = -1f;
            Send(m_TailAmplitude, m_TailDuration);
        }

        void Send(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f)
                return;
            if (m_Main != null)
                m_Main.SendHapticImpulse(amplitude, duration);
            if (m_Support != null)
                m_Support.SendHapticImpulse(amplitude * m_SupportScale, duration);
        }
    }
}
