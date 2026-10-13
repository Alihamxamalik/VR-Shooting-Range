using RangeControl.Core;
using RangeControl.Weapons;
using UnityEditor;
using UnityEngine;

namespace RangeControl.Tests
{
    /// <summary>Builders for the small pieces of data the tests need.</summary>
    static class TestData
    {
        public static WeaponData Weapon(string id)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("m_Id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        public static TargetHit Hit(int shotId, int basePoints, WeaponData weapon = null, float distance = 15f, bool innerX = false)
        {
            return new TargetHit
            {
                ShotId = shotId,
                Type = basePoints < 0 ? TargetType.NoShoot : TargetType.MovingSilhouette,
                BasePoints = basePoints,
                Ring = -1,
                InnerX = innerX,
                Distance = distance,
                Weapon = weapon,
            };
        }

        public static ShotInfo Shot(int shotId, WeaponData weapon = null)
        {
            return new ShotInfo(shotId, weapon, Vector3.zero, Vector3.forward);
        }
    }
}
