using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RangeControl.Weapons
{
    /// <summary>
    /// Sends a gun back to its slot on the rack a moment after it is let go, so nobody has to bend down to
    /// the floor for it. The slot is the gun's parent: the grab interactable hands the gun back to it on
    /// release, and this glides the gun home from wherever it was dropped.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class RackReturn : MonoBehaviour
    {
        [Tooltip("Seconds the gun hangs where it was released before it heads back.")]
        [SerializeField] float m_Delay = 2f;
        [SerializeField] float m_TravelSeconds = 0.5f;

        XRGrabInteractable m_Grab;
        Vector3 m_FromPosition;
        Quaternion m_FromRotation;
        float m_Timer = -1f;
        float m_Progress = -1f;

        public bool IsHome => !m_Grab.isSelected && m_Timer < 0f && m_Progress < 0f;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnGrabbed);
            m_Grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnGrabbed);
            m_Grab.selectExited.RemoveListener(OnReleased);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_Timer = -1f;
            m_Progress = -1f;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (!m_Grab.isSelected)
                m_Timer = m_Delay;
        }

        void Update()
        {
            if (m_Timer >= 0f)
            {
                m_Timer -= Time.deltaTime;
                if (m_Timer >= 0f)
                    return;
                m_FromPosition = transform.localPosition;
                m_FromRotation = transform.localRotation;
                m_Progress = 0f;
            }

            if (m_Progress < 0f)
                return;

            m_Progress = Mathf.Min(1f, m_Progress + Time.deltaTime / Mathf.Max(0.01f, m_TravelSeconds));
            var eased = Mathf.SmoothStep(0f, 1f, m_Progress);
            transform.SetLocalPositionAndRotation(
                Vector3.Lerp(m_FromPosition, Vector3.zero, eased),
                Quaternion.Slerp(m_FromRotation, Quaternion.identity, eased));
            if (m_Progress >= 1f)
                m_Progress = -1f;
        }
    }
}
