using RangeControl.Core;
using RangeControl.Weapons;
using TMPro;
using UnityEngine;

namespace RangeControl.UI
{
    /// <summary>
    /// The small readout on a gun: rounds left, fire mode and, during a round, the combo. It is beside the
    /// gun hand, where the eye already is, and only shows while the gun is held.
    /// </summary>
    public sealed class WeaponHud : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [SerializeField] Weapon m_Weapon;
        [SerializeField] GameObject m_Root;
        [SerializeField] TMP_Text m_Ammo;
        [SerializeField] TMP_Text m_Combo;
        [SerializeField] Color m_NormalColor = Color.white;
        [SerializeField] Color m_EmptyColor = new Color(1f, 0.3f, 0.2f);

        void OnEnable()
        {
            m_Events.WeaponGrabbed += OnWeaponChanged;
            m_Events.WeaponReleased += OnWeaponChanged;
            m_Events.WeaponStateChanged += OnWeaponChanged;
            m_Events.ScoreAwarded += OnScoreAwarded;
            m_Events.ComboBroken += ClearCombo;
            m_Events.RoundStateChanged += OnRoundStateChanged;
            m_Root.SetActive(false);
            ClearCombo();
        }

        void OnDisable()
        {
            m_Events.WeaponGrabbed -= OnWeaponChanged;
            m_Events.WeaponReleased -= OnWeaponChanged;
            m_Events.WeaponStateChanged -= OnWeaponChanged;
            m_Events.ScoreAwarded -= OnScoreAwarded;
            m_Events.ComboBroken -= ClearCombo;
            m_Events.RoundStateChanged -= OnRoundStateChanged;
        }

        void OnWeaponChanged(Weapon weapon)
        {
            if (weapon != m_Weapon)
                return;

            m_Root.SetActive(m_Weapon.IsHeld);
            if (!m_Weapon.IsHeld)
                return;

            if (m_Weapon.IsReloading)
                m_Ammo.SetText("RELOADING");
            else if (m_Weapon.SlideLocked && m_Weapon.Rounds > 0)
                m_Ammo.SetText("RELEASE SLIDE");
            else if (m_Weapon.FireMode == FireMode.Auto)
                m_Ammo.SetText("{0}/{1} AUTO", m_Weapon.Rounds, m_Weapon.Capacity);
            else
                m_Ammo.SetText("{0}/{1} SEMI", m_Weapon.Rounds, m_Weapon.Capacity);
            m_Ammo.color = m_Weapon.Rounds == 0 ? m_EmptyColor : m_NormalColor;
        }

        void OnScoreAwarded(ScoreAward award)
        {
            if (!m_Weapon.IsHeld || !award.Counted)
                return;
            if (award.Combo > 1f)
                m_Combo.SetText("x{0:2}", award.Combo);
            else
                ClearCombo();
        }

        void OnRoundStateChanged(RoundState state)
        {
            ClearCombo();
        }

        void ClearCombo()
        {
            m_Combo.SetText(string.Empty);
        }
    }
}
