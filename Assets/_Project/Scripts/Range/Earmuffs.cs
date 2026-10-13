using RangeControl.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RangeControl.Range
{
    /// <summary>
    /// Ear protection on the bench. Lift it to your head and let go to put it on, and gunshots are muffled
    /// as they would be on a real range. Switching ear protection off on the panel puts it back on the bench.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class Earmuffs : MonoBehaviour
    {
        [SerializeField] ComfortSettings m_Comfort;
        [SerializeField] PlayerAnchors m_Anchors;
        [SerializeField] GameObject m_Visual;
        [Tooltip("Letting go within this many metres of the head puts the earmuffs on.")]
        [SerializeField] float m_WearDistance = 0.3f;

        XRGrabInteractable m_Grab;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            transform.GetLocalPositionAndRotation(out m_HomePosition, out m_HomeRotation);
        }

        void OnEnable()
        {
            m_Grab.selectExited.AddListener(OnReleased);
            m_Comfort.Changed += Sync;
            Sync();
        }

        void OnDisable()
        {
            m_Grab.selectExited.RemoveListener(OnReleased);
            m_Comfort.Changed -= Sync;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            var head = m_Anchors != null ? m_Anchors.Head : null;
            var nearHead = head != null && Vector3.Distance(head.position, transform.position) <= m_WearDistance;
            if (nearHead)
                m_Comfort.EarProtection = true;
            else
                transform.SetLocalPositionAndRotation(m_HomePosition, m_HomeRotation);
        }

        void Sync()
        {
            var worn = m_Comfort.EarProtection;
            if (m_Visual.activeSelf != worn)
                return;

            // Worn earmuffs are out of sight on the player's head; taken off, they are back on the bench.
            m_Visual.SetActive(!worn);
            m_Grab.enabled = !worn;
            if (!worn)
                transform.SetLocalPositionAndRotation(m_HomePosition, m_HomeRotation);
        }
    }
}
