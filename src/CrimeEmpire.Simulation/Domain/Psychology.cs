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

    /// <summary>
    /// Concealment, delay and delegation gain salience; perceived risk and the uncertainty penalty
    /// rise. Omitting an awkward fact from a report counts as concealment and gains salience with
    /// it; flatly denying it does not, because that is the version that collapses if anyone else
    /// talks.
    /// </summary>
    Cautious,

    /// <summary>
    /// Backing down or abandoning a commitment costs more status; deference to authority falls —
    /// including the deference involved in volunteering your own misconduct to your superior.
    /// </summary>
    Proud,

    /// <summary>
    /// Second-hand claims are discounted harder; hostile-intent claims gain salience; seeking a
    /// second account of what one person told you gains salience.
    /// </summary>
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

    /// <summary>
    /// Values are clamped to <c>[0,1]</c> on the way in, at the one point where they enter the type.
    ///
    /// <b>This used to be documentation rather than enforcement, and something came to depend on
    /// it.</b> Both indexers below have always said "in [0,1]"; nothing checked. Milestone 008's
    /// corrective commit then removed <c>Math.Clamp</c> from <see cref="Decision.Utility.LoyaltyReading"/>
    /// on the argument that the three weights total exactly 1.0 and every input is in range — which
    /// was true of every scenario and false of the public API, so a caller passing
    /// <c>Belonging = 5.0</c> got clamped behaviour before that commit and unclamped behaviour after
    /// it. A prose range is not an invariant. Codex found it.
    ///
    /// Clamping rather than throwing, to match <see cref="Relations"/>, which clamps every
    /// relationship dimension on every write. The two halves of a loyalty reading now enforce their
    /// range the same way, which is what lets the reading be split into separately inspectable parts
    /// without a clamp that could bind and could not be honestly apportioned.
    ///
    /// Both <c>With</c> overloads build a dictionary and delegate here, so this is the only gate that
    /// needs to hold. Traits are clamped for the same reason: the range is stated on the indexer and
    /// <c>Utility</c> reads them with coefficients that assume it — <c>1 - 0.55 * proud</c> changes
    /// sign above 1.8.
    /// </summary>
    public Psychology(
        IReadOnlyDictionary<Trait, double>? traits = null,
        IReadOnlyDictionary<Drive, double>? drives = null)
    {
        _traits = Clamped(traits);
        _drives = Clamped(drives);
    }

    private static Dictionary<TKey, double> Clamped<TKey>(IReadOnlyDictionary<TKey, double>? source)
        where TKey : notnull
    {
        var copy = new Dictionary<TKey, double>();
        if (source is null) return copy;
        foreach (var (key, value) in source) copy[key] = Math.Clamp(value, 0.0, 1.0);
        return copy;
    }

    /// <summary>Trait intensity in [0,1], enforced by the constructor. Absent traits read as 0.</summary>
    public double this[Trait t] => _traits.TryGetValue(t, out var v) ? v : 0.0;

    /// <summary>Drive weight in [0,1], enforced by the constructor. Absent drives read as 0.</summary>
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
