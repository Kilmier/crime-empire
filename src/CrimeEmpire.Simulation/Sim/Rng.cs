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
    /// <b>A correction found reviewing milestone 022, first 2026-09-09, then corrected again the same
    /// day after Codex's review overstated the argument below.</b> <see cref="Fnv1a"/> is not
    /// GF(2)-linear — it multiplies — and nothing here depends on it being linear. What matters is
    /// only that it is a *fixed* function of the key: <c>Fnv1a(occasionKey)</c> is some fixed 32-bit
    /// constant for a given key, whatever it is. The finalizer used to be a single linear step,
    /// <c>h ^= h &gt;&gt; 15</c>, and everything downstream of that fixed constant — the XOR
    /// combination with <paramref name="worldSeed"/>, that finalizer, and <see cref="NextUInt"/>'s own
    /// xorshift steps — is linear over GF(2). Linearity is what lets the seed cancel: for two occasion
    /// keys K1, K2 sharing a seed, their pre-finalizer states are <c>Fnv1a(K1) XOR seed*C</c> and
    /// <c>Fnv1a(K2) XOR seed*C</c>, and applying a GF(2)-linear map <c>f</c> to both distributes over
    /// XOR, so <c>f(Fnv1a(K1) XOR seed*C) XOR f(Fnv1a(K2) XOR seed*C) = f(Fnv1a(K1)) XOR f(Fnv1a(K2))</c>
    /// — the <c>seed*C</c> term cancels regardless of what <paramref name="worldSeed"/> is. Because
    /// every subsequent <see cref="NextUInt"/> draw is itself another GF(2)-linear map applied to that
    /// same starting state, this cancellation reproduces at every corresponding draw position: the two
    /// streams' XOR delta at draw <c>t</c> is a fixed value depending only on K1, K2 and <c>t</c>,
    /// never on the seed.
    ///
    /// <b>What that licenses, precisely, and no further.</b> A fixed, seed-independent XOR relationship
    /// between two streams is not by itself a proof that they can never both clear a probability
    /// threshold together — whether it forbids that depends on what the fixed delta actually is. What
    /// it explains is the demonstrated case: three street-talk observers' occasion-keyed rolls on one
    /// event, found unable to co-succeed across tens of thousands of seeds searched. This account does
    /// not extend that to a universal claim that every arbitrary pair of keys under this method was
    /// unable to co-succeed at every seed — only that the mapping made the specific, tested case
    /// unreachable, which is what the fix below addresses. <see cref="ForDecision"/> has the identical
    /// structural shape and therefore the same correlation *risk*, but sharing this outcome for any
    /// concrete pair of decisions has not been demonstrated the way it was here — recorded as a risk,
    /// not a proven identical failure, in <c>OPEN_CONCERNS.md</c>.
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
