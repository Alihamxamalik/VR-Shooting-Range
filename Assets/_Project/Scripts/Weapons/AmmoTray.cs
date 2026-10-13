using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// The tray of fresh magazines on the bench. It stocks its slots for the gun in use and refills a slot a
    /// moment after its magazine is taken.
    /// </summary>
    public sealed class AmmoTray : MonoBehaviour
    {
        [SerializeField] AmmoSupply m_Supply;
        [SerializeField] MagazineSocket[] m_Slots = Array.Empty<MagazineSocket>();
        [SerializeField] float m_RefillDelay = 1f;

        float[] m_Timers;

        void Awake()
        {
            m_Timers = new float[m_Slots.Length];
        }

        void OnEnable()
        {
            m_Supply.ActiveWeaponChanged += Restock;
        }

        void OnDisable()
        {
            m_Supply.ActiveWeaponChanged -= Restock;
        }

        void Restock()
        {
            // A different gun came off the rack: clear out what no longer fits and fill up straight away.
            for (var i = 0; i < m_Slots.Length; i++)
            {
                var magazine = m_Slots[i].Magazine;
                if (magazine != null && !magazine.Fits(m_Supply.ActiveWeapon))
                    magazine.Despawn();
                m_Timers[i] = 0f;
            }
        }

        void Update()
        {
            var weapon = m_Supply.ActiveWeapon;
            if (weapon == null)
                return;

            for (var i = 0; i < m_Slots.Length; i++)
            {
                var slot = m_Slots[i];
                if (slot.Magazine != null)
                {
                    m_Timers[i] = m_RefillDelay;
                    continue;
                }

                m_Timers[i] -= Time.deltaTime;
                if (m_Timers[i] > 0f)
                    continue;

                m_Timers[i] = m_RefillDelay;
                var attach = slot.attachTransform != null ? slot.attachTransform : slot.transform;
                var magazine = m_Supply.Spawn(weapon, attach.position, attach.rotation);
                if (magazine != null && slot.interactionManager != null)
                    slot.interactionManager.SelectEnter((IXRSelectInteractor)slot, (IXRSelectInteractable)magazine.Grab);
            }
        }
    }
}
