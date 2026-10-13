using RangeControl.Core;
using RangeControl.Range;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RangeControl.Weapons
{
    /// <summary>
    /// A gun in the hand: reads the trigger of whichever controller holds it, keeps track of fire mode and
    /// ammunition, and runs the fire sequence. What makes one gun differ from another is all in
    /// <see cref="WeaponData"/>; the shot, the recoil and the haptics are each their own component.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class Weapon : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] WeaponData m_Data;
        [SerializeField] RangeEvents m_Events;
        [SerializeField] RangeSettings m_Settings;
        [SerializeField] ComfortSettings m_Comfort;
        [SerializeField] HandButtons m_Buttons;
        [SerializeField] PlayerAnchors m_Anchors;

        [Header("Parts")]
        [SerializeField] HitscanShooter m_Shooter;
        [SerializeField] WeaponRecoil m_Recoil;
        [SerializeField] WeaponHaptics m_Haptics;
        [SerializeField] MagwellSocket m_Magwell;
        [SerializeField] AudioSource m_Audio;
        [SerializeField] ParticleSystem m_MuzzleFlash;
        [SerializeField] int m_FlashParticles = 6;
        [SerializeField] LaserSight m_Laser;

        [Header("Moving parts")]
        [SerializeField] Transform m_Trigger;
        [Tooltip("Local offset of the trigger at a full pull.")]
        [SerializeField] Vector3 m_TriggerPull = new Vector3(0f, 0f, 0.006f);
        [SerializeField] Transform m_Slide;
        [Tooltip("Local offset of the slide or bolt at the back of its travel.")]
        [SerializeField] Vector3 m_SlideTravel = new Vector3(0f, 0f, 0.03f);
        [SerializeField] float m_SlideReturnSpeed = 14f;

        [Header("Comfort")]
        [Tooltip("The muzzle flash is skipped when the muzzle is closer to the eyes than this, in metres.")]
        [SerializeField] float m_MinFlashDistance = 0.3f;

        static int s_NextShotId;

        readonly FireController m_Fire = new FireController();
        XRGrabInteractable m_Grab;
        XRBaseInputInteractor m_MainHand;
        XRBaseInputInteractor m_SupportHand;
        Magazine m_SpentShell;
        SpreadBloom m_Bloom;
        Rng m_Rng;
        Vector3 m_TriggerRest;
        Vector3 m_SlideRest;
        int m_ModeIndex;
        int m_Rounds;
        int m_ShotCount;
        bool m_SlideLocked;
        bool m_Started;
        float m_SlideAmount;
        float m_SlideShown;
        float m_ReloadTimer = -1f;
        float m_TriggerOverride = -1f;

        public WeaponData Data => m_Data;
        public FireMode FireMode => m_Data.GetFireMode(m_ModeIndex);
        public int Rounds => m_Rounds;
        public int Capacity => m_Data.MagazineSize;
        public bool SlideLocked => m_SlideLocked;
        public bool IsReloading => m_ReloadTimer >= 0f;
        public bool IsHeld => m_Grab != null && m_Grab.isSelected;
        public bool IsTwoHanded => m_SupportHand != null;
        public Transform Muzzle => m_Shooter.Muzzle;

        /// <summary>Which hand has the grip. None while the gun is on the rack.</summary>
        public InteractorHandedness Handedness => m_MainHand != null ? m_MainHand.handedness : InteractorHandedness.None;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetShotIds()
        {
            s_NextShotId = 0;
        }

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Rng = new Rng((uint)(m_Data.Id.GetHashCode() | 1));
            m_Rounds = m_Data.MagazineSize;
            if (m_Trigger != null)
                m_TriggerRest = m_Trigger.localPosition;
            if (m_Slide != null)
                m_SlideRest = m_Slide.localPosition;
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnSelectEntered);
            m_Grab.selectExited.RemoveListener(OnSelectExited);
        }

        void Start()
        {
            // Every gun comes off the rack with a full magazine already in it.
            if (m_Magwell != null && !m_Data.ShellFed && m_Data.MagazinePrefab != null)
            {
                var attach = m_Magwell.attachTransform != null ? m_Magwell.attachTransform : m_Magwell.transform;
                var magazine = Instantiate(m_Data.MagazinePrefab, attach.position, attach.rotation);
                magazine.Prepare(m_Data.MagazineSize, null);
                m_Magwell.Insert(magazine);
            }

            m_Started = true;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Recoil.Tick(deltaTime);
            m_Haptics.Tick(deltaTime);
            m_Bloom.Recover(deltaTime, m_Data.MaxSpreadBloom, m_Data.SpreadRecovery);
            TickReload(deltaTime);
            ConsumeSpentShell();

            if (m_MainHand != null && m_Buttons != null)
            {
                if (m_Buttons.SecondaryPressed(m_MainHand.handedness))
                    PressReload();
                if (m_Buttons.PrimaryPressed(m_MainHand.handedness))
                    PressAction();
            }

            var trigger = ReadTrigger();
            var fireRate = m_Data.RoundsPerMinute * (m_Settings != null ? m_Settings.FireRatePercent / 100f : 1f);
            if (m_Fire.Tick(trigger, deltaTime, FireMode, fireRate))
                TryFire();

            AnimateParts(trigger, deltaTime);
            if (m_Laser != null)
                m_Laser.Show(IsHeld && m_Settings != null && m_Settings.LaserSight);
        }

        /// <summary>Drives the trigger from code instead of a controller. Pass a negative value to hand it back.</summary>
        public void SetTriggerOverride(float value)
        {
            m_TriggerOverride = value;
        }

        /// <summary>
        /// The reload button (B or Y). With quick reload on, the gun refills itself after a short delay;
        /// otherwise it drops the magazine so a fresh one can be pushed in.
        /// </summary>
        public void PressReload()
        {
            if (IsReloading)
                return;

            var quick = m_Magwell == null || (m_Comfort != null && m_Comfort.QuickReload);
            if (quick)
            {
                if (m_Rounds >= m_Data.MagazineSize && !m_SlideLocked)
                    return;
                m_ReloadTimer = m_Data.QuickReloadSeconds;
                m_Fire.Interrupt();
                Play(m_Data.MagazineOutClip);
                m_Events.RaiseWeaponStateChanged(this);
            }
            else if (!m_Data.ShellFed)
            {
                m_Magwell.Eject();
            }
        }

        /// <summary>
        /// The action button (A or X): releases a locked slide once a loaded magazine is in, and otherwise
        /// moves the fire selector on guns that have one.
        /// </summary>
        public void PressAction()
        {
            if (m_SlideLocked)
            {
                if (m_Rounds <= 0)
                    return;
                m_SlideLocked = false;
                Play(m_Data.SlideClip);
                m_Events.RaiseWeaponStateChanged(this);
            }
            else if (m_Data.FireModeCount > 1)
            {
                m_ModeIndex = (m_ModeIndex + 1) % m_Data.FireModeCount;
                m_Fire.Interrupt();
                Play(m_Data.DryFireClip);
                m_Events.RaiseWeaponStateChanged(this);
            }
        }

        /// <summary>Whether the magazine well should take this magazine right now.</summary>
        public bool CanAccept(Magazine magazine)
        {
            return !m_Data.ShellFed || m_Rounds < m_Data.MagazineSize || (m_Magwell != null && m_Magwell.Magazine == magazine);
        }

        public void OnMagazineInserted(Magazine magazine)
        {
            if (m_Data.ShellFed)
            {
                // A shell is used up as it goes in: it adds its round and goes back to its pool next frame.
                m_Rounds = Mathf.Min(m_Data.MagazineSize, m_Rounds + Mathf.Max(1, magazine.Rounds));
                m_SpentShell = magazine;
            }
            else
            {
                m_Rounds = magazine.Rounds;
            }

            if (!m_Started)
                return;
            Play(m_Data.MagazineInClip);
            m_Events.RaiseWeaponStateChanged(this);
        }

        public void OnMagazineRemoved(Magazine magazine)
        {
            if (m_Data.ShellFed)
                return;

            magazine.Rounds = m_Rounds;
            m_Rounds = 0;
            m_Fire.Interrupt();
            Play(m_Data.MagazineOutClip);
            m_Events.RaiseWeaponStateChanged(this);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            var wasHeld = m_MainHand != null;
            RefreshHands();
            if (wasHeld || m_MainHand == null)
                return;

            if (m_Settings != null)
                m_Settings.LongRange = m_Data.LongRange;
            m_Events.RaiseWeaponGrabbed(this);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            var wasHeld = m_MainHand != null;
            RefreshHands();
            if (!wasHeld || m_MainHand != null)
                return;

            m_Fire.Interrupt();
            if (m_Settings != null && m_Data.LongRange)
                m_Settings.LongRange = false;
            m_Events.RaiseWeaponReleased(this);
        }

        void RefreshHands()
        {
            // The first hand to take the gun is the gun hand; a second one steadies it.
            var selecting = m_Grab.interactorsSelecting;
            m_MainHand = selecting.Count > 0 ? selecting[0] as XRBaseInputInteractor : null;
            m_SupportHand = selecting.Count > 1 ? selecting[1] as XRBaseInputInteractor : null;
        }

        float ReadTrigger()
        {
            if (m_TriggerOverride >= 0f)
                return m_TriggerOverride;
            return m_MainHand != null ? m_MainHand.activateInput.ReadValue() : 0f;
        }

        void TryFire()
        {
            if (IsReloading)
                return;

            if (m_SlideLocked || m_Rounds <= 0)
            {
                // One click per pull, then nothing until the trigger is let go.
                m_Fire.Interrupt();
                Play(m_Data.DryFireClip);
                m_Events.RaiseWeaponDryFired(this);
                return;
            }

            if (m_Settings == null || !m_Settings.InfiniteAmmo)
                m_Rounds--;

            var shotId = ++s_NextShotId;
            var muzzle = m_Shooter.Muzzle;
            var shot = new ShotInfo(shotId, m_Data, muzzle.position, muzzle.forward);
            var showTracer = m_ShotCount % m_Data.TracerEvery == 0;
            m_ShotCount++;

            m_Shooter.Fire(m_Data, shotId, m_Data.BaseSpread + m_Bloom.Value, showTracer);
            m_Bloom.AddShot(m_Data.SpreadPerShot, m_Data.MaxSpreadBloom);
            m_Recoil.Kick(m_Data, IsTwoHanded ? m_Data.TwoHandRecoilScale : 1f);
            m_Haptics.Play(m_Data, m_MainHand, m_SupportHand, m_Comfort != null ? m_Comfort.HapticStrength : 1f);
            PlayShot();
            if (m_MuzzleFlash != null && !FlashTooCloseToEyes(shot.Origin))
                m_MuzzleFlash.Emit(m_FlashParticles);
            m_SlideAmount = 1f;

            if (m_Rounds == 0 && m_Data.SlideLocksOnEmpty)
                m_SlideLocked = true;

            // Raised after the pellets have landed, so scoring sees the hits before it counts the shot.
            m_Events.RaiseShotFired(shot);
            m_Events.RaiseWeaponStateChanged(this);
        }

        bool FlashTooCloseToEyes(Vector3 muzzle)
        {
            var head = m_Anchors != null ? m_Anchors.Head : null;
            return head != null && (head.position - muzzle).sqrMagnitude < m_MinFlashDistance * m_MinFlashDistance;
        }

        void TickReload(float deltaTime)
        {
            if (m_ReloadTimer < 0f)
                return;
            m_ReloadTimer -= deltaTime;
            if (m_ReloadTimer > 0f)
                return;

            m_ReloadTimer = -1f;
            m_Rounds = m_Data.MagazineSize;
            m_SlideLocked = false;
            if (m_Magwell != null && m_Magwell.Magazine != null)
                m_Magwell.Magazine.Rounds = m_Rounds;
            Play(m_Data.MagazineInClip);
            m_Events.RaiseWeaponStateChanged(this);
        }

        void ConsumeSpentShell()
        {
            if (m_SpentShell == null)
                return;
            var shell = m_SpentShell;
            m_SpentShell = null;
            shell.Despawn();
        }

        void AnimateParts(float trigger, float deltaTime)
        {
            if (m_Trigger != null)
                m_Trigger.localPosition = m_TriggerRest + m_TriggerPull * Mathf.Clamp01(trigger);

            if (m_Slide == null)
                return;
            // A shot throws the slide back; it runs forward again unless the gun is empty and it locks open.
            m_SlideAmount = Mathf.MoveTowards(m_SlideAmount, m_SlideLocked ? 1f : 0f, m_SlideReturnSpeed * deltaTime);
            if (Mathf.Approximately(m_SlideAmount, m_SlideShown))
                return;
            m_SlideShown = m_SlideAmount;
            m_Slide.localPosition = m_SlideRest + m_SlideTravel * m_SlideAmount;
        }

        void PlayShot()
        {
            if (m_Audio == null || m_Data.ShotClipCount == 0)
                return;
            var clip = m_Data.GetShotClip((int)(m_Rng.NextUInt() % (uint)m_Data.ShotClipCount));
            m_Audio.pitch = 1f + m_Rng.Range(-m_Data.PitchJitter, m_Data.PitchJitter);
            m_Audio.PlayOneShot(clip, m_Comfort != null ? m_Comfort.GunVolume : 1f);
        }

        void Play(AudioClip clip)
        {
            if (m_Audio == null || clip == null)
                return;
            m_Audio.pitch = 1f;
            m_Audio.PlayOneShot(clip);
        }
    }
}
