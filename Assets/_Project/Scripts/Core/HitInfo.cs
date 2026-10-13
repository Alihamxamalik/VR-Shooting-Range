using RangeControl.Weapons;
using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>Everything a target needs to know about one bullet (or one shotgun pellet) landing on it.</summary>
    public readonly struct HitInfo
    {
        /// <summary>Identifies the trigger pull. All pellets of one shotgun blast share it.</summary>
        public readonly int ShotId;
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        /// <summary>Metres from the muzzle to the hit point.</summary>
        public readonly float Distance;
        public readonly Collider Collider;
        public readonly WeaponData Weapon;

        public HitInfo(int shotId, Vector3 origin, Vector3 direction, Vector3 point, Vector3 normal, float distance, Collider collider, WeaponData weapon)
        {
            ShotId = shotId;
            Origin = origin;
            Direction = direction;
            Point = point;
            Normal = normal;
            Distance = distance;
            Collider = collider;
            Weapon = weapon;
        }
    }

    /// <summary>
    /// The one interface every shootable thing implements. The shooter finds it on the collider it hit,
    /// so adding a new target type never touches weapon code.
    /// </summary>
    public interface IHittable
    {
        void OnHit(in HitInfo hit);
    }
}
