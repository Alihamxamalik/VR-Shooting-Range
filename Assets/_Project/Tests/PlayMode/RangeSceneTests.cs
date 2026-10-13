using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RangeControl.Core;
using RangeControl.Range;
using RangeControl.Targets;
using RangeControl.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RangeControl.Tests
{
    /// <summary>
    /// End-to-end checks on the real range scene, with no headset: a gun is aimed and its trigger pulled from
    /// code, and everything downstream of that is the shipping game.
    /// </summary>
    public class RangeSceneTests : RangeSceneFixture
    {
        [UnityTest]
        public IEnumerator Scene_StartsCleanWithFiveLoadedGuns()
        {
            yield return new WaitForSeconds(0.5f);

            Assert.That(m_Controller.State, Is.EqualTo(RoundState.Idle));
            Assert.That(m_Controller.Mode.Mode, Is.EqualTo(GameMode.FreePractice));
            Assert.That(m_Weapons.Keys, Is.EquivalentTo(new[] { "m1911", "uzi", "ak74", "benelli_m4", "m107" }));
            foreach (var weapon in m_Weapons.Values)
            {
                Assert.That(weapon.Rounds, Is.EqualTo(weapon.Capacity), weapon.Data.DisplayName + " should start full");
                if (!weapon.Data.ShellFed)
                    Assert.That(weapon.GetComponentInChildren<MagwellSocket>().Magazine, Is.Not.Null, weapon.Data.DisplayName + " should start with a magazine in it");
            }

            Assert.That(Object.FindObjectsByType<SteelPlate>(FindObjectsSortMode.None).Length, Is.EqualTo(6));
            Assert.That(m_Settings.Locked, Is.False);
        }

        [UnityTest]
        public IEnumerator Pistol_HitsSteelPlate_AndThePlateSwingsThenResets()
        {
            var pistol = m_Weapons["m1911"];
            var plate = Object.FindObjectsByType<SteelPlate>(FindObjectsSortMode.None).OrderBy(p => p.transform.position.z).First();
            var plateCentre = plate.GetComponent<Collider>().bounds.center;

            Aim(pistol, s_FiringPoint, plateCentre);
            yield return Fire(pistol, 0.1f);

            Assert.That(m_Shots, Is.EqualTo(1));
            Assert.That(pistol.Rounds, Is.EqualTo(6));
            Assert.That(m_Hits.Count, Is.EqualTo(1));
            Assert.That(m_Hits[0].Type, Is.EqualTo(TargetType.SteelPlate));
            Assert.That(m_Hits[0].BasePoints, Is.EqualTo(ScoreRules.SteelPlate));
            Assert.That(m_Hits[0].Distance, Is.EqualTo(Vector3.Distance(s_FiringPoint, plateCentre)).Within(0.2f));
            Assert.That(m_Awards[0].Counted, Is.False, "a hit outside a round shows its value but is not counted");
            Assert.That(plate.IsReady, Is.False);

            yield return WaitFor(() => plate.IsReady, 4f, "the plate to settle");
        }

        [UnityTest]
        public IEnumerator Pistol_RunsDry_LocksItsSlide_AndReloadsByHand()
        {
            var pistol = m_Weapons["m1911"];
            var magwell = pistol.GetComponentInChildren<MagwellSocket>();
            Aim(pistol, s_FiringPoint, new Vector3(0f, 1.5f, 60f));

            for (var shot = 0; shot < 7; shot++)
                yield return Fire(pistol, 0.25f);
            Assert.That(pistol.Rounds, Is.EqualTo(0));
            Assert.That(pistol.SlideLocked, Is.True);

            yield return Fire(pistol, 0.25f);
            Assert.That(m_Shots, Is.EqualTo(7));
            Assert.That(m_DryFires, Is.EqualTo(1));

            // B or Y: the empty magazine drops out, and stays out.
            var spent = magwell.Magazine;
            var spentHeight = spent.transform.position.y;
            pistol.PressReload();
            Assert.That(magwell.Magazine, Is.Null);
            Assert.That(spent.Rounds, Is.EqualTo(0));
            yield return new WaitForSeconds(1.2f);
            Assert.That(magwell.Magazine, Is.Null, "the well must not take back the magazine it just dropped");
            Assert.That(spent.transform.position.y, Is.LessThan(spentHeight - 0.3f), "the ejected magazine falls away");
            Assert.That(pistol.Rounds, Is.EqualTo(0));

            // A fresh magazine is pushed into the well and the socket takes it by itself.
            var supply = Object.FindFirstObjectByType<AmmoSupply>();
            var fresh = supply.Spawn(pistol.Data, magwell.transform.position, magwell.transform.rotation);
            yield return WaitFor(() => magwell.Magazine == fresh, 2f, "the fresh magazine to seat");
            Assert.That(pistol.Rounds, Is.EqualTo(7));
            Assert.That(pistol.SlideLocked, Is.True, "the slide stays back until it is released");

            // A or X releases the slide.
            pistol.PressAction();
            Assert.That(pistol.SlideLocked, Is.False);
            yield return Fire(pistol, 0.25f);
            Assert.That(m_Shots, Is.EqualTo(8));
            Assert.That(pistol.Rounds, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator QuickReload_RefillsTheGunAfterItsDelay()
        {
            m_Comfort.QuickReload = true;
            var pistol = m_Weapons["m1911"];
            Aim(pistol, s_FiringPoint, new Vector3(0f, 1.5f, 60f));
            for (var shot = 0; shot < 3; shot++)
                yield return Fire(pistol, 0.25f);
            Assert.That(pistol.Rounds, Is.EqualTo(4));

            pistol.PressReload();
            Assert.That(pistol.IsReloading, Is.True);
            yield return Fire(pistol, 0.1f);
            Assert.That(m_Shots, Is.EqualTo(3), "the gun cannot fire while it reloads");

            yield return WaitFor(() => !pistol.IsReloading, 2f, "the reload to finish");
            Assert.That(pistol.Rounds, Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator Uzi_HeldTrigger_FiresAtItsRateWithATracerEveryThirdShot()
        {
            var uzi = m_Weapons["uzi"];
            Aim(uzi, s_FiringPoint, new Vector3(0f, 1.5f, 60f));

            uzi.SetTriggerOverride(1f);
            yield return new WaitForSeconds(1f);
            uzi.SetTriggerOverride(0f);
            yield return null;
            yield return null;

            // 600 rounds a minute is ten a second; allow a shot either way for where the frames fell.
            Assert.That(m_Shots, Is.InRange(9, 12));
            Assert.That(uzi.Rounds, Is.EqualTo(uzi.Capacity - m_Shots));
            Assert.That(m_Tracers, Is.EqualTo((m_Shots + 2) / 3));
        }

        [UnityTest]
        public IEnumerator Ak74_SelectorSwitchesBetweenSingleShotsAndAutomatic()
        {
            var rifle = m_Weapons["ak74"];
            Aim(rifle, s_FiringPoint, new Vector3(0f, 1.5f, 60f));
            Assert.That(rifle.FireMode, Is.EqualTo(FireMode.Semi));

            rifle.SetTriggerOverride(1f);
            yield return new WaitForSeconds(0.5f);
            rifle.SetTriggerOverride(0f);
            yield return null;
            yield return null;
            Assert.That(m_Shots, Is.EqualTo(1), "single shots: one per pull however long it is held");

            rifle.PressAction();
            Assert.That(rifle.FireMode, Is.EqualTo(FireMode.Auto));
            rifle.SetTriggerOverride(1f);
            yield return new WaitForSeconds(0.5f);
            rifle.SetTriggerOverride(0f);
            yield return null;
            yield return null;
            Assert.That(m_Shots, Is.GreaterThan(4));
        }

        [UnityTest]
        public IEnumerator Shotgun_FiresEightPelletsPerShell()
        {
            var shotgun = m_Weapons["benelli_m4"];
            Aim(shotgun, s_FiringPoint, new Vector3(0f, 1.5f, 60f));
            yield return Fire(shotgun, 0.1f);

            Assert.That(m_Shots, Is.EqualTo(1));
            Assert.That(m_Traces, Is.EqualTo(8));
            Assert.That(shotgun.Rounds, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator Shotgun_TakesShellsOneAtATime_UntilItIsFull()
        {
            var shotgun = m_Weapons["benelli_m4"];
            var port = shotgun.GetComponentInChildren<MagwellSocket>();
            var supply = Object.FindFirstObjectByType<AmmoSupply>();
            Aim(shotgun, s_FiringPoint, new Vector3(0f, 1.5f, 60f));
            yield return Fire(shotgun, 0.4f);
            yield return Fire(shotgun, 0.4f);
            Assert.That(shotgun.Rounds, Is.EqualTo(5));

            for (var expected = 6; expected <= 7; expected++)
            {
                var shell = supply.Spawn(shotgun.Data, port.transform.position, port.transform.rotation);
                var loaded = expected;
                yield return WaitFor(() => shotgun.Rounds == loaded, 2f, "shell " + loaded + " to go in");
                yield return Frames(3);
                Assert.That(shell.gameObject.activeSelf, Is.False, "a shell is used up as it goes in");
            }

            // A full tube takes no more: the next shell stays in the world and falls away.
            var extra = supply.Spawn(shotgun.Data, port.transform.position, port.transform.rotation);
            yield return new WaitForSeconds(0.5f);
            Assert.That(shotgun.Rounds, Is.EqualTo(7));
            Assert.That(extra.gameObject.activeSelf, Is.True);
            Assert.That(extra.transform.position.y, Is.LessThan(port.transform.position.y - 0.2f));
        }

        [UnityTest]
        public IEnumerator MovingGallery_RunsItsSixtySeconds_ThenShowsResults()
        {
            m_Controller.SelectMode(1, 2);
            m_Controller.StartRound();

            // Twenty times speed: the minute passes in about three seconds of test time.
            Time.timeScale = 20f;
            yield return WaitFor(() => m_Controller.State == RoundState.Results, 75f, "the round to time out");
            Time.timeScale = 1f;

            Assert.That(m_Results.Count, Is.EqualTo(1));
            var result = m_Results[0];
            Assert.That(result.duration, Is.EqualTo(60f).Within(1f));
            Assert.That(result.boardKey, Is.EqualTo("MovingGallery/Fast"));
            Assert.That(result.shots, Is.EqualTo(0));
            Assert.That(result.targetsMissed, Is.GreaterThan(10), "targets that crossed unhit are counted as missed");
            Assert.That(result.isPersonalBest, Is.False, "a round with no shots is not a best");
            Assert.That(result.hasMedals, Is.True);
            Assert.That(m_Settings.Locked, Is.True);

            yield return new WaitForSeconds(0.3f);
            Assert.That(Object.FindObjectsByType<FlipTarget>(FindObjectsSortMode.None).Any(candidate => candidate.isActiveAndEnabled), Is.False);
        }

        [UnityTest]
        public IEnumerator ComfortOptions_ReshapeTheBoothAndTheSound()
        {
            var rack = GameObject.Find("WeaponRack").transform;
            var panel = GameObject.Find("ControlPanel").transform;
            var rig = GameObject.Find("XR Origin").transform;
            var muffle = Camera.main.GetComponent<AudioLowPassFilter>();
            var rackX = rack.position.x;
            var panelX = panel.position.x;
            var panelFacing = panel.forward;
            var rigHeight = rig.position.y;
            Assert.That(rackX, Is.LessThan(0f), "the rack starts on the left");
            Assert.That(panelX, Is.GreaterThan(0f), "the panel starts on the right");

            m_Comfort.LeftHanded = true;
            yield return null;
            Assert.That(rack.position.x, Is.EqualTo(-rackX).Within(1e-4f));
            Assert.That(panel.position.x, Is.EqualTo(-panelX).Within(1e-4f));
            Assert.That(panel.forward.x, Is.EqualTo(-panelFacing.x).Within(1e-3f), "the panel still faces the player from the other side");
            Assert.That(panel.forward.z, Is.EqualTo(panelFacing.z).Within(1e-3f));
            m_Comfort.LeftHanded = false;
            yield return null;
            Assert.That(rack.position.x, Is.EqualTo(rackX).Within(1e-4f));
            Assert.That(panel.position.x, Is.EqualTo(panelX).Within(1e-4f));

            m_Comfort.Seated = true;
            Assert.That(rig.position.y, Is.EqualTo(rigHeight + 0.4f).Within(1e-4f));
            m_Comfort.Seated = false;
            Assert.That(rig.position.y, Is.EqualTo(rigHeight).Within(1e-4f));

            Assert.That(muffle.enabled, Is.False);
            m_Comfort.EarProtection = true;
            Assert.That(muffle.enabled, Is.True);
            Assert.That(AudioListener.volume, Is.LessThan(1f));
            m_Comfort.EarProtection = false;
            Assert.That(muffle.enabled, Is.False);

            m_Comfort.MasterVolume = 0.5f;
            Assert.That(AudioListener.volume, Is.EqualTo(0.5f).Within(1e-4f));
            m_Comfort.MasterVolume = 1f;
            Assert.That(AudioListener.volume, Is.EqualTo(1f).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator PanelPreset_SavesAFullSetup_AndLoadsItBack()
        {
            var panel = Object.FindFirstObjectByType<RangeControlPanel>();
            var presetPath = Path.Combine(m_StatsFolder, PresetStore.FileName);
            panel.UsePresets(new PresetStore(presetPath));
            panel.ShowTab(RangeControlPanel.Tab.Round);
            yield return null;

            m_Settings.TargetDistance = 40f;
            m_Settings.Pattern = MovementPattern.PingPong;
            m_Settings.InfiniteAmmo = true;
            GameObject.Find("PresetSave0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(File.Exists(presetPath), Is.True);

            m_Settings.TargetDistance = 10f;
            m_Settings.Pattern = MovementPattern.Static;
            m_Settings.InfiniteAmmo = false;
            yield return null;
            GameObject.Find("PresetLoad0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

            Assert.That(m_Settings.TargetDistance, Is.EqualTo(40f));
            Assert.That(m_Settings.Pattern, Is.EqualTo(MovementPattern.PingPong));
            Assert.That(m_Settings.InfiniteAmmo, Is.True);
        }

        [UnityTest]
        public IEnumerator FreePractice_Round_SpawnsTargets_ScoresHits_AndSavesStats()
        {
            var pistol = m_Weapons["m1911"];
            m_Controller.StartRound();
            Assert.That(m_Controller.State, Is.EqualTo(RoundState.Countdown));
            yield return WaitFor(() => m_Controller.State == RoundState.Running, 5f, "the countdown to finish");

            FlipTarget target = null;
            yield return WaitFor(() =>
            {
                target = Object.FindObjectsByType<FlipTarget>(FindObjectsSortMode.None).FirstOrDefault(candidate => candidate.IsActive);
                return target != null;
            }, 5f, "a target to stand up");

            // Aim at the middle of the board, 1.2 m above the hinge, and fire while it is still there.
            Aim(pistol, s_FiringPoint, target.transform.position + Vector3.up * 1.2f);
            yield return Fire(pistol, 0.1f);

            Assert.That(m_Hits.Count, Is.EqualTo(1));
            Assert.That(m_Hits[0].Type, Is.EqualTo(TargetType.MovingSilhouette));
            // Free Practice multiplies by the live difficulty factor: 15 m at 1.5 m/s is x1.375.
            Assert.That(m_Controller.Score.Score, Is.EqualTo(Mathf.RoundToInt(ScoreRules.SilhouetteBody * 1.375f)));
            Assert.That(m_Awards[0].Counted, Is.True);
            Assert.That(target.IsActive, Is.False, "a hit target flips down");

            m_Controller.StopRound();
            Assert.That(m_Controller.State, Is.EqualTo(RoundState.Results));
            Assert.That(m_Results.Count, Is.EqualTo(1));
            Assert.That(m_Results[0].score, Is.EqualTo(m_Controller.Score.Score));
            Assert.That(m_Results[0].shots, Is.EqualTo(1));
            Assert.That(m_Results[0].isPersonalBest, Is.True);
            Assert.That(m_Results[0].weaponId, Is.EqualTo("m1911"));
            Assert.That(File.Exists(Path.Combine(m_StatsFolder, StatsService.FileName)), Is.True);

            yield return new WaitForSeconds(0.6f);
            Assert.That(Object.FindObjectsByType<FlipTarget>(FindObjectsSortMode.None).Any(candidate => candidate.isActiveAndEnabled), Is.False, "stopping the round clears the rails");
        }

        [UnityTest]
        public IEnumerator Precision_TenShotsAtPaper_EndsTheRoundWithARingScoreAndAGroup()
        {
            // The AK74 holds thirty rounds, so the ten-shot string needs no reload.
            var rifle = m_Weapons["ak74"];
            m_Controller.SelectMode(2, 0);
            Assert.That(m_Controller.Mode.Mode, Is.EqualTo(GameMode.Precision));
            Assert.That(m_Settings.Locked, Is.True);
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(10f));

            m_Controller.StartRound();
            yield return WaitFor(() => m_Controller.State == RoundState.Running, 5f, "the countdown to finish");
            var carrier = m_Controller.Carrier;
            yield return WaitFor(() => !carrier.IsMoving && Mathf.Abs(carrier.Distance - 10f) < 0.01f, 6f, "the carrier to reach 10 m");

            var bullseye = carrier.Paper.transform.position;
            for (var shot = 0; shot < 10; shot++)
            {
                Aim(rifle, new Vector3(0f, 1.5f, 1.2f), bullseye);
                yield return Fire(rifle);
            }

            Assert.That(m_Controller.State, Is.EqualTo(RoundState.Results), "the tenth shot ends the string");
            Assert.That(m_Results.Count, Is.EqualTo(1));
            var result = m_Results[0];
            Assert.That(result.shots, Is.EqualTo(10));
            // The rifle's 0.6 degree cone is at most 4.6 cm off at 8.8 m, which is the 9 ring at worst.
            Assert.That(result.score, Is.InRange(90, 100));
            Assert.That(result.groupSizeCm, Is.GreaterThan(0f).And.LessThan(10f));
            Assert.That(carrier.Paper.HoleCount, Is.EqualTo(10));
            Assert.That(result.boardKey, Is.EqualTo("Precision/10 m"));
            Assert.That(result.medal, Is.Not.EqualTo(Medal.None));
        }

        [UnityTest]
        public IEnumerator ChallengeMode_LocksThePanel_AndFreePracticeGetsItsOwnSetupBack()
        {
            m_Settings.TargetDistance = 35f;
            m_Settings.Pattern = MovementPattern.Strafe;

            m_Controller.SelectMode(1, 2);
            Assert.That(m_Controller.Mode.Mode, Is.EqualTo(GameMode.MovingGallery));
            Assert.That(m_Controller.Tier.name, Is.EqualTo("Fast"));
            Assert.That(m_Settings.Locked, Is.True);
            Assert.That(m_Settings.TargetSpeed, Is.EqualTo(RangeSettings.FastSpeed));
            Assert.That(m_Controller.Multiplier, Is.EqualTo(2f));

            m_Settings.TargetDistance = 5f;
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(15f), "the panel cannot change a locked setting");

            m_Controller.SelectMode(0, 0);
            Assert.That(m_Settings.Locked, Is.False);
            Assert.That(m_Settings.TargetDistance, Is.EqualTo(35f));
            Assert.That(m_Settings.Pattern, Is.EqualTo(MovementPattern.Strafe));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DistanceChange_DrivesTheCarrierAndTheRails_AndRecallBringsThePaperBack()
        {
            var carrier = m_Controller.Carrier;
            var rail = Object.FindObjectsByType<TargetRail>(FindObjectsSortMode.None).OrderBy(r => r.DepthOffset).First();
            Assert.That(carrier.Distance, Is.EqualTo(15f).Within(0.01f));

            m_Settings.TargetDistance = 30f;
            yield return new WaitForSeconds(0.5f);
            Assert.That(carrier.Distance, Is.GreaterThan(15.5f), "the change shows on the range within a second");

            yield return WaitFor(() => Mathf.Abs(carrier.Distance - 30f) < 0.01f, 6f, "the carrier to reach 30 m");
            Assert.That(rail.transform.localPosition.z, Is.EqualTo(30f + rail.DepthOffset).Within(0.01f));

            carrier.Recall();
            yield return WaitFor(() => carrier.Distance < 1.6f && !carrier.IsMoving, 8f, "the paper to come back to the booth");
            carrier.Send();
            yield return WaitFor(() => Mathf.Abs(carrier.Distance - 30f) < 0.01f, 8f, "the paper to go back out");
        }

        [UnityTest]
        public IEnumerator Scope_RendersOnlyWhileTheEyeIsAtTheLens()
        {
            var rifle = m_Weapons["m107"];
            var scope = rifle.GetComponentInChildren<ScopeView>();
            var head = Camera.main.transform;
            yield return null;
            Assert.That(scope.IsLive, Is.False, "on the rack the scope camera is off");

            // Zeroed at 75 m: there the line of sight and the line of the barrel are within a centimetre.
            var sight = scope.GetComponentInChildren<Camera>(true).transform;
            var shotAtZero = rifle.Muzzle.position + rifle.Muzzle.forward * 75f;
            var sightAtZero = sight.position + sight.forward * Vector3.Distance(sight.position, shotAtZero);
            Assert.That(Vector3.Distance(sightAtZero, shotAtZero), Is.LessThan(0.01f));

            // Bring the rifle up so the eyepiece is ten centimetres in front of the eye.
            var lens = scope.transform.Find("Lens");
            rifle.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            rifle.transform.position += head.position + head.forward * 0.1f - lens.position;
            yield return null;
            yield return null;
            Assert.That(scope.IsLive, Is.True);

            rifle.transform.position += Vector3.forward * 1f;
            yield return null;
            yield return null;
            Assert.That(scope.IsLive, Is.False);
        }
    }
}
