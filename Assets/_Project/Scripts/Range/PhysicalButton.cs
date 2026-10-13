using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RangeControl.Range
{
    /// <summary>
    /// A chunky button on the console that is pressed with a fingertip. Any selection counts as a press, so
    /// a poke, a grip on it or a ray from across the booth all work.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PhysicalButton : MonoBehaviour
    {
        [Tooltip("The part that moves down when pressed.")]
        [SerializeField] Transform m_Cap;
        [SerializeField] Vector3 m_PressedOffset = new Vector3(0f, -0.012f, 0f);
        [Tooltip("Seconds before the button can be pressed again, so one push is one press.")]
        [SerializeField] float m_Cooldown = 0.4f;
        [SerializeField] AudioSource m_Click;

        XRSimpleInteractable m_Interactable;
        Vector3 m_CapRest;
        float m_Depth;
        float m_LastPressTime = float.NegativeInfinity;

        public event Action Pressed;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            if (m_Cap != null)
                m_CapRest = m_Cap.localPosition;
        }

        void OnEnable()
        {
            m_Interactable.selectEntered.AddListener(OnSelected);
        }

        void OnDisable()
        {
            m_Interactable.selectEntered.RemoveListener(OnSelected);
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            Press();
        }

        /// <summary>Presses the button from code, exactly as a finger would.</summary>
        public void Press()
        {
            if (Time.unscaledTime - m_LastPressTime < m_Cooldown)
                return;
            m_LastPressTime = Time.unscaledTime;
            m_Depth = 1f;
            if (m_Click != null)
                m_Click.Play();
            Pressed?.Invoke();
        }

        void Update()
        {
            if (m_Cap == null || m_Depth <= 0f)
                return;
            m_Depth = Mathf.Max(0f, m_Depth - Time.unscaledDeltaTime * 5f);
            m_Cap.localPosition = m_CapRest + m_PressedOffset * Mathf.Clamp01(m_Depth * 2f);
        }
    }
}
