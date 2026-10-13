using System;
using System.Text;
using RangeControl.Core;
using RangeControl.Weapons;
using TMPro;
using UnityEngine;

namespace RangeControl.UI
{
    /// <summary>
    /// The stats board in the booth: lifetime shots, hits and accuracy for each gun, and the best score in
    /// every mode and tier. It is rewritten when a round ends, never while one is running.
    /// </summary>
    public sealed class StatsBoardUI : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [SerializeField] GameModeController m_Controller;
        [SerializeField] WeaponData[] m_Weapons = Array.Empty<WeaponData>();
        [SerializeField] GameModeDefinition[] m_Modes = Array.Empty<GameModeDefinition>();
        [SerializeField] TMP_Text m_WeaponColumn;
        [SerializeField] TMP_Text m_BestColumn;

        readonly StringBuilder m_Builder = new StringBuilder(512);

        void OnEnable()
        {
            m_Events.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            m_Events.RoundEnded -= OnRoundEnded;
        }

        void Start()
        {
            Refresh();
        }

        void OnRoundEnded(RoundResult result)
        {
            Refresh();
        }

        public void Refresh()
        {
            var stats = m_Controller.Stats;
            if (stats == null)
                return;

            m_Builder.Clear();
            foreach (var weapon in m_Weapons)
            {
                var entry = stats.FindWeapon(weapon.Id);
                m_Builder.Append("<b>").Append(weapon.DisplayName).Append("</b>\n");
                if (entry == null || entry.shots == 0)
                {
                    m_Builder.Append("<color=#9AA0A6>not fired yet</color>\n");
                    continue;
                }

                m_Builder.Append(entry.shots.ToString("N0")).Append(" shots   ")
                    .Append(entry.hits.ToString("N0")).Append(" hits   ")
                    .Append(Mathf.RoundToInt(entry.Accuracy * 100f)).Append("%");
                if (entry.longestHit > 0f)
                    m_Builder.Append("   ").Append(entry.longestHit.ToString("0")).Append(" m");
                m_Builder.Append('\n');
            }

            m_WeaponColumn.text = m_Builder.ToString();

            m_Builder.Clear();
            foreach (var mode in m_Modes)
            {
                var tiers = Mathf.Max(1, mode.TierCount);
                for (var tier = 0; tier < tiers; tier++)
                {
                    var best = stats.BestScore(mode.BoardKey(tier));
                    m_Builder.Append("<b>").Append(mode.DisplayName);
                    if (mode.TierCount > 0)
                        m_Builder.Append(" · ").Append(mode.GetTier(tier).name);
                    m_Builder.Append("</b>   ").Append(best > 0 ? best.ToString("N0") : "-").Append('\n');
                }
            }

            m_Builder.Append('\n');
            m_Builder.Append("Longest hit   ").Append(stats.Data.longestHit > 0f ? stats.Data.longestHit.ToString("0.0") + " m" : "-").Append('\n');
            m_Builder.Append("Best group   ").Append(stats.Data.bestGroupCm > 0f ? stats.Data.bestGroupCm.ToString("0.0") + " cm" : "-");
            m_BestColumn.text = m_Builder.ToString();
        }
    }
}
