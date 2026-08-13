namespace CrimeSim.Domain;

/// <summary>
/// Traits modify perception, salience and evaluation. They never fire actions.
///
/// The vocabulary is closed and small on purpose. Every entry has a stated behavioural purpose
/// below; a trait that cannot name one does not belong here, and two traits that move the same
/// numbers should be merged.
/// </summary>
public enum Trait
{
    /// <summary>Coercive candidates gain salience; force looks more likely to work; escalation cost looks lower.</summary>
    Aggressive,

    /// <summary>Concealment, delay and delegation gain salience; perceived risk and the uncertainty penalty rise.</summary>
    Cautious,

    /// <summary>Backing down or abandoning a commitment costs more status; deference to authority falls.</summary>
    Proud,

    /// <summary>Second-hand claims are discounted harder; hostile-intent claims gain salience.</summary>
    Suspicious,
}

/// <summary>
/// Drives are utility weights: they make outcomes more or less desirable. They are not goals and
/// do not execute themselves.
///
/// Deliberately absent:
///  - Loyalty. It is derived per-relationship from trust, obligation and Belonging rather than
///    stored as a scalar, because a single loyalty number collapses distinctions that should
///    produce different behaviour (loyal to whom, and for which reason).
///  - Ambition. Ambition makes advancement valuable, which is what a Status weight already is.
///    If tuning shows a separate advancement drive is needed, that is a finding, not a default.
/// </summary>
public enum Drive
{
    Wealth,
    Status,
    Security,
    Belonging,
}

/// <summary>
/// RESTRICTED ACCESS. This type is read from exactly two places: Decision/Salience.cs and
/// Decision/Utility.cs.
///
/// Candidate generators receive a pre-computed SalienceProfile and a CharacterView that has no
/// Psychology property, so a generator physically cannot write `if (aggressive) EmitAttack()`.
/// That is the compile-time guard against this project's most-named anti-pattern.
/// </summary>
public sealed class Psychology
{
    private readonly Dictionary<Trait, double> _traits;
    private readonly Dictionary<Drive, double> _drives;

    public Psychology(
        IReadOnlyDictionary<Trait, double>? traits = null,
        IReadOnlyDictionary<Drive, double>? drives = null)
    {
        _traits = traits is null ? new() : new(traits);
        _drives = drives is null ? new() : new(drives);
    }

    /// <summary>Trait intensity in [0,1]. Absent traits read as 0.</summary>
    public double this[Trait t] => _traits.TryGetValue(t, out var v) ? v : 0.0;

    /// <summary>Drive weight in [0,1]. Absent drives read as 0.</summary>
    public double this[Drive d] => _drives.TryGetValue(d, out var v) ? v : 0.0;

    public IEnumerable<(Trait Trait, double Value)> SignificantTraits()
        => _traits.Where(kv => kv.Value >= 0.5)
                  .OrderByDescending(kv => kv.Value)
                  .ThenBy(kv => kv.Key)
                  .Select(kv => (kv.Key, kv.Value));

    public Psychology With(Trait t, double value)
    {
        var traits = new Dictionary<Trait, double>(_traits) { [t] = value };
        return new Psychology(traits, _drives);
    }

    public Psychology With(Drive d, double value)
    {
        var drives = new Dictionary<Drive, double>(_drives) { [d] = value };
        return new Psychology(_traits, drives);
    }
}
