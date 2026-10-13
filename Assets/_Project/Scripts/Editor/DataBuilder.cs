using System.Linq;
using RangeControl.Core;
using RangeControl.Range;
using RangeControl.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RangeControl.Editor
{
    /// <summary>
    /// Creates the data assets: the shared event channel and settings, the five guns and the three game modes.
    /// The numbers are the starting values from the design document. Shared assets are only created when
    /// missing; gun and mode values are written each time, so after tuning them by hand in the Inspector,
    /// copy the result back here or stop running this step.
    /// </summary>
    public static class DataBuilder
    {
        public static RangeEvents Events => EditorAssets.Load<RangeEvents>(ProjectPaths.Data + "/RangeEvents.asset");
        public static RangeSettings Settings => EditorAssets.Load<RangeSettings>(ProjectPaths.Data + "/RangeSettings.asset");
        public static ComfortSettings Comfort => EditorAssets.Load<ComfortSettings>(ProjectPaths.Data + "/ComfortSettings.asset");
        public static PlayerAnchors Anchors => EditorAssets.Load<PlayerAnchors>(ProjectPaths.Data + "/PlayerAnchors.asset");
        public static HandButtons Buttons => EditorAssets.Load<HandButtons>(ProjectPaths.Data + "/HandButtons.asset");

        public static readonly string[] WeaponIds = { "m1911", "uzi", "ak74", "benelli_m4", "m107" };
        public static readonly string[] ModeNames = { "FreePractice", "MovingGallery", "Precision" };

        public static WeaponData Weapon(string id)
        {
            return EditorAssets.Load<WeaponData>(ProjectPaths.Weapons + "/" + id + ".asset");
        }

        public static GameModeDefinition Mode(string name)
        {
            return EditorAssets.Load<GameModeDefinition>(ProjectPaths.Modes + "/" + name + ".asset");
        }

        [MenuItem("Tools/Range Control/Build/2. Data", priority = 21)]
        public static void Build()
        {
            EditorAssets.EnsureFolder(ProjectPaths.Weapons);
            EditorAssets.EnsureFolder(ProjectPaths.Modes);

            EditorAssets.LoadOrCreate<RangeEvents>(ProjectPaths.Data + "/RangeEvents.asset");
            EditorAssets.LoadOrCreate<RangeSettings>(ProjectPaths.Data + "/RangeSettings.asset");
            EditorAssets.LoadOrCreate<ComfortSettings>(ProjectPaths.Data + "/ComfortSettings.asset");
            EditorAssets.LoadOrCreate<PlayerAnchors>(ProjectPaths.Data + "/PlayerAnchors.asset");
            BuildHandButtons();
            BuildWeapons();
            BuildModes();

            AssetDatabase.SaveAssets();
            Debug.Log("Range Control: data built.");
        }

        static void BuildHandButtons()
        {
            var path = ProjectPaths.Input + "/RangeInput.inputactions";
            var references = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>().ToArray();
            InputActionReference Find(string map, string action)
            {
                var reference = references.FirstOrDefault(r => r.action != null && r.action.actionMap.name == map && r.action.name == action);
                if (reference == null)
                    throw new System.InvalidOperationException("Input action " + map + "/" + action + " not found in " + path);
                return reference;
            }

            var buttons = EditorAssets.LoadOrCreate<HandButtons>(ProjectPaths.Data + "/HandButtons.asset");
            EditorAssets.Set(buttons,
                ("m_LeftPrimary", Find("LeftHand", "Primary")),
                ("m_LeftSecondary", Find("LeftHand", "Secondary")),
                ("m_RightPrimary", Find("RightHand", "Primary")),
                ("m_RightSecondary", Find("RightHand", "Secondary")));
        }

        static AudioClip Clip(string relativePath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/" + relativePath);
            if (clip == null)
                Debug.LogWarning("Range Control: missing audio clip " + relativePath);
            return clip;
        }

        static void BuildWeapons()
        {
            // The M1911 row is the feel of the original pistol script, value for value.
            Weapon("m1911", "M1911", "Pistol", new[] { FireMode.Semi }, 400f, 1, 0.3f, 0f, 0f, 150f, 1,
                7, false, true, 0.03f, 6f, 2f, 8f, 0.95f, 0.045f, 0.35f, 0.05f, false,
                new[] { Clip("Weapons/Shot_M1911_1.wav"), Clip("Weapons/Shot_M1911_2.wav") });

            Weapon("uzi", "Uzi", "SMG", new[] { FireMode.Auto }, 600f, 1, 1.2f, 0.25f, 2.5f, 150f, 3,
                32, false, false, 0.01f, 2f, 1f, 10f, 0.5f, 0.03f, 0.2f, 0.03f, false,
                new[] { Clip("Weapons/Shot_Uzi_1.wav"), Clip("Weapons/Shot_Uzi_2.wav") });

            Weapon("ak74", "AK74", "Assault rifle", new[] { FireMode.Semi, FireMode.Auto }, 650f, 1, 0.6f, 0.2f, 2f, 300f, 3,
                30, false, false, 0.02f, 3f, 1.2f, 9f, 0.7f, 0.035f, 0.25f, 0.04f, false,
                new[] { Clip("Weapons/Shot_AK74_1.wav"), Clip("Weapons/Shot_AK74_2.wav") });

            Weapon("benelli_m4", "Benelli M4", "Shotgun", new[] { FireMode.Semi }, 240f, 8, 3.5f, 0f, 0f, 80f, 1,
                7, true, false, 0.05f, 10f, 2.5f, 6f, 1f, 0.07f, 0.45f, 0.08f, false,
                new[] { Clip("Weapons/Shot_Benelli_1.wav"), Clip("Weapons/Shot_Benelli_2.wav") });

            Weapon("m107", "M107", "Sniper rifle", new[] { FireMode.Semi }, 60f, 1, 0.05f, 0f, 0f, 500f, 1,
                10, false, false, 0.06f, 12f, 1.5f, 5f, 1f, 0.09f, 0.5f, 0.12f, true,
                new[] { Clip("Weapons/Shot_M107_1.wav"), Clip("Weapons/Shot_M107_2.wav") });
        }

        static void Weapon(string id, string displayName, string role, FireMode[] modes, float roundsPerMinute, int pellets,
            float baseSpread, float spreadPerShot, float maxBloom, float maxRange, int tracerEvery,
            int magazineSize, bool shellFed, bool slideLocks, float kickBack, float climb, float yawJitter, float returnSpeed,
            float hapticAmplitude, float hapticDuration, float tailAmplitude, float tailDuration, bool longRange, AudioClip[] shots)
        {
            var weapon = EditorAssets.LoadOrCreate<WeaponData>(ProjectPaths.Weapons + "/" + id + ".asset");
            EditorAssets.Set(weapon,
                ("m_Id", id),
                ("m_DisplayName", displayName),
                ("m_Role", role),
                ("m_FireModes", modes.Cast<object>().ToArray()),
                ("m_RoundsPerMinute", roundsPerMinute),
                ("m_Pellets", pellets),
                ("m_BaseSpread", baseSpread),
                ("m_SpreadPerShot", spreadPerShot),
                ("m_MaxSpreadBloom", maxBloom),
                ("m_SpreadRecovery", 0.4f),
                ("m_MaxRange", maxRange),
                ("m_TracerEvery", tracerEvery),
                ("m_MagazineSize", magazineSize),
                ("m_ShellFed", shellFed),
                ("m_SlideLocksOnEmpty", slideLocks),
                ("m_QuickReloadSeconds", 1.2f),
                ("m_KickBack", kickBack),
                ("m_MuzzleClimb", climb),
                ("m_YawJitter", yawJitter),
                ("m_RecoilReturnSpeed", returnSpeed),
                ("m_TwoHandRecoilScale", 0.6f),
                ("m_HapticAmplitude", hapticAmplitude),
                ("m_HapticDuration", hapticDuration),
                ("m_HapticTailAmplitude", tailAmplitude),
                ("m_HapticTailDuration", tailDuration),
                ("m_SupportHandHaptics", 0.6f),
                ("m_LongRange", longRange),
                ("m_ShotClips", shots.Where(clip => clip != null).Cast<object>().ToArray()),
                ("m_PitchJitter", 0.05f),
                ("m_DryFireClip", Clip("Weapons/DryFire.wav")),
                ("m_MagazineOutClip", Clip("Weapons/MagazineOut.wav")),
                ("m_MagazineInClip", Clip("Weapons/MagazineIn.wav")),
                ("m_SlideClip", Clip("Weapons/SlideRelease.wav")));
        }

        static void BuildModes()
        {
            Mode("FreePractice", GameMode.FreePractice, "Free Practice",
                "No goal. Set the range any way you like and shoot. Harder settings are worth more.",
                false, true, true, 0f, 0, true, 0f, false);

            var gallery = Mode("MovingGallery", GameMode.MovingGallery, "Moving Gallery",
                "Hit as many moving targets as you can in 60 seconds. Leave the white no-shoots alone.",
                true, true, false, 60f, 0, true, 0.2f, false);
            // Developer bests are placeholders until they are replaced with real playtest scores.
            SetTiers(gallery,
                ("Slow", GallerySettings(RangeSettings.SlowSpeed, 2f), 1f, 3000),
                ("Medium", GallerySettings(RangeSettings.MediumSpeed, 1.5f), 1.5f, 5000),
                ("Fast", GallerySettings(RangeSettings.FastSpeed, 1f), 2f, 8000));

            var precision = Mode("Precision", GameMode.Precision, "Precision",
                "Ten shots at paper, no timer. Score the rings and keep the group tight.",
                true, false, false, 0f, 10, false, 0f, true);
            SetTiers(precision,
                ("10 m", PrecisionSettings(10f), 1f, 95),
                ("25 m", PrecisionSettings(25f), 1f, 85),
                ("50 m", PrecisionSettings(50f), 1f, 70));
        }

        static RangeSettingsData GallerySettings(float speed, float spawnInterval)
        {
            var settings = RangeSettingsData.Default;
            settings.targetDistance = 15f;
            settings.targetSpeed = speed;
            settings.pattern = MovementPattern.LeftToRight;
            settings.spawnInterval = spawnInterval;
            settings.targetsPerRound = RangeSettings.Endless;
            settings.roundTimer = 60;
            return settings;
        }

        static RangeSettingsData PrecisionSettings(float distance)
        {
            var settings = RangeSettingsData.Default;
            settings.targetDistance = distance;
            settings.targetSpeed = 0f;
            settings.pattern = MovementPattern.Static;
            settings.targetsPerRound = RangeSettings.Endless;
            return settings;
        }

        static GameModeDefinition Mode(string assetName, GameMode mode, string displayName, string goal, bool locksPanel, bool useCombo,
            bool useDifficultyFactor, float duration, int shotLimit, bool spawnsTargets, float noShootChance, bool usesPaper)
        {
            var asset = EditorAssets.LoadOrCreate<GameModeDefinition>(ProjectPaths.Modes + "/" + assetName + ".asset");
            EditorAssets.Set(asset,
                ("m_Mode", mode),
                ("m_DisplayName", displayName),
                ("m_Goal", goal),
                ("m_LocksPanel", locksPanel),
                ("m_UseCombo", useCombo),
                ("m_UseDifficultyFactor", useDifficultyFactor),
                ("m_Duration", duration),
                ("m_ShotLimit", shotLimit),
                ("m_SpawnsTargets", spawnsTargets),
                ("m_NoShootChance", noShootChance),
                ("m_UsesPaper", usesPaper));
            return asset;
        }

        static void SetTiers(GameModeDefinition mode, params (string name, RangeSettingsData settings, float multiplier, int developerBest)[] tiers)
        {
            var serialized = new SerializedObject(mode);
            var list = serialized.FindProperty("m_Tiers");
            list.arraySize = tiers.Length;
            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = list.GetArrayElementAtIndex(i);
                tier.FindPropertyRelative("name").stringValue = tiers[i].name;
                tier.FindPropertyRelative("scoreMultiplier").floatValue = tiers[i].multiplier;
                tier.FindPropertyRelative("developerBest").intValue = tiers[i].developerBest;

                var data = tiers[i].settings;
                var settings = tier.FindPropertyRelative("settings");
                settings.FindPropertyRelative("targetDistance").floatValue = data.targetDistance;
                settings.FindPropertyRelative("targetSpeed").floatValue = data.targetSpeed;
                settings.FindPropertyRelative("pattern").intValue = (int)data.pattern;
                settings.FindPropertyRelative("spawnInterval").floatValue = data.spawnInterval;
                settings.FindPropertyRelative("popUpExposure").floatValue = data.popUpExposure;
                settings.FindPropertyRelative("targetsPerRound").intValue = data.targetsPerRound;
                settings.FindPropertyRelative("targetSize").intValue = (int)data.targetSize;
                settings.FindPropertyRelative("fireRatePercent").intValue = data.fireRatePercent;
                settings.FindPropertyRelative("infiniteAmmo").boolValue = data.infiniteAmmo;
                settings.FindPropertyRelative("laserSight").boolValue = data.laserSight;
                settings.FindPropertyRelative("roundTimer").intValue = data.roundTimer;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mode);
        }
    }
}
