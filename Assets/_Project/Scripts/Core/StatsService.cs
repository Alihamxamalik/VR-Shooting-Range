using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>What the scoreboard shows when a round ends, and what gets written to the stats file.</summary>
    [Serializable]
    public sealed class RoundResult
    {
        public GameMode mode;
        public string modeName;
        public string tierName;
        /// <summary>Leaderboard key, for example "MovingGallery/Fast".</summary>
        public string boardKey;
        public int score;
        public int shots;
        public int hits;
        public float accuracy;
        public int bestStreak;
        public int targetsHit;
        public int targetsMissed;
        public int penalties;
        public int innerX;
        /// <summary>Widest distance between any two holes on the paper, in centimetres. Zero when not measured.</summary>
        public float groupSizeCm;
        public float longestHit;
        public float duration;
        public Medal medal;
        /// <summary>False in modes that have no medal thresholds, such as Free Practice.</summary>
        public bool hasMedals;
        public bool isPersonalBest;
        public int previousBest;
        public string weaponId;
    }

    [Serializable]
    public sealed class WeaponStats
    {
        public string weaponId;
        public int shots;
        public int hits;
        public float longestHit;

        public float Accuracy => shots > 0 ? (float)hits / shots : 0f;
    }

    [Serializable]
    public sealed class ScoreEntry
    {
        public int score;
        public int innerX;
        public float accuracy;
        public string weaponId;
        /// <summary>ISO 8601, UTC.</summary>
        public string date;
    }

    [Serializable]
    public sealed class Leaderboard
    {
        public string key;
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    [Serializable]
    public sealed class StatsData
    {
        public int version = StatsService.CurrentVersion;
        public List<WeaponStats> weapons = new List<WeaponStats>();
        public List<Leaderboard> boards = new List<Leaderboard>();
        public float longestHit;
        public string longestHitWeaponId;
        /// <summary>Best Precision group in centimetres. Zero until one is recorded.</summary>
        public float bestGroupCm;
        public bool tutorialCompleted;
    }

    /// <summary>
    /// Loads and saves player stats as a versioned JSON file. Plain C# with the file path and the clock passed
    /// in, so the tests run it against a temporary folder.
    /// </summary>
    public sealed class StatsService
    {
        public const int CurrentVersion = 1;
        public const int LeaderboardSize = 5;
        public const string FileName = "stats.json";

        readonly string m_Path;
        readonly Func<DateTime> m_Clock;

        public StatsData Data { get; private set; } = new StatsData();

        public StatsService(string path, Func<DateTime> clock = null)
        {
            m_Path = path;
            m_Clock = clock ?? (() => DateTime.UtcNow);
        }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public void Load()
        {
            Data = new StatsData();
            var temp = m_Path + ".tmp";
            try
            {
                // A save that was interrupted between delete and move leaves only the temp file behind.
                if (!File.Exists(m_Path) && File.Exists(temp))
                    File.Move(temp, m_Path);
                if (!File.Exists(m_Path))
                    return;

                var loaded = JsonUtility.FromJson<StatsData>(File.ReadAllText(m_Path));
                if (loaded != null)
                    Data = Migrate(loaded);
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Stats file could not be read and was set aside; starting fresh. " + exception.Message);
                TryKeepCorruptCopy();
                Data = new StatsData();
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(m_Path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                Data.version = CurrentVersion;
                var temp = m_Path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(m_Path))
                    File.Delete(m_Path);
                File.Move(temp, m_Path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Stats could not be saved: " + exception.Message);
            }
        }

        public int BestScore(string boardKey)
        {
            var board = FindBoard(boardKey);
            return board != null && board.entries.Count > 0 ? board.entries[0].score : 0;
        }

        public Leaderboard FindBoard(string boardKey)
        {
            foreach (var board in Data.boards)
            {
                if (board.key == boardKey)
                    return board;
            }

            return null;
        }

        public WeaponStats FindWeapon(string weaponId)
        {
            foreach (var weapon in Data.weapons)
            {
                if (weapon.weaponId == weaponId)
                    return weapon;
            }

            return null;
        }

        /// <summary>
        /// Folds a finished round into the stats: per-weapon tallies, the mode's top five and the records.
        /// Fills in <see cref="RoundResult.isPersonalBest"/> and <see cref="RoundResult.previousBest"/>.
        /// </summary>
        public void RecordRound(RoundResult result, IReadOnlyDictionary<string, ScoreService.WeaponTally> weapons)
        {
            if (weapons != null)
            {
                foreach (var pair in weapons)
                {
                    var stats = FindWeapon(pair.Key);
                    if (stats == null)
                    {
                        stats = new WeaponStats { weaponId = pair.Key };
                        Data.weapons.Add(stats);
                    }

                    stats.shots += pair.Value.Shots;
                    stats.hits += pair.Value.Hits;
                    if (pair.Value.LongestHit > stats.longestHit)
                        stats.longestHit = pair.Value.LongestHit;
                    if (pair.Value.LongestHit > Data.longestHit)
                    {
                        Data.longestHit = pair.Value.LongestHit;
                        Data.longestHitWeaponId = pair.Key;
                    }
                }
            }

            if (result.mode == GameMode.Precision && result.groupSizeCm > 0f && (Data.bestGroupCm <= 0f || result.groupSizeCm < Data.bestGroupCm))
                Data.bestGroupCm = result.groupSizeCm;

            result.previousBest = BestScore(result.boardKey);
            result.isPersonalBest = result.shots > 0 && result.score > result.previousBest;
            if (result.shots == 0)
                return;

            var board = FindBoard(result.boardKey);
            if (board == null)
            {
                board = new Leaderboard { key = result.boardKey };
                Data.boards.Add(board);
            }

            var entry = new ScoreEntry
            {
                score = result.score,
                innerX = result.innerX,
                accuracy = result.accuracy,
                weaponId = result.weaponId,
                date = m_Clock().ToString("o"),
            };

            // Higher score first; the inner-X count breaks ties; an earlier entry keeps its place over a later equal one.
            var index = 0;
            while (index < board.entries.Count && !Beats(entry, board.entries[index]))
                index++;
            board.entries.Insert(index, entry);
            if (board.entries.Count > LeaderboardSize)
                board.entries.RemoveRange(LeaderboardSize, board.entries.Count - LeaderboardSize);
        }

        static bool Beats(ScoreEntry candidate, ScoreEntry existing)
        {
            if (candidate.score != existing.score)
                return candidate.score > existing.score;
            return candidate.innerX > existing.innerX;
        }

        static StatsData Migrate(StatsData data)
        {
            // Version 1 is the first format. Later versions upgrade older files here, one step at a time.
            if (data.weapons == null)
                data.weapons = new List<WeaponStats>();
            if (data.boards == null)
                data.boards = new List<Leaderboard>();
            data.version = CurrentVersion;
            return data;
        }

        void TryKeepCorruptCopy()
        {
            try
            {
                if (File.Exists(m_Path))
                    File.Copy(m_Path, m_Path + ".corrupt", true);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // Nothing more to do: the file is unreadable and cannot be copied either.
            }
        }
    }
}
