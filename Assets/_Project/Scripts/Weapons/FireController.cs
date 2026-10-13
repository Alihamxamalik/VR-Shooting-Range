using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Weapons
{
    /// <summary>
    /// Turns an analog trigger value into shots. The trigger has to pass 80% to fire and fall below 30% to
    /// re-arm, which stops a twitchy finger from doubling. Plain C#, so the timing is covered by tests.
    /// </summary>
    public sealed class FireController
    {
        public const float PressThreshold = 0.8f;
        public const float ReleaseThreshold = 0.3f;

        bool m_Armed = true;
        bool m_Held;
        float m_Cooldown;

        /// <summary>True from the moment the trigger passes the press point until it re-arms.</summary>
        public bool IsHeld => m_Held;

        /// <summary>
        /// Advances by <paramref name="deltaTime"/> with the current trigger value, and returns true when a
        /// shot is due this frame. Semi-automatic fires once per pull, no faster than the rate allows;
        /// automatic keeps firing at the rate while the trigger stays in.
        /// </summary>
        public bool Tick(float trigger, float deltaTime, FireMode mode, float roundsPerMinute)
        {
            // Carry at most one frame of overshoot into the next interval, so automatic fire holds its
            // average rate at any frame rate without banking shots while the gun sits idle.
            m_Cooldown = Mathf.Max(m_Cooldown - deltaTime, -deltaTime);

            var pressed = false;
            if (m_Armed && trigger >= PressThreshold)
            {
                m_Armed = false;
                m_Held = true;
                pressed = true;
            }
            else if (!m_Armed && trigger < ReleaseThreshold)
            {
                m_Armed = true;
                m_Held = false;
            }

            var wantsShot = mode == FireMode.Auto ? m_Held : pressed;
            if (!wantsShot || m_Cooldown > 0f)
                return false;

            // A pull after a pause starts a fresh interval; only shots that follow on keep the remainder.
            if (pressed)
                m_Cooldown = 0f;
            m_Cooldown += roundsPerMinute > 0f ? 60f / roundsPerMinute : 0f;
            return true;
        }

        /// <summary>
        /// Stops automatic fire until the trigger is released and pulled again. Used when the gun runs dry
        /// or starts a reload with the trigger still held.
        /// </summary>
        public void Interrupt()
        {
            m_Held = false;
        }
    }

    /// <summary>
    /// How much the shot cone has opened up during a burst. It grows with each shot and settles back
    /// linearly, taking the weapon's recovery time from fully bloomed.
    /// </summary>
    public struct SpreadBloom
    {
        float m_Bloom;

        public float Value => m_Bloom;

        public void AddShot(float perShot, float max)
        {
            m_Bloom = Mathf.Min(max, m_Bloom + perShot);
        }

        public void Recover(float deltaTime, float max, float recoverySeconds)
        {
            if (m_Bloom <= 0f)
                return;
            var rate = recoverySeconds > 0f ? max / recoverySeconds : float.MaxValue;
            m_Bloom = Mathf.Max(0f, m_Bloom - rate * deltaTime);
        }
    }
}
