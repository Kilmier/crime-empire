namespace CrimeSim.Sim;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;

public sealed class Business
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DistrictId { get; init; }
    public required string OwnerId { get; init; }

    public double MonthlyRevenue { get; set; }
    public bool PayingTribute { get; set; }

    /// <summary>
    /// Whether the initial payment for the current continuous paying state has already been taken.
    /// Separate from <see cref="PayingTribute"/>: agreement precedes collection by one strategy step,
    /// and two operations reaching that later step must not both award the same payment. A future
    /// transition back to non-payment must clear this together with changing <see cref="PayingTribute"/>.
    /// </summary>
    public bool TributeCollectedForCurrentAgreement { get; set; }

    /// <summary>How hard the owner resists demands. Objective; characters only estimate it.</summary>
    public double Resistance { get; set; }

    public bool Damaged { get; set; }
}

/// <summary>
/// One perceived account conflict, as it reached one listener. Never consulted by any decision.
///
/// Recorded so the milestone's run-wide properties can be asserted directly rather than argued for
/// from the call sites' structure — the gap milestone 005's fifth finding was about. It also makes
/// "does this fire naturally in the accepted scenario" a question a test can answer.
///
/// <b>Amended by milestone 018:</b> this record is no longer developer/test-only. It is now the one
/// source <c>PlayerView.Build</c> reads to project qualitative trust movement, filtered to
/// <c>ListenerId == </c> the viewpoint and reduced to who moved and which direction — never
/// <see cref="AccountConflict.Strength"/> or anything else on <see cref="AccountConflict"/>. See
/// <c>Session.PlayerRelationshipMovement</c>.
/// </summary>
public sealed record PerceivedConflict(string ListenerId, AccountConflict Conflict, DateTime At);

/// <summary>
/// One perceived account agreement, as it reached one listener. Never consulted by any decision.
///
/// Milestone 016. Mirrors <see cref="PerceivedConflict"/> exactly, for the same reason: the run-wide
/// property "trust rose because of a real, fresh, non-repeated corroboration, and only that" needs to
/// be asserted directly against something rather than argued for from call-site structure.
///
/// <b>Amended by milestone 018:</b> the same narrow, filtered player-facing reading
/// <see cref="PerceivedConflict"/> now has — see its doc comment.
/// </summary>
public sealed record PerceivedAgreement(string ListenerId, AccountAgreement Agreement, DateTime At);

/// <summary>
/// One encounter: this character now knows that one exists, because they met.
///
/// Developer/test state. No decision consults it — the consequence lives on the relationship
/// <see cref="Relations.Meet"/> establishes — and it exists so the run-wide invariant "no relationship
/// was created by reading one" can still be asserted directly now that a legitimate route creates
/// all-zero relationships. Same footing, and the same reasoning, as
/// <see cref="PerceivedConflict"/>.
/// </summary>
public sealed record Encounter(string WhoId, string MetId, DateTime At);

/// <summary>Authoritative record of what actually happened. Never consulted by decision-making.</summary>
public sealed record WorldEvent(
    long Id,
    DateTime At,
    string Kind,
    string ActorId,
    string? TargetId,
    string Summary,
    IReadOnlyList<Trace> Traces);

/// <summary>
/// Objective world state.
///
/// Note what is absent: there is no global police-attention or heat scalar. Police interest exists
/// only as claims held by specific characters. That is deliberate — INFORMATION_AND_LEGIBILITY.md's
/// anti-heat-bar tests require that attention and case strength be separable, and the cheapest way
/// to guarantee that is to never create the variable in the first place.
/// </summary>
public sealed class World
{
    public required int Seed { get; init; }
    public DateTime Now { get; set; }
    public EventQueue Queue { get; } = new();

    public Dictionary<string, Character> Characters { get; } = new();
    public Dictionary<string, Business> Businesses { get; } = new();
    public required Organization Org { get; init; }

    public List<WorldEvent> TruthLog { get; } = new();
    public List<DecisionRecord> Decisions { get; } = new();

    /// <summary>
    /// Every message sent through the organisational report channel, including what each sender
    /// chose to withhold. Developer truth — see <see cref="Report"/>. The player-facing layer reads
    /// the recipient's cognition instead.
    /// </summary>
    public List<Report> Reports { get; } = new();

    /// <summary>
    /// Every request for an account, answered or not. See <see cref="InformationRequest"/> — a
    /// question is spent when asked, so this is what stops a character asking the same man the
    /// same thing on every wake.
    /// </summary>
    public List<InformationRequest> Requests { get; } = new();

    /// <summary>
    /// Every RNG occasion key ever used to schedule an ObservationOpportunity, in schedule order.
    /// Milestone 005's uniqueness property — (strategy instance, advance ordinal, trace kind,
    /// observer) identifies at most one opportunity — is asserted directly against this rather than
    /// only argued for, so a future change that lets two opportunities collide on one key is caught
    /// rather than silently redrawing one of them. Developer/test state; never consulted by any
    /// decision.
    /// </summary>
    public List<string> ObservationOccasionKeys { get; } = new();

    /// <summary>
    /// Every perceived account conflict that reached anybody, in the order they occurred. Read by
    /// <c>PlayerView.Build</c> since milestone 018, filtered to one listener at a time — see
    /// <see cref="PerceivedConflict"/>.
    /// </summary>
    public List<PerceivedConflict> AccountConflicts { get; } = new();

    /// <summary>
    /// Every perceived account agreement that reached anybody, in the order they occurred. Read by
    /// <c>PlayerView.Build</c> since milestone 018, filtered to one listener at a time — see
    /// <see cref="PerceivedAgreement"/>. Mirrors <see cref="AccountConflicts"/>.
    /// </summary>
    public List<PerceivedAgreement> AccountAgreements { get; } = new();

    /// <summary>
    /// Every encounter that established one character knows another exists, in order. Developer and
    /// test state only — see <see cref="Encounter"/>.
    /// </summary>
    public List<Encounter> Encounters { get; } = new();

    private long _nextWorldEventId = 1;
    private long _nextAssignmentId = 1;
    private long _nextDecisionId = 1;
    private long _nextReportId = 1;
    private long _nextRequestId = 1;

    public Character Get(string id) => Characters[id];
    public Character? Find(string id) => Characters.TryGetValue(id, out var c) ? c : null;

    public long NextAssignmentId() => _nextAssignmentId++;
    public long NextDecisionId() => _nextDecisionId++;
    public long NextReportId() => _nextReportId++;
    public long NextRequestId() => _nextRequestId++;

    public WorldEvent Record(
        string kind,
        string actorId,
        string? targetId,
        string summary,
        params Trace[] traces)
    {
        var ev = new WorldEvent(_nextWorldEventId++, Now, kind, actorId, targetId, summary, traces);
        TruthLog.Add(ev);
        return ev;
    }

    public IEnumerable<Character> ActiveCharacters()
        => Characters.Values.Where(c => c.Tier == Tier.Active).OrderBy(c => c.Id, StringComparer.Ordinal);

    public IEnumerable<Business> BusinessesIn(string districtId)
        => Businesses.Values.Where(b => b.DistrictId == districtId).OrderBy(b => b.Id, StringComparer.Ordinal);
}
