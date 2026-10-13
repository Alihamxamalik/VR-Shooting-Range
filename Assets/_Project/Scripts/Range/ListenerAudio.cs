using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>
    /// Applies the player's volume setting to everything they hear and, with ear protection on, muffles it
    /// through a low-pass filter on the listener, the way earmuffs take the crack out of a shot.
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public sealed class ListenerAudio : MonoBehaviour
    {
        [SerializeField] ComfortSettings m_Comfort;
        [SerializeField] AudioLowPassFilter m_Muffle;
        [SerializeField, Range(0f, 1f)] float m_MuffledVolume = 0.8f;

        void OnEnable()
        {
            m_Comfort.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            m_Comfort.Changed -= Apply;
        }

        void Apply()
        {
            var muffled = m_Comfort.EarProtection;
            AudioListener.volume = m_Comfort.MasterVolume * (muffled ? m_MuffledVolume : 1f);
            if (m_Muffle != null)
                m_Muffle.enabled = muffled;
        }
    }
}
