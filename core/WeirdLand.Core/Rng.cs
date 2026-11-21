using System.Collections.Generic;

namespace WeirdLand.Core
{
    /// <summary>Deterministic mulberry32 PRNG. All game randomness must go through this.</summary>
    public sealed class Rng
    {
        private uint _state;

        public Rng(uint seed) { _state = seed; }

        public uint State => _state;

        public double NextDouble()
        {
            unchecked
            {
                _state += 0x6D2B79F5u;
                uint t = _state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                t ^= t >> 14;
                return t / 4294967296.0;
            }
        }

        public int Next(int maxExclusive) => (int)(NextDouble() * maxExclusive);

        public bool Chance(double p) => NextDouble() < p;

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        public Rng Fork() => new Rng(unchecked((uint)(NextDouble() * uint.MaxValue) ^ 0x9E3779B9u));
    }
}
