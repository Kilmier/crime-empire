namespace CrimeSim.Session;

using System.Collections.ObjectModel;
using CrimeSim.Domain;

/// <summary>
/// A claim as the player-facing layer may hold it: the predicate, and not the correlation number.
///
/// <b>Why this type exists rather than passing <see cref="Claim"/> through.</b> The predicate itself
/// is canon-supported player-facing data — <c>INFORMATION_AND_LEGIBILITY.md</c>'s "Player
/// Intelligence Entry" lists "claims currently available to the player character" as a field of the
/// contract, and the claim vocabulary is what those claims are made of. <see cref="Claim.EventId"/>
/// is not. It is <see cref="Sim.WorldEvent.Id"/>, a monotonic counter over the authoritative truth
/// log, and `REVIEW_LEDGER.md` already treats it as developer correlation data — the behavioural
/// replay comparator deliberately excludes every free-text field that can embed one, precisely
/// because it is a global identifier and not a fact about the world.
///
/// So the predicate crosses the boundary and the counter is dropped at it. Milestone 009's first
/// correction; before it, a player-facing belief carried <c>PersonUsedViolence(tommy -&gt;
/// bellini-grocery#7)</c> with the <c>#7</c> intact.
///
/// <b>What is lost, stated rather than hidden.</b> Two incidents at the same shop by the same person
/// differ only in <c>EventId</c> and therefore become one <see cref="PlayerClaim"/>. Nothing
/// player-facing could distinguish them anyway — <see cref="PlayerNarration.Describe"/> never printed
/// the counter, so both already rendered as the same sentence — but a future surface that needed to
/// tell two incidents apart would need an identity granted for that purpose, not this one restored.
/// </summary>
public readonly record struct PlayerClaim(ClaimKind Kind, string Subject, string Object)
{
    internal static PlayerClaim Of(Claim claim) => new(claim.Kind, claim.Subject, claim.Object);

    /// <summary>
    /// Whether this is the player-facing shadow of that claim, ignoring the counter.
    ///
    /// <b>Internal, and that is the point rather than an oversight.</b> A public overload would put
    /// <see cref="Claim"/> back into the player-facing surface as a parameter type — which the
    /// recursive boundary test catches, and correctly: the rule is that the type does not appear in
    /// the DTO graph at all, not that it appears only in positions somebody has argued are harmless.
    /// The comparison is a developer and test convenience; nothing an interface does needs it.
    /// </summary>
    internal bool Matches(Claim claim) => Equals(Of(claim));

    public override string ToString()
        => Object.Length == 0 ? $"{Kind}({Subject})" : $"{Kind}({Subject} -> {Object})";
}

/// <summary>
/// Collection handling for the player-facing records.
///
/// Every list-shaped property on a player-facing type is frozen here at construction. An
/// <c>IReadOnlyList&lt;T&gt;</c> is an interface and not a guarantee: handing out a
/// <c>List&lt;T&gt;</c> behind one lets any consumer cast it straight back and mutate the snapshot it
/// was given. That is the same defect milestone 006 fixed on <c>IRelationship.Grievances</c>, found
/// again on the player boundary by milestone 009's review, and it is fixed the same way — a
/// <see cref="ReadOnlyCollection{T}"/> cannot be cast to its backing list, and its
/// <c>IList</c> surface reports <c>IsReadOnly</c>.
///
/// Applied in an <c>init</c> accessor rather than at the call site, so the guarantee is a property of
/// the type and cannot be skipped by a future builder that forgets.
/// </summary>
internal static class Frozen
{
    internal static IReadOnlyList<T> List<T>(IReadOnlyList<T>? items)
        => items is ReadOnlyCollection<T> already
            ? already
            : new ReadOnlyCollection<T>(items?.ToList() ?? new List<T>());
}
