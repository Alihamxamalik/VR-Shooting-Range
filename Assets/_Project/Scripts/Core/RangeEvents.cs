using System;
using RangeControl.Weapons;
using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>One trigger pull, raised after all of its pellets have been resolved.</summary>
    public readonly struct ShotInfo
    {
        public readonly int ShotId;
        public readonly WeaponData Weapon;
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;

        public ShotInfo(int shotId, WeaponData weapon, Vector3 origin, Vector3 direction)
        {
            ShotId = shotId;
            Weapon = weapon;
            Origin = origin;
            Direction = direction;
        }
    }

    /// <summary>The path of one bullet or pellet, for tracers and impact effects. Carries no scoring.</summary>
    public readonly struct BulletTrace
    {
        public readonly Vector3 Origin;
        public readonly Vector3 End;
        public readonly Vector3 Normal;
        /// <summary>Null when the bullet left the range without hitting anything.</summary>
        public readonly Collider Collider;
        public readonly bool ShowTracer;

        public BulletTrace(Vector3 origin, Vector3 end, Vector3 normal, Collider collider, bool showTracer)
        {
            Origin = origin;
            End = end;
            Normal = normal;
            Collider = collider;
            ShowTracer = showTracer;
        }
    }

    /// <summary>A bullet landed on a target. Points here are the target's base value, before combo and mode multipliers.</summary>
    public struct TargetHit
    {
        public int ShotId;
        public TargetType Type;
        public SurfaceType Surface;
        /// <summary>Base points for this hit. Negative for a no-shoot, zero for paper outside the rings.</summary>
        public int BasePoints;
        /// <summary>Paper ring value 0-10, or -1 for targets without rings.</summary>
        public int Ring;
        public bool InnerX;
        /// <summary>Hit position on the target face in metres from its centre (x right, y up).</summary>
        public Vector2 FacePoint;
        /// <summary>Half-width of the target face in metres, so a monitor can normalise <see cref="FacePoint"/>.</summary>
        public float FaceRadius;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
        /// <summary>World scale of the target, used to pitch steel pings.</summary>
        public float Scale;
        public WeaponData Weapon;
    }

    /// <summary>A target left the range without being hit.</summary>
    public readonly struct TargetExpiry
    {
        public readonly TargetType Type;
        public readonly Vector3 Position;

        public TargetExpiry(TargetType type, Vector3 position)
        {
            Type = type;
            Position = position;
        }
    }

    /// <summary>Points actually awarded for a hit, after combo and mode multipliers.</summary>
    public readonly struct ScoreAward
    {
        public readonly int Points;
        public readonly float Combo;
        public readonly int TotalScore;
        public readonly Vector3 Position;
        public readonly float Distance;
        /// <summary>False outside a round: the popup still shows, but nothing is added to a score.</summary>
        public readonly bool Counted;

        public ScoreAward(int points, float combo, int totalScore, Vector3 position, float distance, bool counted)
        {
            Points = points;
            Combo = combo;
            TotalScore = totalScore;
            Position = position;
            Distance = distance;
            Counted = counted;
        }
    }

    /// <summary>
    /// The event channel every system talks through. Weapons, targets, scoring, effects and UI each hold a
    /// reference to this one asset and never to each other, so no system looks another up at runtime.
    /// Subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    [CreateAssetMenu(menuName = "Range Control/Range Events", fileName = "RangeEvents")]
    public sealed class RangeEvents : ScriptableObject
    {
        public event Action<ShotInfo> ShotFired;
        public event Action<BulletTrace> BulletTraced;
        public event Action<TargetHit> TargetHit;
        public event Action<TargetExpiry> TargetExpired;
        public event Action<ScoreAward> ScoreAwarded;
        /// <summary>Raised when the combo is lost to a miss or a no-shoot.</summary>
        public event Action ComboBroken;
        public event Action<RoundState> RoundStateChanged;
        /// <summary>Seconds left on the start countdown: 3, 2, 1.</summary>
        public event Action<int> CountdownTick;
        public event Action<RoundResult> RoundEnded;
        /// <summary>The game mode or tier selection changed while idle.</summary>
        public event Action ModeChanged;
        public event Action<Weapon> WeaponGrabbed;
        public event Action<Weapon> WeaponReleased;
        public event Action<Weapon> WeaponDryFired;
        /// <summary>Rounds loaded, fire mode or slide state changed on a weapon.</summary>
        public event Action<Weapon> WeaponStateChanged;
        /// <summary>A fresh paper target was hung, so anything showing the old holes should clear.</summary>
        public event Action PaperCleared;

        public void RaiseShotFired(in ShotInfo shot) => ShotFired?.Invoke(shot);
        public void RaiseBulletTraced(in BulletTrace trace) => BulletTraced?.Invoke(trace);
        public void RaiseTargetHit(in TargetHit hit) => TargetHit?.Invoke(hit);
        public void RaiseTargetExpired(in TargetExpiry expiry) => TargetExpired?.Invoke(expiry);
        public void RaiseScoreAwarded(in ScoreAward award) => ScoreAwarded?.Invoke(award);
        public void RaiseComboBroken() => ComboBroken?.Invoke();
        public void RaiseRoundStateChanged(RoundState state) => RoundStateChanged?.Invoke(state);
        public void RaiseCountdownTick(int secondsLeft) => CountdownTick?.Invoke(secondsLeft);
        public void RaiseRoundEnded(RoundResult result) => RoundEnded?.Invoke(result);
        public void RaiseModeChanged() => ModeChanged?.Invoke();
        public void RaiseWeaponGrabbed(Weapon weapon) => WeaponGrabbed?.Invoke(weapon);
        public void RaiseWeaponReleased(Weapon weapon) => WeaponReleased?.Invoke(weapon);
        public void RaiseWeaponDryFired(Weapon weapon) => WeaponDryFired?.Invoke(weapon);
        public void RaiseWeaponStateChanged(Weapon weapon) => WeaponStateChanged?.Invoke(weapon);
        public void RaisePaperCleared() => PaperCleared?.Invoke();
    }
}
