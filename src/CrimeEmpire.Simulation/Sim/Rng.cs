namespace CrimeSim.Sim;

/// <summary>
/// Deterministic xorshift32 stream.
///
/// Seeded per decision from (worldSeed, characterId, decisionIndex) rather than from one global
/// stream. That is deliberate: it means changing Vincent's traits does not shift Kane's dice, so
/// A/B variant runs stay comparable instead of confounded by stream drift.
///
/// Uses its own FNV-1a string hash because String.GetHashCode is randomised per process in .NET
/// and would break run-to-run reproducibility.
/// </summary>
public sealed class Rng
{
    private uint _state;

    private Rng(uint seed) => _state = seed == 0 ? 0x9E3779B9u : seed;

    public static Rng ForDecision(int worldSeed, string characterId, int decisionIndex)
    {
        uint h = Fnv1a(characterId);
        unchecked
        {
            h ^= (uint)worldSeed * 0x85EBCA6Bu;
            h ^= (uint)decisionIndex * 0xC2B2AE35u;
            h ^= h >> 15;
        }
        return new Rng(h);
    }

    public static Rng ForWorld(int worldSeed) => new(unchecked((uint)worldSeed * 0x9E3779B9u + 1u));

    /// <summary>
    /// Seeded from a causally local occasion key rather than any part of a decision index. Use this
    /// where the occasion is not "this actor's Nth decision" but something else stable and
    /// insertion-proof — a strategy instance's Nth advance, or a specific observer's chance to
    /// notice a specific trace of a specific advance. The key must never contain a global scheduling
    /// identifier (ScheduledEvent.Id, WorldEvent.Id, or a Claim.EventId derived from the truth-log
    /// counter) — those shift when anything anywhere is scheduled or recorded, which is the defect
    /// this method exists to avoid repeating.
    ///
    /// <b>A correction found reviewing milestone 022, 2026-09-09.</b> The finalizer used to be a
    /// single linear step, <c>h ^= h &gt;&gt; 15</c>. XOR and shift are both linear over GF(2), and so
    /// is the rest of this pipeline up to and including this method's
    /// own combination of the key hash with <paramref name="worldSeed"/> — which meant that for any
    /// two occasion keys under the same seed, the seed's own contribution cancelled out of their XOR
    /// difference algebraically, leaving a fixed, seed-independent delta between the two streams'
    /// entire output sequences. No seed could ever change it. Two street-talk observers of the same
    /// event were found permanently unable to both succeed, not rarely but for every one of tens of
    /// thousands of seeds tested — and the same proof holds for any two keys under this method,
    /// related or not, and for <see cref="ForDecision"/>'s identical pipeline shape (confirmed, not
    /// fixed here — see <c>OPEN_CONCERNS.md</c>).
    ///
    /// The finalizer below is a standard integer-hash avalanche (fmix32, as used in MurmurHash3):
    /// multiplication by an odd constant is not linear over GF(2), so it breaks the algebra the
    /// defect depended on. Nothing about the key, the seed, or what a caller does with the resulting
    /// stream changed — a given <paramref name="worldSeed"/> and <paramref name="occasionKey"/> still
    /// produce one fixed, reproducible stream, and two unrelated occasion keys still cannot influence
    /// each other's schedule (the key still carries no global counter) — only the *relationship*
    /// between any two distinct keys' streams is no longer forced into a fixed, unbreakable pattern.
    /// This is not a claim that two keys' streams are statistically independent — nothing here proves
    /// that, and nothing needs it to be true. It is the narrower, load-bearing fact: they are no
    /// longer structurally locked together by a seed-independent relationship, so two or more distinct
    /// occasions are free to succeed together at a given seed rather than being algebraically barred
    /// from it. <c>DESIGN_DECISIONS.md</c>'s "Keyed stochastic opportunities can co-succeed" records
    /// the durable rule this establishes.
    /// </summary>
    public static Rng ForOccasion(int worldSeed, string occasionKey)
    {
        uint h = Fnv1a(occasionKey);
        unchecked
        {
            h ^= (uint)worldSeed * 0x85EBCA6Bu;
            // fmix32 (MurmurHash3's finalizer). Multiplication is not GF(2)-linear, which is what
            // decouples two occasion keys' streams from each other — see the correction note above.
            h ^= h >> 16;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;
        }
        return new Rng(h);
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

    /// <summary>Uniform in [0,1).</summary>
    public double NextDouble() => NextUInt() / 4294967296.0;

    /// <summary>Uniform in [min,max).</summary>
    public double Range(double min, double max) => min + NextDouble() * (max - min);

    public int RangeInt(int minInclusive, int maxExclusive)
        => minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));

    public bool Chance(double probability) => NextDouble() < probability;

    public static uint Fnv1a(string s)
    {
        uint hash = 2166136261u;
        foreach (char c in s)
        {
            unchecked
            {
                hash ^= c;
                hash *= 16777619u;
            }
        }
        return hash;
    }
}
