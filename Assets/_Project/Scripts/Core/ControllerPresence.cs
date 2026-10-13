using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Core
{
    /// <summary>
    /// Sits on each controller. While that hand is holding something, the controller model is hidden so it
    /// does not poke through the gun, and the fingertip poke is switched off so the gun hand cannot press
    /// panel buttons by accident.
    /// </summary>
    public sealed class ControllerPresence : MonoBehaviour
    {
        [SerializeField] XRBaseInteractor m_GrabInteractor;
        [SerializeField] GameObject m_ControllerVisual;
        [SerializeField] Behaviour m_PokeInteractor;

        void OnEnable()
        {
            m_GrabInteractor.selectEntered.AddListener(OnSelectEntered);
            m_GrabInteractor.selectExited.AddListener(OnSelectExited);
            Refresh();
        }

        void OnDisable()
        {
            m_GrabInteractor.selectEntered.RemoveListener(OnSelectEntered);
            m_GrabInteractor.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            Refresh();
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            Refresh();
        }

        void Refresh()
        {
            var holding = false;
            var selected = m_GrabInteractor.interactablesSelected;
            for (var i = 0; i < selected.Count; i++)
            {
                if (selected[i] is XRGrabInteractable)
                {
                    holding = true;
                    break;
                }
            }

            if (m_ControllerVisual != null)
                m_ControllerVisual.SetActive(!holding);
            if (m_PokeInteractor != null)
                m_PokeInteractor.enabled = !holding;
        }
    }
}
