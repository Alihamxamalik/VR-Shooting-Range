using System;
using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Weapons
{
    /// <summary>
    /// Every number, sound and prefab that makes one gun different from another. A new gun is a new asset
    /// of this type plus a model; no weapon code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Range Control/Weapon Data", fileName = "WeaponData")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key used in saved stats. Do not change it once players have stats.")]
        [SerializeField] string m_Id = "weapon";
        [SerializeField] string m_DisplayName = "Weapon";
        [SerializeField] string m_Role = "Pistol";

        [Header("Fire")]
        [Tooltip("The first entry is the mode the gun starts in. Two entries give it a selector.")]
        [SerializeField] FireMode[] m_FireModes = { FireMode.Semi };
        [Tooltip("Rate of automatic fire, and the fastest a semi-automatic can be fired.")]
        [SerializeField] float m_RoundsPerMinute = 400f;
        [SerializeField, Min(1)] int m_Pellets = 1;
        [Tooltip("Full angle of the cone shots land in, in degrees.")]
        [SerializeField] float m_BaseSpread = 0.3f;
        [Tooltip("Degrees added to the cone by each shot of a burst.")]
        [SerializeField] float m_SpreadPerShot;
        [Tooltip("Most the cone can grow by, in degrees.")]
        [SerializeField] float m_MaxSpreadBloom;
        [Tooltip("Seconds for a fully bloomed cone to settle back.")]
        [SerializeField] float m_SpreadRecovery = 0.4f;
        [SerializeField] float m_MaxRange = 300f;
        [Tooltip("Draw a tracer on every n-th shot.")]
        [SerializeField, Min(1)] int m_TracerEvery = 1;

        [Header("Ammunition")]
        [SerializeField, Min(1)] int m_MagazineSize = 7;
        [Tooltip("Loaded one shell at a time through a port instead of with a magazine.")]
        [SerializeField] bool m_ShellFed;
        [SerializeField] bool m_SlideLocksOnEmpty;
        [SerializeField] float m_QuickReloadSeconds = 1.2f;
        [Tooltip("The magazine, or the single shell, this gun takes.")]
        [SerializeField] Magazine m_MagazinePrefab;

        [Header("Recoil")]
        [Tooltip("Metres the gun kicks straight back per shot.")]
        [SerializeField] float m_KickBack = 0.03f;
        [Tooltip("Degrees the muzzle climbs per shot.")]
        [SerializeField] float m_MuzzleClimb = 6f;
        [Tooltip("Most the muzzle wanders sideways per shot, in degrees either way.")]
        [SerializeField] float m_YawJitter = 2f;
        [Tooltip("How quickly the gun settles back. Higher is snappier.")]
        [SerializeField] float m_RecoilReturnSpeed = 8f;
        [Tooltip("Recoil left when a second hand is on the gun. 0.6 is a 40% reduction.")]
        [SerializeField, Range(0f, 1f)] float m_TwoHandRecoilScale = 0.6f;

        [Header("Haptics")]
        [SerializeField, Range(0f, 1f)] float m_HapticAmplitude = 0.95f;
        [SerializeField] float m_HapticDuration = 0.045f;
        [SerializeField, Range(0f, 1f)] float m_HapticTailAmplitude = 0.35f;
        [SerializeField] float m_HapticTailDuration = 0.05f;
        [Tooltip("Strength of the pulse in the supporting hand, as a share of the gun hand's.")]
        [SerializeField, Range(0f, 1f)] float m_SupportHandHaptics = 0.6f;

        [Header("Handling")]
        [Tooltip("Lets the control panel send targets out to 100 m while this gun is held.")]
        [SerializeField] bool m_LongRange;

        [Header("Audio")]
        [SerializeField] AudioClip[] m_ShotClips = Array.Empty<AudioClip>();
        [Tooltip("Random pitch change either way per shot. 0.05 is 5%.")]
        [SerializeField, Range(0f, 0.2f)] float m_PitchJitter = 0.05f;
        [SerializeField] AudioClip m_DryFireClip;
        [SerializeField] AudioClip m_MagazineOutClip;
        [SerializeField] AudioClip m_MagazineInClip;
        [SerializeField] AudioClip m_SlideClip;

        public string Id => m_Id;
        public string DisplayName => m_DisplayName;
        public string Role => m_Role;

        public int FireModeCount => m_FireModes.Length;
        public float RoundsPerMinute => m_RoundsPerMinute;
        public int Pellets => m_Pellets;
        public float BaseSpread => m_BaseSpread;
        public float SpreadPerShot => m_SpreadPerShot;
        public float MaxSpreadBloom => m_MaxSpreadBloom;
        public float SpreadRecovery => m_SpreadRecovery;
        public float MaxRange => m_MaxRange;
        public int TracerEvery => m_TracerEvery;

        public int MagazineSize => m_MagazineSize;
        public bool ShellFed => m_ShellFed;
        public bool SlideLocksOnEmpty => m_SlideLocksOnEmpty;
        public float QuickReloadSeconds => m_QuickReloadSeconds;
        public Magazine MagazinePrefab => m_MagazinePrefab;

        public float KickBack => m_KickBack;
        public float MuzzleClimb => m_MuzzleClimb;
        public float YawJitter => m_YawJitter;
        public float RecoilReturnSpeed => m_RecoilReturnSpeed;
        public float TwoHandRecoilScale => m_TwoHandRecoilScale;

        public float HapticAmplitude => m_HapticAmplitude;
        public float HapticDuration => m_HapticDuration;
        public float HapticTailAmplitude => m_HapticTailAmplitude;
        public float HapticTailDuration => m_HapticTailDuration;
        public float SupportHandHaptics => m_SupportHandHaptics;

        public bool LongRange => m_LongRange;

        public float PitchJitter => m_PitchJitter;
        public AudioClip DryFireClip => m_DryFireClip;
        public AudioClip MagazineOutClip => m_MagazineOutClip;
        public AudioClip MagazineInClip => m_MagazineInClip;
        public AudioClip SlideClip => m_SlideClip;
        public int ShotClipCount => m_ShotClips.Length;

        public FireMode GetFireMode(int index)
        {
            return m_FireModes.Length == 0 ? FireMode.Semi : m_FireModes[Mathf.Abs(index) % m_FireModes.Length];
        }

        public AudioClip GetShotClip(int index)
        {
            return m_ShotClips.Length == 0 ? null : m_ShotClips[Mathf.Abs(index) % m_ShotClips.Length];
        }
    }
}
