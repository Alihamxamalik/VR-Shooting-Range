using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>
    /// Background sound. The outdoor ambience plays all the time; music plays only between rounds and on the
    /// results screen, and fades out when the range goes live so nothing competes with the shots.
    /// </summary>
    public sealed class Ambience : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [SerializeField] ComfortSettings m_Comfort;
        [SerializeField] AudioSource m_Music;
        [SerializeField] float m_FadeSeconds = 1.5f;

        float m_MusicLevel = 1f;
        bool m_Live;

        void OnEnable()
        {
            m_Events.RoundStateChanged += OnRoundStateChanged;
        }

        void OnDisable()
        {
            m_Events.RoundStateChanged -= OnRoundStateChanged;
        }

        void OnRoundStateChanged(RoundState state)
        {
            m_Live = state == RoundState.Countdown || state == RoundState.Running;
        }

        void Update()
        {
            if (m_Music == null)
                return;
            m_MusicLevel = Mathf.MoveTowards(m_MusicLevel, m_Live ? 0f : 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, m_FadeSeconds));
            m_Music.volume = m_MusicLevel * m_Comfort.MusicVolume;
        }
    }
}
