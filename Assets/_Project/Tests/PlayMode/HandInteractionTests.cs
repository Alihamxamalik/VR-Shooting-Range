using System.Collections;
using System.Linq;
using NUnit.Framework;
using RangeControl.Core;
using RangeControl.Range;
using RangeControl.Targets;
using RangeControl.UI;
using RangeControl.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace RangeControl.Tests
{
    /// <summary>
    /// Drives the real hands of the rig: a controller is moved by hand, its grip and trigger are pressed
    /// through XRI's own input readers, and the interaction manager does the rest exactly as it would with a
    /// headset on. This is what stands in for picking a gun up, pulling its trigger, reloading and pressing
    /// buttons until the same things are checked on the device.
    /// </summary>
    public class HandInteractionTests : RangeSceneFixture
    {
        Transform m_RightController;
        Transform m_LeftController;
        NearFarInteractor m_RightHand;
        NearFarInteractor m_LeftHand;
        XRPokeInteractor m_RightFinger;
        int m_Grabbed;
        int m_Released;

        [UnitySetUp]
        public IEnumerator TakeOverTheHands()
        {
            var rig = GameObject.Find("XR Origin").transform;
            m_RightController = rig.Find("Camera Offset/Right Controller");
            m_LeftController = rig.Find("Camera Offset/Left Controller");

            // No tracked devices are needed: the rig is told to stop switching hands on and off with them, the
            // controllers are switched on by hand, and the test places them itself.
            rig.GetComponent<XRInputModalityManager>().enabled = false;
            foreach (var controller in new[] { m_RightController, m_LeftController })
            {
                foreach (var driver in controller.GetComponents<TrackedPoseDriver>())
                    driver.enabled = false;
                controller.gameObject.SetActive(true);
            }

            yield return null;

            m_RightHand = m_RightController.GetComponentInChildren<NearFarInteractor>();
            m_LeftHand = m_LeftController.GetComponentInChildren<NearFarInteractor>();
            m_RightFinger = m_RightController.GetComponentInChildren<XRPokeInteractor>();
            foreach (var hand in new[] { m_RightHand, m_LeftHand })
            {
                hand.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
                hand.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            }

            m_RightController.SetPositionAndRotation(new Vector3(0.3f, 1.2f, -0.7f), Quaternion.identity);
            m_LeftController.SetPositionAndRotation(new Vector3(-0.3f, 1.2f, -0.7f), Quaternion.identity);
            m_Grabbed = m_Released = 0;
            m_Events.WeaponGrabbed += OnGrabbed;
            m_Events.WeaponReleased += OnReleased;
            yield return Frames(2);
        }

        [UnityTearDown]
        public IEnumerator ReleaseTheHands()
        {
            m_Events.WeaponGrabbed -= OnGrabbed;
            m_Events.WeaponReleased -= OnReleased;
            yield return null;
        }

        void OnGrabbed(Weapon weapon) => m_Grabbed++;
        void OnReleased(Weapon weapon) => m_Released++;

        /// <summary>
        /// Waits for a physics step and a few frames. A hand finds things by overlap query, and the physics
        /// scene only learns that something moved at its next step.
        /// </summary>
        static IEnumerator Settle()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return Frames(3);
        }

        static IEnumerator Grip(NearFarInteractor hand)
        {
            hand.selectInput.QueueManualState(true, 1f);
            yield return Frames(3);
        }

        static IEnumerator LetGo(NearFarInteractor hand)
        {
            hand.selectInput.QueueManualState(false, 0f);
            yield return Frames(3);
        }

        /// <summary>Moves a hand onto a gun's grip and squeezes.</summary>
        IEnumerator PickUp(Transform controller, NearFarInteractor hand, Weapon weapon)
        {
            var grab = weapon.GetComponent<XRGrabInteractable>();
            controller.position = grab.attachTransform.position;
            yield return Settle();
            Assert.That(hand.interactablesHovered.Contains(grab), Is.True, "the hand should find the " + weapon.Data.DisplayName + " on the rack");
            yield return Grip(hand);
            Assert.That(weapon.IsHeld, Is.True, weapon.Data.DisplayName + " should be in the hand");
        }

        /// <summary>Holds the gun at <paramref name="at"/> with its barrel pointing at <paramref name="target"/>.</summary>
        static void PointGun(Transform controller, Weapon weapon, Vector3 at, Vector3 target)
        {
            // The gun's grip is pitched in the hand, so the controller is turned by the same amount the other way.
            var grip = weapon.GetComponent<XRGrabInteractable>().attachTransform;
            controller.SetPositionAndRotation(at, Quaternion.LookRotation(target - at) * grip.localRotation);
        }

        [UnityTest]
        public IEnumerator Hands_CanReachEveryGunMagazineButtonAndPouch()
        {
            var grabbables = Object.FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None);
            Assert.That(grabbables.Length, Is.GreaterThan(10));
            foreach (var caster in Object.FindObjectsByType<SphereInteractionCaster>(FindObjectsSortMode.None))
            {
                foreach (var interactable in grabbables)
                {
                    foreach (var collider in interactable.colliders)
                    {
                        var reachable = (caster.physicsLayerMask.value & (1 << collider.gameObject.layer)) != 0;
                        Assert.That(reachable, Is.True, caster.transform.parent.name + " cannot reach " + interactable.name + " on layer " + LayerMask.LayerToName(collider.gameObject.layer));
                    }
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Pistol_IsPickedUpByItsGrip_FiredWithTheTrigger_AndFloatsBackToTheRack()
        {
            var pistol = m_Weapons["m1911"];
            var grab = pistol.GetComponent<XRGrabInteractable>();
            var slot = pistol.transform.parent;
            var visual = m_RightController.Find("Right Controller Visual").gameObject;

            yield return PickUp(m_RightController, m_RightHand, pistol);
            Assert.That(pistol.Handedness, Is.EqualTo(InteractorHandedness.Right));
            Assert.That(m_Grabbed, Is.EqualTo(1));
            Assert.That(visual.activeSelf, Is.False, "the controller model hides while a gun is in that hand");
            Assert.That(m_RightFinger.enabled, Is.False, "the gun hand cannot poke the panel");

            // Carry it to the firing line and point it at the nearest steel plate.
            var plate = Object.FindObjectsByType<SteelPlate>(FindObjectsSortMode.None).OrderBy(p => p.transform.position.z).First();
            var plateCentre = plate.GetComponent<Collider>().bounds.center;
            var hold = new Vector3(0.2f, 1.4f, 1.3f);
            PointGun(m_RightController, pistol, hold, plateCentre);
            yield return Settle();
            Assert.That(Vector3.Distance(grab.attachTransform.position, m_RightController.position), Is.LessThan(0.01f), "the pistol sits in the hand at its grip");
            Assert.That(Vector3.Angle(pistol.Muzzle.forward, plateCentre - pistol.Muzzle.position), Is.LessThan(1.5f), "the barrel points where the hand points");

            // The trigger goes through the hand's own input reader, the same path a real controller takes.
            m_RightHand.activateInput.QueueManualState(true, 1f);
            yield return Settle();
            Assert.That(m_Shots, Is.EqualTo(1));
            Assert.That(m_Hits.Count, Is.EqualTo(1));
            Assert.That(m_Hits[0].Type, Is.EqualTo(TargetType.SteelPlate));
            m_RightHand.activateInput.QueueManualState(false, 0f);
            yield return Frames(2);

            yield return LetGo(m_RightHand);
            Assert.That(pistol.IsHeld, Is.False);
            Assert.That(m_Released, Is.EqualTo(1));
            Assert.That(visual.activeSelf, Is.True);
            Assert.That(Vector3.Distance(pistol.transform.position, slot.position), Is.GreaterThan(0.5f), "a dropped gun hangs where it was let go at first");

            yield return new WaitForSeconds(2.8f);
            Assert.That(pistol.transform.parent, Is.EqualTo(slot));
            Assert.That(pistol.transform.localPosition.magnitude, Is.LessThan(0.001f), "after two seconds it is back on the rack");
            Assert.That(Quaternion.Angle(pistol.transform.localRotation, Quaternion.identity), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator Rifle_TakesASecondHand_AndThenAimsAlongTheLineBetweenThem()
        {
            var rifle = m_Weapons["ak74"];
            var grab = rifle.GetComponent<XRGrabInteractable>();
            yield return PickUp(m_RightController, m_RightHand, rifle);
            PointGun(m_RightController, rifle, new Vector3(0.2f, 1.35f, 1.3f), new Vector3(0.2f, 1.35f, 30f));
            yield return Settle();
            Assert.That(rifle.IsTwoHanded, Is.False);

            m_LeftController.position = grab.secondaryAttachTransform.position;
            yield return Settle();
            Assert.That(m_LeftHand.interactablesHovered.Contains(grab), Is.True, "the other hand should find the handguard");
            yield return Grip(m_LeftHand);
            Assert.That(grab.interactorsSelecting.Count, Is.EqualTo(2));
            Assert.That(rifle.IsTwoHanded, Is.True);
            Assert.That(m_Grabbed, Is.EqualTo(1), "a second hand steadies the gun; it is not a second pick-up");

            var before = rifle.Muzzle.forward;
            m_LeftController.position += Vector3.up * 0.08f;
            yield return Settle();
            Assert.That(rifle.Muzzle.forward.y, Is.GreaterThan(before.y + 0.1f), "raising the front hand raises the muzzle");

            yield return LetGo(m_LeftHand);
            Assert.That(rifle.IsTwoHanded, Is.False);
            Assert.That(rifle.IsHeld, Is.True);
        }

        [UnityTest]
        public IEnumerator Reload_AMagazineFromTheBeltPouch_PushedIntoTheWell_LoadsTheGun()
        {
            var pistol = m_Weapons["m1911"];
            var magwell = pistol.GetComponentInChildren<MagwellSocket>();
            yield return PickUp(m_RightController, m_RightHand, pistol);
            PointGun(m_RightController, pistol, new Vector3(0.2f, 1.4f, 1.3f), new Vector3(0.2f, 1.4f, 30f));
            yield return Settle();

            pistol.PressReload();
            Assert.That(magwell.Magazine, Is.Null);
            Assert.That(pistol.Rounds, Is.EqualTo(0));

            // Reach down to the belt and squeeze: a magazine for the gun in hand comes out in that hand.
            var pouch = Object.FindFirstObjectByType<HipPouch>();
            m_LeftController.position = pouch.transform.position;
            yield return Settle();
            yield return Grip(m_LeftHand);
            yield return Frames(3);
            Assert.That(m_LeftHand.hasSelection, Is.True, "the pouch should put a magazine in the hand");
            var magazine = m_LeftHand.interactablesSelected[0].transform.GetComponent<Magazine>();
            Assert.That(magazine, Is.Not.Null);
            Assert.That(magazine.Fits(pistol.Data), Is.True);
            Assert.That(magazine.Rounds, Is.EqualTo(pistol.Capacity));

            // Push it up into the grip without letting go; the well takes it out of the hand.
            yield return new WaitForSeconds(1f);
            m_LeftController.position = magwell.transform.position;
            yield return WaitFor(() => magwell.Magazine == magazine, 2f, "the magazine to seat");
            Assert.That(pistol.Rounds, Is.EqualTo(pistol.Capacity));
            yield return Frames(2);
            Assert.That(m_LeftHand.hasSelection, Is.False, "the hand no longer holds the magazine once it is in the gun");
        }

        [UnityTest]
        public IEnumerator AmmoTray_StocksForTheGunInHand_RefillsATakenSlot_AndRestocksForAnotherGun()
        {
            var pistol = m_Weapons["m1911"];
            var rifle = m_Weapons["ak74"];
            var slots = Object.FindFirstObjectByType<AmmoTray>().GetComponentsInChildren<MagazineSocket>();
            Assert.That(slots.Length, Is.EqualTo(4));
            Assert.That(slots.All(slot => slot.Magazine == null), Is.True, "the tray is empty until a gun is picked up");

            yield return PickUp(m_RightController, m_RightHand, pistol);
            yield return new WaitForSeconds(0.5f);
            Assert.That(slots.All(slot => slot.Magazine != null && slot.Magazine.Fits(pistol.Data)), Is.True, "the tray stocks pistol magazines");

            // Take one from the tray with the other hand: the slot is restocked a moment later.
            var taken = slots[0].Magazine;
            m_LeftController.position = taken.transform.position;
            yield return Settle();
            yield return Grip(m_LeftHand);
            Assert.That(m_LeftHand.hasSelection && m_LeftHand.interactablesSelected[0].transform == taken.transform, Is.True, "the hand should lift the magazine out of the tray");
            Assert.That(slots[0].Magazine, Is.Null);
            m_LeftController.position = new Vector3(-0.3f, 1.3f, -0.6f);
            yield return new WaitForSeconds(1.5f);
            Assert.That(slots[0].Magazine, Is.Not.Null);
            Assert.That(slots[0].Magazine, Is.Not.EqualTo(taken));
            yield return LetGo(m_LeftHand);

            // Swap the pistol for the rifle: the tray clears and restocks with rifle magazines.
            yield return LetGo(m_RightHand);
            yield return PickUp(m_RightController, m_RightHand, rifle);
            yield return new WaitForSeconds(0.5f);
            Assert.That(slots.All(slot => slot.Magazine != null && slot.Magazine.Fits(rifle.Data)), Is.True, "the tray restocks for the rifle");
        }

        [UnityTest]
        public IEnumerator Tutorial_WalksThroughPickUp_Plates_Reload_AndStart()
        {
            var tutorial = Object.FindFirstObjectByType<TutorialHints>();
            var text = tutorial.GetComponentInChildren<TMP_Text>(true);
            var hint = text.transform.parent.gameObject;
            var pistol = m_Weapons["m1911"];
            var magwell = pistol.GetComponentInChildren<MagwellSocket>();
            Assert.That(hint.activeSelf, Is.True);
            StringAssert.Contains("pick it up", text.text);

            yield return PickUp(m_RightController, m_RightHand, pistol);
            StringAssert.Contains("0 / 3", text.text);

            // Three steel plates at ten metres, one shot each.
            var hold = new Vector3(0.2f, 1.4f, 1.3f);
            var plates = Object.FindObjectsByType<SteelPlate>(FindObjectsSortMode.None).OrderBy(plate => plate.transform.position.z).Take(3).ToArray();
            foreach (var plate in plates)
            {
                PointGun(m_RightController, pistol, hold, plate.GetComponent<Collider>().bounds.center);
                yield return Settle();
                yield return PullTrigger(m_RightHand);
            }

            Assert.That(m_Hits.Count(hit => hit.Type == TargetType.SteelPlate), Is.EqualTo(3));
            StringAssert.Contains("runs empty", text.text);

            // Four rounds left; fire them off down the range.
            PointGun(m_RightController, pistol, hold, new Vector3(-6f, 4f, 60f));
            yield return Settle();
            for (var shot = 0; shot < 4; shot++)
                yield return PullTrigger(m_RightHand);
            Assert.That(pistol.Rounds, Is.EqualTo(0));
            StringAssert.Contains("drop the magazine", text.text);

            // Reload by hand: drop the empty one, take a fresh one from the belt, push it in, release the slide.
            pistol.PressReload();
            m_LeftController.position = Object.FindFirstObjectByType<HipPouch>().transform.position;
            yield return Settle();
            yield return Grip(m_LeftHand);
            yield return new WaitForSeconds(1f);
            m_LeftController.position = magwell.transform.position;
            yield return WaitFor(() => magwell.Magazine != null, 2f, "the magazine to seat");
            StringAssert.Contains("drop the magazine", text.text, "the hint stays until the slide is released");
            pistol.PressAction();
            StringAssert.Contains("press START", text.text);
            Assert.That(tutorial.IsDone, Is.False);

            m_Controller.StartRound();
            Assert.That(tutorial.IsDone, Is.True);
            Assert.That(hint.activeSelf, Is.False);
            Assert.That(m_Controller.Stats.Data.tutorialCompleted, Is.True);
        }

        static IEnumerator PullTrigger(NearFarInteractor hand)
        {
            hand.activateInput.QueueManualState(true, 1f);
            yield return Frames(4);
            hand.activateInput.QueueManualState(false, 0f);
            yield return new WaitForSeconds(0.6f);
        }

        [UnityTest]
        public IEnumerator StartButton_PushedWithAFingertip_StartsTheCountdown()
        {
            var button = GameObject.Find("StartButton").transform;
            var fingertip = m_RightFinger.attachTransform;
            m_RightController.rotation = button.rotation;
            var tipOffset = fingertip.position - m_RightController.position;

            // Come in along the button's axis from five centimetres out and push two centimetres past its face.
            for (var depth = -0.05f; depth <= 0.02f; depth += 0.004f)
            {
                m_RightController.position = button.position + button.forward * depth - tipOffset;
                yield return null;
                yield return null;
            }

            Assert.That(m_Controller.State, Is.EqualTo(RoundState.Countdown));
        }

        [UnityTest]
        public IEnumerator PanelButton_PokedOnTheScreen_ChangesTheSetting()
        {
            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(1.5f));
            var fast = (RectTransform)GameObject.Find("Fast").transform;
            var centre = fast.TransformPoint(fast.rect.center);
            var fingertip = m_RightFinger.attachTransform;
            m_RightController.rotation = Quaternion.LookRotation(fast.forward, fast.up);
            var tipOffset = fingertip.position - m_RightController.position;

            // Press into the screen and come back out, as a finger does.
            for (var depth = -0.05f; depth <= 0.012f; depth += 0.003f)
            {
                m_RightController.position = centre + fast.forward * depth - tipOffset;
                yield return null;
                yield return null;
            }

            for (var depth = 0.012f; depth >= -0.05f; depth -= 0.003f)
            {
                m_RightController.position = centre + fast.forward * depth - tipOffset;
                yield return null;
                yield return null;
            }

            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(RangeSettings.FastSpeed));
        }
    }
}
