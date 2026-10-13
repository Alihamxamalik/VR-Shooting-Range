using RangeControl.Core;
using RangeControl.Weapons;
using TMPro;
using UnityEngine;

namespace RangeControl.UI
{
    /// <summary>
    /// The name plate under a gun on the rack. It turns gold once the player has landed enough hits with
    /// that gun. Progression in this game is cosmetic only: nothing is ever locked.
    /// </summary>
    public sealed class RackPlate : MonoBehaviour
    {
        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] RangeEvents m_Events;
        [SerializeField] GameModeController m_Controller;
        [SerializeField] WeaponData m_Weapon;
        [SerializeField] Renderer m_Plate;
        [SerializeField] TMP_Text m_Label;
        [SerializeField] int m_GoldAtHits = 500;
        [SerializeField] Color m_PlainColor = new Color(0.2f, 0.2f, 0.22f);
        [SerializeField] Color m_GoldColor = new Color(1f, 0.78f, 0.2f);

        MaterialPropertyBlock m_Block;

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
            if (m_Label != null && m_Weapon != null)
                m_Label.text = m_Weapon.DisplayName;
            Refresh();
        }

        void OnRoundEnded(RoundResult result)
        {
            Refresh();
        }

        void Refresh()
        {
            if (m_Plate == null || m_Weapon == null || m_Controller.Stats == null)
                return;
            var stats = m_Controller.Stats.FindWeapon(m_Weapon.Id);
            var gold = stats != null && stats.hits >= m_GoldAtHits;
            m_Block ??= new MaterialPropertyBlock();
            m_Block.SetColor(s_BaseColor, gold ? m_GoldColor : m_PlainColor);
            m_Plate.SetPropertyBlock(m_Block);
        }
    }
}
