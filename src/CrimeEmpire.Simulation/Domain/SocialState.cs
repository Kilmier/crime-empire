namespace CrimeSim.Domain;

public sealed record Grievance(string AgainstId, string Description, double Severity, DateTime At);

/// <summary>
/// What a man seemed to make of what was put to him, as read off his face by the man who put it —
/// milestone 026. A perception, and it can be wrong: see <c>Org.Reactions</c> for the roll.
/// </summary>
public enum ImpressionKind
{
    SeemedConvinced,
    SeemedUnconvinced,
    SeemedFrightened,
    SeemedUnmoved,
    GaveNothingAway,
}

/// <summary>
/// One reading of another man's face, remembered on the reader's relationship toward him — the
/// same shape as <see cref="StandingChange"/> and for the same reason: it is about the man, so it
/// lives with him. <see cref="About"/> is the claim the exchange concerned, null for a demand.
/// </summary>
public sealed record Impression(ImpressionKind Kind, Claim? About, DateTime At);

/// <summary>
/// Why this character's standing toward another moved — the closed set of things that actually move
/// a relationship dimension at runtime.
///
/// <b>Typed, not prose, and that is the settled rule rather than a preference.</b>
/// `DESIGN_DECISIONS.md`: "No string authored by a scheduler or a generator crosses the boundary" —
/// `PlayerOption` builds its wording from a candidate's typed fields for exactly this reason. A
/// description written here would have only ids and claims to work with, so it would either leak
/// `bellini-grocery` into a sentence a player reads or force `Relations` to know about names, which
/// it has no business knowing. `Session/PlayerNarration.cs` turns these into words.
///
/// One member per runtime mutator in <see cref="Relations"/> that moves a dimension. `Meet` and
/// `Establish` are absent deliberately: meeting somebody moves nothing, and scenario construction is
/// not something a character remembers happening to him.
/// </summary>
public enum StandingCause
{
    /// <summary>Somebody asserted the opposite of a position he held. Trust fell.</summary>
    AccountContradicted,

    /// <summary>A fresh account agreed with a position he held. Trust rose.</summary>
    AccountCorroborated,

    /// <summary>Somebody frightened him. Fear rose.</summary>
    Frightened,
}

/// <summary>
/// One remembered reason this character's standing toward another moved — milestone 023.
///
/// <b>A record of something that already happened, never an input to anything.</b> Trust has moved at
/// runtime since milestone 006 and in both directions since 016; fear has moved since the first
/// coercion resolution. None of it left any trace of *why*, so a relationship that cooled because
/// somebody contradicted him to his face was indistinguishable, in the interface, from one that was
/// never warm — which `PlayerNarration.Standing` argued was correct for a reader who could reconstruct
/// it from the claim log, and is wrong for somebody playing a game. Matt reversed that on 2026-09-04.
///
/// <b>Deliberately not a dimension.</b> `RELATIONSHIPS.md`'s rule for admitting one is that it must
/// name a decision that reads it, and nothing scores these. A durable positive counterpart to
/// <see cref="Grievance"/> that fed loyalty would be a real fifth dimension and a much larger claim;
/// this is the history of the four that exist. Grievance is the precedent for the shape — dated, kept
/// on the relationship, surfaced to the player — and the reason that shape works is that it is written
/// where the movement happens, from the same evidence the movement is derived from, so it cannot
/// record a cause the character has no access to.
///
/// <b>No direction field.</b> Which way each cause moves things is fixed — a contradiction always
/// costs trust, a corroboration always adds it, being frightened always adds fear — so storing the
/// direction alongside the cause would be one fact in two places, free to disagree. The reader derives
/// it.
///
/// <b><paramref name="About"/> is what the exchange was about, and it was not here at first.</b> The
/// first version stored the cause alone, and the rendered roster came out as three identical lines on
/// one day — three genuinely different corroborations, which milestone 016's freshness rule permits
/// and requires, rendered as the same sentence repeated. A history that cannot distinguish its own
/// entries reads as a bug even when the state behind it is right. Null for
/// <see cref="StandingCause.Frightened"/>, which is about no claim at all: somebody put the
/// frighteners on him, and there is no proposition in it.
/// </summary>
public sealed record StandingChange(StandingCause Cause, DateTime At, Claim? About = null);

/// <summary>
/// One character's directed social state.
///
/// This type stores relationships and answers questions about them. It does not change them —
/// every mutation lives in <see cref="Relations"/>, and <see cref="IRelationship"/>'s
/// dimensions have private setters, so that separation is enforced by the compiler rather than by
/// convention. See the header of Relations.cs for why.
/// </summary>
public sealed class SocialState
{
    private readonly Dictionary<string, IRelationship> _relationships = new();

    public string? OrganizationId { get; set; }

    /// <summary>
    /// The relationships this character actually has, in id order.
    ///
    /// Ordered rather than handed out as a dictionary, because this feeds the replay comparison and
    /// dictionary enumeration order is an implementation detail the determinism rules explicitly
    /// forbid depending on. Callers that want a stable sequence get one here rather than each
    /// remembering to sort.
    /// </summary>
    public IEnumerable<IRelationship> All
        => _relationships.Values.OrderBy(r => r.OtherId, StringComparer.Ordinal);

    /// <summary>Ids of everyone this character has a relationship with, in id order.</summary>
    public IEnumerable<string> Others
        => _relationships.Keys.OrderBy(id => id, StringComparer.Ordinal);

    /// <summary>
    /// How this character stands toward another. <b>Reading never creates.</b>
    ///
    /// This used to be a get-or-create, which was invisible while relationships were outside the
    /// replay comparison and became a determinism hazard the moment they entered it: scoring reads
    /// a great many relationships that do not exist — every delegation candidate, every concession,
    /// every policy breach consults one — and a creating read would have made the act of scoring a
    /// candidate change the snapshot. A character with no relationship to somebody reads as zero
    /// across the board, which is what "no relationship" means, and scores identically to the empty
    /// record the old behaviour would have inserted.
    /// </summary>
    public IRelationship Toward(string otherId)
        => _relationships.TryGetValue(otherId, out var r) ? r : Relations.Absent(otherId);

    /// <summary>The stored relationship, or null. Null means none has ever been established.</summary>
    internal IRelationship? Existing(string otherId)
        => _relationships.TryGetValue(otherId, out var r) ? r : null;

    /// <summary>
    /// The stored relationship, creating an empty one if there is none.
    ///
    /// Internal, and called only by <see cref="Relations"/> on a route that is about to change
    /// something. Note that a bypass of this method would be inert rather than dangerous: what it
    /// hands back is a relationship nobody outside <see cref="Relations"/> can move. The enforcement
    /// lives on the dimensions, not on the dictionary.
    /// </summary>
    internal IRelationship Ensure(string otherId)
    {
        if (!_relationships.TryGetValue(otherId, out var r))
        {
            r = Relations.Create(otherId);
            _relationships[otherId] = r;
        }
        return r;
    }

    /// <summary>
    /// Everything this character holds against anybody, in a deterministic order: by the person it
    /// is against, then in the order the grievances accumulated against that person.
    /// </summary>
    public IEnumerable<Grievance> Grievances
    {
        get
        {
            foreach (var rel in All)
                foreach (var g in rel.Grievances)
                    yield return g;
        }
    }

    /// <summary>What this character holds against one person, summed.</summary>
    public double GrievanceAgainst(string otherId) => Toward(otherId).GrievanceWeight;
}
