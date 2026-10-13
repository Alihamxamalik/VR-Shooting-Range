using System;
using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>
    /// Player comfort and accessibility options, saved between sessions. The asset holds the defaults; the
    /// values in play live in memory, so changing them in the Editor never dirties the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Range Control/Comfort Settings", fileName = "ComfortSettings")]
    public sealed class ComfortSettings : ScriptableObject
    {
        const string k_PrefsKey = "RangeControl.Comfort";

        [Serializable]
        public struct Data
        {
            public bool seated;
            public bool leftHanded;
            public bool snapTurn;
            public bool quickReload;
            [Range(0f, 1f)] public float masterVolume;
            [Range(0f, 1f)] public float gunVolume;
            [Range(0f, 1f)] public float musicVolume;
            [Range(0f, 1f)] public float hapticStrength;
        }

        [SerializeField] Data m_Defaults = new Data { masterVolume = 1f, gunVolume = 1f, musicVolume = 0.6f, hapticStrength = 1f };

        [NonSerialized] Data m_Current;
        [NonSerialized] bool m_Loaded;
        [NonSerialized] bool m_EarProtection;

        public event Action Changed;

        public bool Seated { get => Current.seated; set { EnsureLoaded(); m_Current.seated = value; Commit(); } }
        public bool LeftHanded { get => Current.leftHanded; set { EnsureLoaded(); m_Current.leftHanded = value; Commit(); } }
        public bool SnapTurn { get => Current.snapTurn; set { EnsureLoaded(); m_Current.snapTurn = value; Commit(); } }
        /// <summary>Reload with a button press instead of handling magazines.</summary>
        public bool QuickReload { get => Current.quickReload; set { EnsureLoaded(); m_Current.quickReload = value; Commit(); } }
        public float MasterVolume { get => Current.masterVolume; set { EnsureLoaded(); m_Current.masterVolume = Mathf.Clamp01(value); Commit(); } }
        public float GunVolume { get => Current.gunVolume; set { EnsureLoaded(); m_Current.gunVolume = Mathf.Clamp01(value); Commit(); } }
        public float MusicVolume { get => Current.musicVolume; set { EnsureLoaded(); m_Current.musicVolume = Mathf.Clamp01(value); Commit(); } }
        public float HapticStrength { get => Current.hapticStrength; set { EnsureLoaded(); m_Current.hapticStrength = Mathf.Clamp01(value); Commit(); } }

        /// <summary>True while the earmuffs are on. Not saved: the player puts them on each session.</summary>
        public bool EarProtection
        {
            get => m_EarProtection;
            set
            {
                if (m_EarProtection == value)
                    return;
                m_EarProtection = value;
                Changed?.Invoke();
            }
        }

        Data Current
        {
            get
            {
                EnsureLoaded();
                return m_Current;
            }
        }

        public void ResetToDefaults()
        {
            m_Current = m_Defaults;
            m_Loaded = true;
            m_EarProtection = false;
            Changed?.Invoke();
        }

        void EnsureLoaded()
        {
            if (m_Loaded)
                return;
            m_Current = m_Defaults;
            m_Loaded = true;
            if (!Application.isPlaying || !PlayerPrefs.HasKey(k_PrefsKey))
                return;
            try
            {
                m_Current = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(k_PrefsKey));
            }
            catch (ArgumentException)
            {
                m_Current = m_Defaults;
            }
        }

        void Commit()
        {
            if (Application.isPlaying)
                PlayerPrefs.SetString(k_PrefsKey, JsonUtility.ToJson(m_Current));
            Changed?.Invoke();
        }
    }
}
