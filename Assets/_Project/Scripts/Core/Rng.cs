namespace RangeControl.Core
{
    /// <summary>
    /// Small seedable xorshift generator. Shot spread and target motion use it instead of
    /// <see cref="UnityEngine.Random"/> so they are repeatable in tests and never touch shared state.
    /// </summary>
    public struct Rng
    {
        const uint k_DefaultSeed = 0x9E3779B9u;

        uint m_State;

        public Rng(uint seed)
        {
            m_State = seed == 0u ? k_DefaultSeed : seed;
        }

        public uint NextUInt()
        {
            var x = m_State == 0u ? k_DefaultSeed : m_State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            m_State = x;
            return x;
        }

        /// <summary>Uniform in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>+1 or -1 with equal chance.</summary>
        public float Sign()
        {
            return (NextUInt() & 1u) == 0u ? 1f : -1f;
        }
    }
}
