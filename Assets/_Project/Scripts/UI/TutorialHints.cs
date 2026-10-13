using RangeControl.Core;
using RangeControl.Weapons;
using TMPro;
using UnityEngine;

namespace RangeControl.UI
{
    /// <summary>
    /// The first-launch walkthrough: one floating hint at a time, placed next to the thing it talks about.
    /// Pick up the pistol, hit three steel plates, reload, press Start. It advances by listening to what the
    /// player actually does, and never blocks them from doing something else.
    /// </summary>
    public sealed class TutorialHints : MonoBehaviour
    {
        enum Step
        {
            PickUpGun,
            ShootPlates,
            EmptyGun,
            Reload,
            PressStart,
            Done,
        }

        [SerializeField] RangeEvents m_Events;
        [SerializeField] GameModeController m_Controller;
        [SerializeField] PlayerAnchors m_Anchors;
        [SerializeField] ComfortSettings m_Comfort;
        [SerializeField] Transform m_Hint;
        [SerializeField] TMP_Text m_Text;
        [SerializeField] Transform m_RackAnchor;
        [SerializeField] Transform m_PlatesAnchor;
        [SerializeField] Transform m_BenchAnchor;
        [SerializeField] Transform m_PanelAnchor;
        [SerializeField] int m_PlatesToHit = 3;

        Step m_Step = Step.PickUpGun;
        Weapon m_Weapon;
        Vector3 m_HintScale;
        int m_PlatesHit;

        public bool IsDone => m_Step == Step.Done;

        void Awake()
        {
            m_HintScale = m_Hint.localScale;
        }

        void OnEnable()
        {
            m_Events.WeaponGrabbed += OnWeaponGrabbed;
            m_Events.WeaponStateChanged += OnWeaponStateChanged;
            m_Events.TargetHit += OnTargetHit;
            m_Events.RoundStateChanged += OnRoundStateChanged;
        }

        void OnDisable()
        {
            m_Events.WeaponGrabbed -= OnWeaponGrabbed;
            m_Events.WeaponStateChanged -= OnWeaponStateChanged;
            m_Events.TargetHit -= OnTargetHit;
            m_Events.RoundStateChanged -= OnRoundStateChanged;
        }

        void Start()
        {
            if (m_Controller.Stats != null && m_Controller.Stats.Data.tutorialCompleted)
                Go(Step.Done);
            else
                Go(Step.PickUpGun);
        }

        void LateUpdate()
        {
            if (m_Step == Step.Done)
                return;
            var head = m_Anchors != null ? m_Anchors.Head : null;
            if (head == null)
                return;
            var away = Vector3.ProjectOnPlane(m_Hint.position - head.position, Vector3.up);
            if (away.sqrMagnitude > 1e-4f)
                m_Hint.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        void OnWeaponGrabbed(Weapon weapon)
        {
            m_Weapon = weapon;
            if (m_Step == Step.PickUpGun)
                Go(Step.ShootPlates);
        }

        void OnTargetHit(TargetHit hit)
        {
            if (m_Step != Step.ShootPlates || hit.Type != TargetType.SteelPlate)
                return;
            m_PlatesHit++;
            if (m_PlatesHit < m_PlatesToHit)
                Go(Step.ShootPlates);
            else
                Go(m_Weapon != null && m_Weapon.Rounds == 0 ? Step.Reload : Step.EmptyGun);
        }

        void OnWeaponStateChanged(Weapon weapon)
        {
            if (weapon != m_Weapon)
                return;

            var empty = weapon.Rounds == 0 || weapon.SlideLocked;
            if (m_Step == Step.EmptyGun && empty)
                Go(Step.Reload);
            else if (m_Step == Step.Reload && !empty && !weapon.IsReloading)
                Go(Step.PressStart);
            else if (m_Step == Step.ShootPlates)
                Go(Step.ShootPlates);
        }

        void OnRoundStateChanged(RoundState state)
        {
            // Starting a round by any route means the player has found their way around.
            if (state != RoundState.Idle && m_Step != Step.Done)
            {
                Go(Step.Done);
                m_Controller.CompleteTutorial();
            }
        }

        void Go(Step step)
        {
            m_Step = step;
            m_Hint.gameObject.SetActive(step != Step.Done);
            if (step == Step.Done)
                return;

            var quickReload = m_Comfort != null && m_Comfort.QuickReload;
            var reloadText = quickReload
                ? "Empty. Press B or Y on the gun hand to reload."
                : "Empty. Press B or Y to drop the magazine, take a fresh one from the tray or your belt, push it into the grip, then press A or X to release the slide.";

            switch (step)
            {
                case Step.PickUpGun:
                    Place(m_RackAnchor, "Reach for the pistol on the rack and squeeze the grip button to pick it up.");
                    break;
                case Step.ShootPlates:
                    // Running dry before the third plate should not strand the player without the reload hint.
                    if (m_Weapon != null && (m_Weapon.Rounds == 0 || m_Weapon.SlideLocked))
                        Place(m_BenchAnchor, reloadText);
                    else
                        Place(m_PlatesAnchor, "Pull the trigger to fire. Hit the three steel plates.  " + m_PlatesHit + " / " + m_PlatesToHit);
                    break;
                case Step.EmptyGun:
                    Place(m_BenchAnchor, "Good. Keep firing until the pistol runs empty.");
                    break;
                case Step.Reload:
                    Place(m_BenchAnchor, reloadText);
                    break;
                case Step.PressStart:
                    Place(m_PanelAnchor, "Set the range on this panel, then press START. From here you are on your own.");
                    break;
            }
        }

        void Place(Transform anchor, string text)
        {
            m_Text.text = text;
            if (anchor == null)
                return;
            // An anchor far down the range carries a larger scale, so its hint stays readable from the booth.
            m_Hint.position = anchor.position;
            m_Hint.localScale = m_HintScale * anchor.lossyScale.x;
        }
    }
}
