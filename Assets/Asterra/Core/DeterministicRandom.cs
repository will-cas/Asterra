namespace Asterra.Core
{
    /// <summary>
    /// Seeded Xorshift32 for lockstep. Do not use UnityEngine.Random in simulation code.
    /// </summary>
    public struct DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(uint seed)
        {
            _state = seed == 0 ? 2463534242u : seed;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat()
        {
            return (NextUInt() & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
