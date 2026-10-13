using System;
using RangeControl.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RangeControl.UI
{
    /// <summary>
    /// The small screen on the panel that plots each shot on a mini target, like the electronic scoring at a
    /// real range. At 50 m a bullet hole is a few pixels; this is how the player reads the group from the booth.
    /// </summary>
    public sealed class ScoringMonitor : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [Tooltip("The mini target. Shots are plotted inside it, with its half-width standing for the outermost ring.")]
        [SerializeField] RectTransform m_Face;
        [SerializeField] Image[] m_Dots = Array.Empty<Image>();
        [SerializeField] TMP_Text m_LastShot;
        [SerializeField] TMP_Text m_Total;
        [SerializeField] Color m_LatestColor = new Color(1f, 0.35f, 0.1f);
        [SerializeField] Color m_OlderColor = new Color(0.1f, 0.1f, 0.1f);

        int m_Next;
        int m_Latest = -1;
        int m_Sum;
        int m_Shots;

        void OnEnable()
        {
            m_Events.TargetHit += OnTargetHit;
            m_Events.PaperCleared += Clear;
            Clear();
        }

        void OnDisable()
        {
            m_Events.TargetHit -= OnTargetHit;
            m_Events.PaperCleared -= Clear;
        }

        public void Clear()
        {
            foreach (var dot in m_Dots)
                dot.enabled = false;
            m_Next = 0;
            m_Latest = -1;
            m_Sum = 0;
            m_Shots = 0;
            m_LastShot.SetText("-");
            m_Total.SetText("{0} / {1}", 0, 0);
        }

        void OnTargetHit(TargetHit hit)
        {
            if (hit.Ring < 0 || m_Dots.Length == 0 || hit.FaceRadius <= 0f)
                return;

            if (m_Latest >= 0)
                m_Dots[m_Latest].color = m_OlderColor;

            var dot = m_Dots[m_Next];
            m_Latest = m_Next;
            m_Next = (m_Next + 1) % m_Dots.Length;

            // Shots that land outside the rings are pinned to the edge of the monitor rather than drawn off it.
            var normalised = Vector2.ClampMagnitude(hit.FacePoint / hit.FaceRadius, 1.15f);
            dot.rectTransform.anchoredPosition = normalised * (m_Face.rect.width * 0.5f);
            dot.color = m_LatestColor;
            dot.enabled = true;

            m_Sum += hit.Ring;
            m_Shots++;
            if (hit.InnerX)
                m_LastShot.SetText("X");
            else
                m_LastShot.SetText("{0}", hit.Ring);
            m_Total.SetText("{0} / {1}", m_Sum, m_Shots * 10);
        }
    }
}
