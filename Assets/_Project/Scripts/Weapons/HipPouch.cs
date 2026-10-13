using RangeControl.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// A magazine pouch that rides on the player's belt. Reaching down and gripping there puts a fresh
    /// magazine for the gun in use straight into that hand. The zone spans the front of the waist, so it
    /// works for either hand.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class HipPouch : MonoBehaviour
    {
        [SerializeField] PlayerAnchors m_Anchors;
        [SerializeField] AmmoSupply m_Supply;
        [Tooltip("Where the pouch sits relative to the head, in metres, turning with the body but not tilting with the head.")]
        [SerializeField] Vector3 m_Offset = new Vector3(0f, -0.62f, 0.06f);

        XRSimpleInteractable m_Interactable;
        IXRSelectInteractor m_Requester;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
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
            // Only a hand takes a magazine; a poke or a socket touching the zone does nothing.
            if (args.interactorObject is XRBaseInputInteractor)
                m_Requester = args.interactorObject;
        }

        void Update()
        {
            // The swap is done here, outside the interaction manager's own update, one frame after the grip.
            if (m_Requester == null)
                return;
            var hand = m_Requester;
            m_Requester = null;

            var manager = m_Interactable.interactionManager;
            if (manager == null)
                return;
            manager.SelectExit(hand, m_Interactable);

            var attach = hand.GetAttachTransform(m_Interactable);
            var magazine = m_Supply.Spawn(m_Supply.ActiveWeapon, attach.position, attach.rotation);
            if (magazine != null)
                manager.SelectEnter(hand, magazine.Grab);
        }

        void LateUpdate()
        {
            var head = m_Anchors != null ? m_Anchors.Head : null;
            if (head == null)
                return;

            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
            var facing = Quaternion.LookRotation(forward.normalized, Vector3.up);
            transform.SetPositionAndRotation(head.position + facing * m_Offset, facing);
        }
    }
}
