using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// A socket that only takes magazines. On its own it is a slot in the ammunition tray;
    /// <see cref="MagwellSocket"/> extends it into the magazine well of a gun.
    /// </summary>
    public class MagazineSocket : XRSocketInteractor
    {
        /// <summary>The magazine in the socket, or null.</summary>
        public Magazine Magazine { get; private set; }

        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && TryGetAccepted(interactable, out _);
        }

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && TryGetAccepted(interactable, out _);
        }

        /// <summary>Whether this socket takes the magazine. The tray takes any; a gun only its own.</summary>
        protected virtual bool Accepts(Magazine magazine)
        {
            return true;
        }

        protected bool TryGetAccepted(IXRInteractable interactable, out Magazine magazine)
        {
            return interactable.transform.TryGetComponent(out magazine) && Accepts(magazine);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (args.interactableObject.transform.TryGetComponent(out Magazine magazine))
            {
                Magazine = magazine;
                OnMagazineInserted(magazine);
            }
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            if (Magazine == null)
                return;
            var magazine = Magazine;
            Magazine = null;
            OnMagazineRemoved(magazine);
        }

        protected virtual void OnMagazineInserted(Magazine magazine) { }

        protected virtual void OnMagazineRemoved(Magazine magazine) { }
    }
}
