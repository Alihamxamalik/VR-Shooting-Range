using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Weapons
{
    /// <summary>
    /// Resolves a shot the instant it is fired with a raycast from the muzzle. The ray starts from the muzzle
    /// as recoil has left it, not from the controller, so muzzle climb really costs accuracy in a burst.
    /// The tracer the player sees is drawn afterwards from the result and carries no hit logic.
    /// </summary>
    public sealed class HitscanShooter : MonoBehaviour
    {
        [SerializeField] RangeEvents m_Events;
        [SerializeField] Transform m_Muzzle;
        [Tooltip("Layers a bullet can hit: targets and environment, never weapons or the player.")]
        [SerializeField] LayerMask m_HitMask = ~0;
        [SerializeField] uint m_Seed = 1;

        Rng m_Rng;

        public Transform Muzzle => m_Muzzle;

        void Awake()
        {
            m_Rng = new Rng(m_Seed);
        }

        /// <summary>Reseeds the spread so a test can fire a repeatable pattern.</summary>
        public void SetSeed(uint seed)
        {
            m_Rng = new Rng(seed);
        }

        /// <summary>
        /// Fires every pellet of one shot. <paramref name="spreadDegrees"/> is the full angle of the cone the
        /// pellets land in; shots are weighted toward its centre.
        /// </summary>
        public void Fire(WeaponData weapon, int shotId, float spreadDegrees, bool showTracer)
        {
            var origin = m_Muzzle.position;
            var forward = m_Muzzle.forward;
            var right = m_Muzzle.right;
            var up = m_Muzzle.up;
            var maxOffset = Mathf.Tan(Mathf.Max(0f, spreadDegrees) * 0.5f * Mathf.Deg2Rad);

            for (var pellet = 0; pellet < weapon.Pellets; pellet++)
            {
                var direction = forward;
                if (maxOffset > 0f)
                {
                    var radius = maxOffset * m_Rng.NextFloat();
                    var angle = m_Rng.NextFloat() * (2f * Mathf.PI);
                    direction = (forward + right * (radius * Mathf.Cos(angle)) + up * (radius * Mathf.Sin(angle))).normalized;
                }

                if (Physics.Raycast(origin, direction, out var hit, weapon.MaxRange, m_HitMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.TryGetComponent(out IHittable hittable))
                        hittable.OnHit(new HitInfo(shotId, origin, direction, hit.point, hit.normal, hit.distance, hit.collider, weapon));
                    m_Events.RaiseBulletTraced(new BulletTrace(origin, hit.point, hit.normal, hit.collider, showTracer));
                }
                else
                {
                    m_Events.RaiseBulletTraced(new BulletTrace(origin, origin + direction * weapon.MaxRange, -direction, null, showTracer));
                }
            }
        }
    }
}
