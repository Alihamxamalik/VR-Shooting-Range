using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// A magazine, or a single shotgun shell, that can be grabbed and pushed into the gun it fits. One left
    /// lying around goes back to its pool after a few seconds, so the floor never fills up. While it is seated
    /// in a gun, hands cannot take hold of it: it comes out with the release button, and a hand reaching for
    /// the handguard gets the rifle, not the magazine beside it.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Magazine : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
        [Tooltip("The gun this magazine fits.")]
        [SerializeField] WeaponData m_Weapon;
        [SerializeField] int m_Rounds = 7;
        [Tooltip("Seconds a magazine nobody is holding stays in the world.")]
        [SerializeField] float m_DespawnDelay = 4f;

        XRGrabInteractable m_Grab;
        Rigidbody m_Body;
        Action<Magazine> m_Release;
        float m_IdleTime;
        bool m_Seated;

        public WeaponData Weapon => m_Weapon;
        public XRGrabInteractable Grab => m_Grab;

        public int Rounds
        {
            get => m_Rounds;
            set => m_Rounds = Mathf.Max(0, value);
        }

        /// <summary>True while the magazine is locked into a gun. Set by the gun's magazine well.</summary>
        public bool IsSeated
        {
            get => m_Seated;
            set => m_Seated = value;
        }

        bool IXRHoverFilter.canProcess => true;
        bool IXRSelectFilter.canProcess => true;

        bool IXRHoverFilter.Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            return !m_Seated || interactor is XRSocketInteractor;
        }

        bool IXRSelectFilter.Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return !m_Seated || interactor is XRSocketInteractor;
        }

        public bool Fits(WeaponData weapon)
        {
            return weapon != null && weapon == m_Weapon;
        }

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Body = GetComponent<Rigidbody>();
            m_Grab.hoverFilters.Add(this);
            m_Grab.selectFilters.Add(this);
        }

        /// <summary>Readies a magazine that has just come out of a pool.</summary>
        public void Prepare(int rounds, Action<Magazine> release)
        {
            m_Rounds = rounds;
            m_Release = release;
            m_IdleTime = 0f;
            m_Seated = false;
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
        }

        /// <summary>Returns the magazine to its pool, or destroys it if it did not come from one.</summary>
        public void Despawn()
        {
            if (m_Release != null)
                m_Release(this);
            else
                Destroy(gameObject);
        }

        void Update()
        {
            // In a hand, a gun or a tray slot the magazine is selected and stays. Loose, it times out.
            if (m_Grab.isSelected)
            {
                m_IdleTime = 0f;
                return;
            }

            m_IdleTime += Time.deltaTime;
            if (m_IdleTime >= m_DespawnDelay)
                Despawn();
        }
    }
}
