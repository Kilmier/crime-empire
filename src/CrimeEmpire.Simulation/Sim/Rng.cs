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
    /// </summary>
    public static Rng ForOccasion(int worldSeed, string occasionKey)
    {
        uint h = Fnv1a(occasionKey);
        unchecked
        {
            h ^= (uint)worldSeed * 0x85EBCA6Bu;
            h ^= h >> 15;
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
