using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// The magazine well of a gun, or the loading port of a shotgun. Unlike an ordinary socket it does not
    /// wait for the hand to let go: pushing a fitting magazine into it is enough, which is how a reload feels.
    /// For a moment after a magazine comes out the well takes nothing, so the one that was just dropped or
    /// pulled is not sucked straight back in.
    /// </summary>
    public sealed class MagwellSocket : MagazineSocket
    {
        [SerializeField] Weapon m_Weapon;

        float m_ClosedUntil;

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            if (!TryGetAccepted(interactable, out _))
                return false;
            if (IsSelecting(interactable))
                return interactable.interactorsSelecting.Count == 1;
            // A socket's own recycle delay only stops it hovering; it would still re-select, so the well shuts itself.
            return !hasSelection && Time.time >= m_ClosedUntil;
        }

        protected override bool Accepts(Magazine magazine)
        {
            return m_Weapon != null && magazine.Fits(m_Weapon.Data) && m_Weapon.CanAccept(magazine);
        }

        /// <summary>Lets go of the magazine so it drops out of the gun.</summary>
        public void Eject()
        {
            if (Magazine != null && interactionManager != null)
                interactionManager.SelectExit((IXRSelectInteractor)this, (IXRSelectInteractable)Magazine.Grab);
        }

        /// <summary>Puts a loose magazine straight into the well, as when a gun is set up with one loaded.</summary>
        public void Insert(Magazine magazine)
        {
            if (Magazine == null && interactionManager != null)
                interactionManager.SelectEnter((IXRSelectInteractor)this, (IXRSelectInteractable)magazine.Grab);
        }

        protected override void OnMagazineInserted(Magazine magazine)
        {
            magazine.IsSeated = true;
            m_Weapon.OnMagazineInserted(magazine);
        }

        protected override void OnMagazineRemoved(Magazine magazine)
        {
            // A shell is used up as it goes in, so the port can take the next one at once.
            if (!m_Weapon.Data.ShellFed)
                m_ClosedUntil = Time.time + recycleDelayTime;
            magazine.IsSeated = false;
            m_Weapon.OnMagazineRemoved(magazine);
        }
    }
}
