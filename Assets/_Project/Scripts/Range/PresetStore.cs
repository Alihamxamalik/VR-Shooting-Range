using System;
using System.IO;
using UnityEngine;

namespace RangeControl.Range
{
    /// <summary>The control panel's three preset slots, each holding a full setup, saved as a small JSON file.</summary>
    public sealed class PresetStore
    {
        public const int SlotCount = 3;
        public const string FileName = "presets.json";

        [Serializable]
        public sealed class Slot
        {
            public bool used;
            public int modeIndex;
            public int tierIndex;
            public RangeSettingsData settings;
        }

        [Serializable]
        sealed class PresetFile
        {
            public int version = 1;
            public Slot[] slots;
        }

        readonly string m_Path;
        Slot[] m_Slots = NewSlots();

        public PresetStore(string path)
        {
            m_Path = path;
        }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public Slot Get(int index)
        {
            return m_Slots[Mathf.Clamp(index, 0, SlotCount - 1)];
        }

        public void Set(int index, int modeIndex, int tierIndex, in RangeSettingsData settings)
        {
            var slot = Get(index);
            slot.used = true;
            slot.modeIndex = modeIndex;
            slot.tierIndex = tierIndex;
            slot.settings = settings;
            Save();
        }

        public void Load()
        {
            m_Slots = NewSlots();
            try
            {
                if (!File.Exists(m_Path))
                    return;
                var file = JsonUtility.FromJson<PresetFile>(File.ReadAllText(m_Path));
                if (file?.slots == null)
                    return;
                for (var i = 0; i < SlotCount && i < file.slots.Length; i++)
                {
                    if (file.slots[i] != null)
                        m_Slots[i] = file.slots[i];
                }
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Presets could not be read; starting with empty slots. " + exception.Message);
                m_Slots = NewSlots();
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(m_Path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(m_Path, JsonUtility.ToJson(new PresetFile { slots = m_Slots }, true));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Presets could not be saved: " + exception.Message);
            }
        }

        static Slot[] NewSlots()
        {
            var slots = new Slot[SlotCount];
            for (var i = 0; i < SlotCount; i++)
                slots[i] = new Slot();
            return slots;
        }
    }
}
