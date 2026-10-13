using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// The face buttons of each controller, shared by every gun. A gun asks for the buttons of whichever hand
    /// is holding it: A or X is the primary button, B or Y the secondary.
    /// </summary>
    [CreateAssetMenu(menuName = "Range Control/Hand Buttons", fileName = "HandButtons")]
    public sealed class HandButtons : ScriptableObject
    {
        [SerializeField] InputActionReference m_LeftPrimary;
        [SerializeField] InputActionReference m_LeftSecondary;
        [SerializeField] InputActionReference m_RightPrimary;
        [SerializeField] InputActionReference m_RightSecondary;

        /// <summary>A on the right controller, X on the left.</summary>
        public bool PrimaryPressed(InteractorHandedness hand)
        {
            return WasPressed(hand == InteractorHandedness.Left ? m_LeftPrimary : m_RightPrimary);
        }

        /// <summary>B on the right controller, Y on the left.</summary>
        public bool SecondaryPressed(InteractorHandedness hand)
        {
            return WasPressed(hand == InteractorHandedness.Left ? m_LeftSecondary : m_RightSecondary);
        }

        static bool WasPressed(InputActionReference reference)
        {
            return reference != null && reference.action != null && reference.action.WasPressedThisFrame();
        }
    }
}
