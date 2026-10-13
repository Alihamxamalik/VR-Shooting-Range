using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>The range's own voice: countdown beeps, the start buzzer and the end-of-round horn.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class RangeAnnouncer : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [SerializeField] AudioClip m_CountdownBeep;
        [SerializeField] AudioClip m_StartBuzzer;
        [SerializeField] AudioClip m_EndBuzzer;
        [SerializeField] AudioClip m_PenaltyBuzzer;

        AudioSource m_Source;

        void Awake()
        {
            m_Source = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            m_Events.CountdownTick += OnCountdownTick;
            m_Events.RoundStateChanged += OnRoundStateChanged;
            m_Events.TargetHit += OnTargetHit;
        }

        void OnDisable()
        {
            m_Events.CountdownTick -= OnCountdownTick;
            m_Events.RoundStateChanged -= OnRoundStateChanged;
            m_Events.TargetHit -= OnTargetHit;
        }

        void OnCountdownTick(int secondsLeft)
        {
            Play(m_CountdownBeep);
        }

        void OnRoundStateChanged(RoundState state)
        {
            if (state == RoundState.Running)
                Play(m_StartBuzzer);
            else if (state == RoundState.Results)
                Play(m_EndBuzzer);
        }

        void OnTargetHit(TargetHit hit)
        {
            if (hit.BasePoints < 0)
                Play(m_PenaltyBuzzer);
        }

        void Play(AudioClip clip)
        {
            if (clip != null)
                m_Source.PlayOneShot(clip);
        }
    }
}
