using System;
using RangeControl.Core;
using RangeControl.Targets;
using RangeControl.UI;
using RangeControl.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RangeControl.Range
{
    /// <summary>
    /// The control console in the booth. It turns button and slider input into changes on
    /// <see cref="RangeSettings"/> and requests to the round controller, and redraws itself when either one
    /// reports a change. It never moves a target or touches a gun itself: the spawner, the carrier and the
    /// weapons all react to the same settings asset on their own.
    /// </summary>
    public sealed class RangeControlPanel : MonoBehaviour
    {
        public enum Tab
        {
            Targets,
            Gun,
            Round,
            Comfort,
        }

        static readonly string[] s_PatternNames = { "Static", "Left to right", "Ping-pong", "Strafe", "Pop-up" };
        static readonly string[] s_SizeNames = { "Small 0.7x", "Normal 1x", "Large 1.5x" };
        const float k_PreviewRailMetres = 8f;

        [SerializeField] RangeEvents m_Events;
        [SerializeField] RangeSettings m_Settings;
        [SerializeField] ComfortSettings m_Comfort;
        [SerializeField] GameModeController m_Controller;

        [Header("Tabs, in the order of the Tab enum")]
        [SerializeField] Button[] m_TabButtons = Array.Empty<Button>();
        [SerializeField] GameObject[] m_TabPages = Array.Empty<GameObject>();
        [SerializeField] Color m_TabActive = new Color(0.95f, 0.6f, 0.1f);
        [SerializeField] Color m_TabIdle = new Color(0.25f, 0.27f, 0.3f);

        [Header("Targets tab")]
        [SerializeField] SliderRow m_Distance;
        [SerializeField] SliderRow m_Speed;
        [SerializeField] Button m_SlowPreset;
        [SerializeField] Button m_MediumPreset;
        [SerializeField] Button m_FastPreset;
        [SerializeField] StepperRow m_Pattern;
        [SerializeField] StepperRow m_SpawnInterval;
        [SerializeField] StepperRow m_Exposure;
        [SerializeField] StepperRow m_TargetsPerRound;
        [SerializeField] StepperRow m_TargetSize;
        [SerializeField] RectTransform m_PreviewTrack;
        [SerializeField] RectTransform m_PreviewGhost;

        [Header("Gun tab")]
        [SerializeField] StepperRow m_FireRate;
        [SerializeField] StepperRow m_InfiniteAmmo;
        [SerializeField] StepperRow m_LaserSight;
        [SerializeField] TMP_Text m_GunSummary;

        [Header("Round tab")]
        [SerializeField] StepperRow m_Mode;
        [SerializeField] StepperRow m_Tier;
        [SerializeField] StepperRow m_Timer;
        [SerializeField] TMP_Text m_Goal;
        [SerializeField] Button m_NewPaper;
        [SerializeField] Button[] m_PresetLoad = Array.Empty<Button>();
        [SerializeField] Button[] m_PresetSave = Array.Empty<Button>();
        [SerializeField] TMP_Text[] m_PresetLabels = Array.Empty<TMP_Text>();

        [Header("Comfort tab")]
        [SerializeField] StepperRow m_Seated;
        [SerializeField] StepperRow m_LeftHanded;
        [SerializeField] StepperRow m_SnapTurn;
        [SerializeField] StepperRow m_QuickReload;
        [SerializeField] StepperRow m_EarProtection;
        [SerializeField] StepperRow m_MasterVolume;
        [SerializeField] StepperRow m_GunVolume;
        [SerializeField] StepperRow m_MusicVolume;
        [SerializeField] StepperRow m_Haptics;

        [Header("Frame")]
        [SerializeField] TMP_Text m_Status;
        [SerializeField] TMP_Text m_Multiplier;
        [SerializeField] GameObject m_LockBanner;
        [SerializeField] Button m_Retry;

        [Header("Physical controls")]
        [SerializeField] PhysicalButton m_StartLever;
        [SerializeField] PhysicalButton m_RecallButton;
        [SerializeField] Transform m_LeverArm;
        [SerializeField] float m_LeverOffAngle = -35f;
        [SerializeField] float m_LeverOnAngle = 35f;

        PresetStore m_Presets;
        WeaponData m_HeldWeapon;
        Tab m_Tab;
        float m_PreviewPhase;
        float m_LeverAngle;
        bool m_Bound;

        public Tab CurrentTab => m_Tab;

        void Awake()
        {
            m_Presets = new PresetStore(PresetStore.DefaultPath);
            m_Presets.Load();
            m_LeverAngle = m_LeverOffAngle;
            Bind();
        }

        void OnEnable()
        {
            m_Settings.Changed += Refresh;
            m_Comfort.Changed += Refresh;
            m_Events.RoundStateChanged += OnRoundStateChanged;
            m_Events.ModeChanged += Refresh;
            m_Events.WeaponGrabbed += OnWeaponGrabbed;
            m_Events.WeaponReleased += OnWeaponReleased;

            for (var i = 0; i < m_TabButtons.Length; i++)
            {
                var tab = (Tab)i;
                m_TabButtons[i].onClick.AddListener(() => ShowTab(tab));
            }

            m_SlowPreset.onClick.AddListener(() => m_Settings.TargetSpeed = RangeSettings.SlowSpeed);
            m_MediumPreset.onClick.AddListener(() => m_Settings.TargetSpeed = RangeSettings.MediumSpeed);
            m_FastPreset.onClick.AddListener(() => m_Settings.TargetSpeed = RangeSettings.FastSpeed);
            m_NewPaper.onClick.AddListener(m_Controller.NewPaper);
            m_Retry.onClick.AddListener(m_Controller.StartRound);
            for (var i = 0; i < PresetStore.SlotCount && i < m_PresetLoad.Length; i++)
            {
                var slot = i;
                m_PresetLoad[i].onClick.AddListener(() => LoadPreset(slot));
                m_PresetSave[i].onClick.AddListener(() => SavePreset(slot));
            }

            if (m_StartLever != null)
                m_StartLever.Pressed += m_Controller.ToggleRound;
            if (m_RecallButton != null)
                m_RecallButton.Pressed += OnRecallPressed;

            ShowTab(m_Tab);
            Refresh();
        }

        void OnDisable()
        {
            m_Settings.Changed -= Refresh;
            m_Comfort.Changed -= Refresh;
            m_Events.RoundStateChanged -= OnRoundStateChanged;
            m_Events.ModeChanged -= Refresh;
            m_Events.WeaponGrabbed -= OnWeaponGrabbed;
            m_Events.WeaponReleased -= OnWeaponReleased;

            foreach (var button in m_TabButtons)
                button.onClick.RemoveAllListeners();
            m_SlowPreset.onClick.RemoveAllListeners();
            m_MediumPreset.onClick.RemoveAllListeners();
            m_FastPreset.onClick.RemoveAllListeners();
            m_NewPaper.onClick.RemoveAllListeners();
            m_Retry.onClick.RemoveAllListeners();
            foreach (var button in m_PresetLoad)
                button.onClick.RemoveAllListeners();
            foreach (var button in m_PresetSave)
                button.onClick.RemoveAllListeners();

            if (m_StartLever != null)
                m_StartLever.Pressed -= m_Controller.ToggleRound;
            if (m_RecallButton != null)
                m_RecallButton.Pressed -= OnRecallPressed;
        }

        /// <summary>Swaps in a preset store, so tests can keep the preset file out of the real data folder.</summary>
        public void UsePresets(PresetStore presets)
        {
            m_Presets = presets;
            Refresh();
        }

        public void ShowTab(Tab tab)
        {
            m_Tab = tab;
            for (var i = 0; i < m_TabPages.Length; i++)
            {
                m_TabPages[i].SetActive(i == (int)tab);
                if (i < m_TabButtons.Length)
                    m_TabButtons[i].image.color = i == (int)tab ? m_TabActive : m_TabIdle;
            }
        }

        void Bind()
        {
            if (m_Bound)
                return;
            m_Bound = true;
            var settings = m_Settings;
            var comfort = m_Comfort;

            m_Distance.Bind("Distance", RangeSettings.MinDistance, settings.DistanceLimit, RangeSettings.DistanceStep,
                () => settings.TargetDistance, value => settings.TargetDistance = value, value => value.ToString("0") + " m");
            m_Speed.Bind("Speed", RangeSettings.MinSpeed, RangeSettings.MaxSpeed, RangeSettings.SpeedStep,
                () => settings.TargetSpeed, value => settings.TargetSpeed = value, value => value.ToString("0.0") + " m/s");

            m_Pattern.Bind("Movement", () => s_PatternNames[(int)settings.Pattern],
                step => settings.Pattern = (MovementPattern)Wrap((int)settings.Pattern + step, s_PatternNames.Length));
            m_SpawnInterval.Bind("Spawn interval", () => settings.SpawnInterval.ToString("0.0") + " s",
                step => settings.SpawnInterval += step * RangeSettings.SpawnIntervalStep);
            m_Exposure.Bind("Pop-up exposure", () => settings.PopUpExposure.ToString("0.00") + " s",
                step => settings.PopUpExposure += step * RangeSettings.PopUpExposureStep);
            m_TargetsPerRound.Bind("Targets per round",
                () => settings.TargetsPerRound == RangeSettings.Endless ? "Endless" : settings.TargetsPerRound.ToString(),
                settings.StepTargetsPerRound);
            m_TargetSize.Bind("Target size", () => s_SizeNames[(int)settings.TargetSize],
                step => settings.TargetSize = (TargetSize)Mathf.Clamp((int)settings.TargetSize + step, 0, s_SizeNames.Length - 1));

            m_FireRate.Bind("Fire rate", FormatFireRate, step => settings.FireRatePercent += step * RangeSettings.FireRateStep);
            m_InfiniteAmmo.Bind("Infinite ammo", () => OnOff(settings.InfiniteAmmo), _ => settings.InfiniteAmmo = !settings.InfiniteAmmo);
            m_LaserSight.Bind("Laser sight", () => OnOff(settings.LaserSight), _ => settings.LaserSight = !settings.LaserSight);

            m_Mode.Bind("Mode", () => m_Controller.Mode.DisplayName, m_Controller.StepMode);
            m_Tier.Bind("Tier", () => m_Controller.Tier != null ? m_Controller.Tier.name : "-", m_Controller.StepTier);
            m_Timer.Bind("Round timer", () => settings.RoundTimer == 0 ? "Off" : settings.RoundTimer + " s", settings.StepRoundTimer);

            m_Seated.Bind("Seated mode", () => OnOff(comfort.Seated), _ => comfort.Seated = !comfort.Seated);
            m_LeftHanded.Bind("Left-handed booth", () => OnOff(comfort.LeftHanded), _ => comfort.LeftHanded = !comfort.LeftHanded);
            m_SnapTurn.Bind("Snap turn 45°", () => OnOff(comfort.SnapTurn), _ => comfort.SnapTurn = !comfort.SnapTurn);
            m_QuickReload.Bind("Quick reload", () => OnOff(comfort.QuickReload), _ => comfort.QuickReload = !comfort.QuickReload);
            m_EarProtection.Bind("Ear protection", () => OnOff(comfort.EarProtection), _ => comfort.EarProtection = !comfort.EarProtection);
            m_MasterVolume.Bind("Master volume", () => Percent(comfort.MasterVolume), step => comfort.MasterVolume += step * 0.1f);
            m_GunVolume.Bind("Gunshot volume", () => Percent(comfort.GunVolume), step => comfort.GunVolume += step * 0.1f);
            m_MusicVolume.Bind("Music volume", () => Percent(comfort.MusicVolume), step => comfort.MusicVolume += step * 0.1f);
            m_Haptics.Bind("Haptic strength", () => Percent(comfort.HapticStrength), step => comfort.HapticStrength += step * 0.1f);
        }

        void Refresh()
        {
            var state = m_Controller.State;
            var busy = state == RoundState.Countdown || state == RoundState.Running;
            var editable = !m_Settings.Locked;

            m_Distance.SetRange(RangeSettings.MinDistance, m_Settings.DistanceLimit);
            m_Speed.Refresh();
            m_Pattern.Refresh();
            m_SpawnInterval.Refresh();
            m_Exposure.Refresh();
            m_TargetsPerRound.Refresh();
            m_TargetSize.Refresh();
            m_FireRate.Refresh();
            m_InfiniteAmmo.Refresh();
            m_LaserSight.Refresh();
            m_Mode.Refresh();
            m_Tier.Refresh();
            m_Timer.Refresh();
            m_Seated.Refresh();
            m_LeftHanded.Refresh();
            m_SnapTurn.Refresh();
            m_QuickReload.Refresh();
            m_EarProtection.Refresh();
            m_MasterVolume.Refresh();
            m_GunVolume.Refresh();
            m_MusicVolume.Refresh();
            m_Haptics.Refresh();

            m_Distance.SetInteractable(editable);
            m_Speed.SetInteractable(editable);
            m_SlowPreset.interactable = editable;
            m_MediumPreset.interactable = editable;
            m_FastPreset.interactable = editable;
            m_Pattern.SetInteractable(editable);
            m_SpawnInterval.SetInteractable(editable);
            m_Exposure.SetInteractable(editable);
            m_TargetsPerRound.SetInteractable(editable);
            m_TargetSize.SetInteractable(editable);
            m_FireRate.SetInteractable(editable);
            m_InfiniteAmmo.SetInteractable(editable);
            m_LaserSight.SetInteractable(editable);
            m_Timer.SetInteractable(editable);

            m_Mode.SetInteractable(!busy);
            m_Tier.SetInteractable(!busy && m_Controller.Mode.TierCount > 1);
            m_NewPaper.interactable = !(busy && m_Controller.Mode.LocksPanel);
            for (var i = 0; i < m_PresetLoad.Length; i++)
            {
                var slot = m_Presets.Get(i);
                m_PresetLoad[i].interactable = !busy && slot.used;
                m_PresetSave[i].interactable = !busy;
                if (i < m_PresetLabels.Length)
                    m_PresetLabels[i].text = slot.used ? DescribePreset(slot) : "Empty";
            }

            m_LockBanner.SetActive(m_Settings.Locked);
            m_Retry.gameObject.SetActive(state == RoundState.Results);
            m_Goal.text = m_Controller.Mode.Goal;
            m_GunSummary.text = m_HeldWeapon != null
                ? m_HeldWeapon.DisplayName + "\n" + m_HeldWeapon.Role + " · " + m_HeldWeapon.MagazineSize + " rounds"
                : "No gun in hand";
            m_Multiplier.text = "Score x" + m_Controller.Multiplier.ToString("0.00");
            m_Status.text = m_Controller.Mode.DisplayName.ToUpperInvariant() + (m_Controller.Tier != null ? " · " + m_Controller.Tier.name.ToUpperInvariant() : string.Empty) + " · " + StateLabel(state);
        }

        void Update()
        {
            AnimateLever();
            if (m_Tab == Tab.Targets)
                AnimatePreview();
        }

        void AnimateLever()
        {
            if (m_LeverArm == null)
                return;
            var state = m_Controller.State;
            var goal = state == RoundState.Countdown || state == RoundState.Running ? m_LeverOnAngle : m_LeverOffAngle;
            if (Mathf.Approximately(m_LeverAngle, goal))
                return;
            m_LeverAngle = Mathf.MoveTowards(m_LeverAngle, goal, 360f * Time.unscaledDeltaTime);
            m_LeverArm.localRotation = Quaternion.Euler(m_LeverAngle, 0f, 0f);
        }

        void AnimatePreview()
        {
            // A ghost target crosses the strip at the chosen speed, scaled to the width of a real rail.
            var width = m_PreviewTrack.rect.width - m_PreviewGhost.rect.width;
            var pattern = TargetMovers.Effective(m_Settings.Pattern, m_Settings.TargetSpeed);
            float position;
            switch (pattern)
            {
                case MovementPattern.LeftToRight:
                    m_PreviewPhase += m_Settings.TargetSpeed / k_PreviewRailMetres * Time.unscaledDeltaTime;
                    position = Mathf.Repeat(m_PreviewPhase, 1f);
                    break;
                case MovementPattern.PingPong:
                case MovementPattern.Strafe:
                    m_PreviewPhase += m_Settings.TargetSpeed / k_PreviewRailMetres * Time.unscaledDeltaTime;
                    position = Mathf.PingPong(m_PreviewPhase, 1f);
                    break;
                default:
                    position = 0.5f;
                    break;
            }

            m_PreviewGhost.anchoredPosition = new Vector2((position - 0.5f) * width, 0f);
        }

        void OnRoundStateChanged(RoundState state)
        {
            Refresh();
        }

        void OnWeaponGrabbed(Weapon weapon)
        {
            m_HeldWeapon = weapon.Data;
            Refresh();
        }

        void OnWeaponReleased(Weapon weapon)
        {
            if (m_HeldWeapon == weapon.Data)
                m_HeldWeapon = null;
            Refresh();
        }

        void OnRecallPressed()
        {
            if (m_Controller.Carrier != null)
                m_Controller.Carrier.ToggleRecall();
        }

        void SavePreset(int slot)
        {
            m_Presets.Set(slot, m_Controller.ModeIndex, m_Controller.TierIndex, m_Settings.Current);
            Refresh();
        }

        void LoadPreset(int slot)
        {
            var preset = m_Presets.Get(slot);
            if (!preset.used)
                return;
            m_Controller.SelectMode(preset.modeIndex, preset.tierIndex);
            if (!m_Controller.Mode.LocksPanel)
                m_Settings.Apply(preset.settings);
        }

        string DescribePreset(PresetStore.Slot slot)
        {
            return slot.settings.targetDistance.ToString("0") + " m · " + slot.settings.targetSpeed.ToString("0.0") + " m/s · " + s_PatternNames[(int)slot.settings.pattern];
        }

        string FormatFireRate()
        {
            var percent = m_Settings.FireRatePercent;
            if (m_HeldWeapon == null)
                return percent + "%";
            return percent + "% · " + Mathf.RoundToInt(m_HeldWeapon.RoundsPerMinute * percent / 100f) + " rpm";
        }

        static string StateLabel(RoundState state)
        {
            switch (state)
            {
                case RoundState.Countdown: return "GET READY";
                case RoundState.Running: return "LIVE";
                case RoundState.Results: return "RESULTS";
                default: return "READY";
            }
        }

        static string OnOff(bool value)
        {
            return value ? "On" : "Off";
        }

        static string Percent(float value)
        {
            return Mathf.RoundToInt(value * 100f) + "%";
        }

        static int Wrap(int value, int count)
        {
            return (value % count + count) % count;
        }
    }
}
