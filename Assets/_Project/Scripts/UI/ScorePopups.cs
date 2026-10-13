using System.Collections.Generic;
using RangeControl.Core;
using TMPro;
using UnityEngine;

namespace RangeControl.UI
{
    /// <summary>
    /// Floating score numbers at the target. They grow with distance so a hit at 50 m reads as easily as one
    /// at 5 m, always face the player and come from a pool.
    /// </summary>
    public sealed class ScorePopups : MonoBehaviour
    {
        struct Popup
        {
            public TextMeshPro Text;
            public Vector3 Start;
            public float Age;
        }

        [SerializeField] RangeEvents m_Events;
        [SerializeField] PlayerAnchors m_Anchors;
        [SerializeField] TextMeshPro m_Prefab;
        [SerializeField] int m_Prewarm = 8;
        [SerializeField] float m_Lifetime = 0.9f;
        [SerializeField] float m_Rise = 0.45f;
        [Tooltip("World scale of a popup per metre of distance from the player.")]
        [SerializeField] float m_ScalePerMetre = 0.06f;
        [SerializeField] float m_MinScale = 0.3f;
        [SerializeField] Color m_GainColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] Color m_PenaltyColor = new Color(0.95f, 0.2f, 0.15f);
        [SerializeField] Color m_PracticeColor = Color.white;

        readonly List<Popup> m_Active = new List<Popup>(16);
        PrefabPool<TextMeshPro> m_Pool;

        void Awake()
        {
            m_Pool = new PrefabPool<TextMeshPro>(m_Prefab, transform, m_Prewarm);
        }

        void OnEnable()
        {
            m_Events.ScoreAwarded += OnScoreAwarded;
        }

        void OnDisable()
        {
            m_Events.ScoreAwarded -= OnScoreAwarded;
        }

        void OnScoreAwarded(ScoreAward award)
        {
            if (award.Points == 0)
                return;

            var text = m_Pool.Get();
            if (award.Points > 0)
                text.SetText("+{0}", award.Points);
            else
                text.SetText("{0}", award.Points);
            text.color = award.Points < 0 ? m_PenaltyColor : (award.Counted ? m_GainColor : m_PracticeColor);

            var start = award.Position + Vector3.up * 0.25f;
            var head = m_Anchors != null ? m_Anchors.Head : null;
            var distance = head != null ? Vector3.Distance(head.position, start) : award.Distance;
            var popup = text.transform;
            popup.position = start;
            popup.localScale = Vector3.one * Mathf.Max(m_MinScale, distance * m_ScalePerMetre);
            if (head != null)
                popup.rotation = Quaternion.LookRotation(start - head.position, Vector3.up);

            m_Active.Add(new Popup { Text = text, Start = start, Age = 0f });
        }

        void Update()
        {
            for (var i = m_Active.Count - 1; i >= 0; i--)
            {
                var popup = m_Active[i];
                popup.Age += Time.deltaTime;
                if (popup.Age >= m_Lifetime)
                {
                    m_Pool.Release(popup.Text);
                    m_Active[i] = m_Active[m_Active.Count - 1];
                    m_Active.RemoveAt(m_Active.Count - 1);
                    continue;
                }

                var progress = popup.Age / m_Lifetime;
                popup.Text.transform.position = popup.Start + Vector3.up * (m_Rise * popup.Text.transform.localScale.x * progress);
                popup.Text.alpha = 1f - progress * progress;
                m_Active[i] = popup;
            }
        }
    }
}
