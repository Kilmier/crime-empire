namespace CrimeSim.Session;

using CrimeSim.Domain;

/// <summary>
/// One thing the controlled character could do, as he would put it to himself.
///
/// <see cref="Id"/> is an <b>opaque token</b>, not the candidate id. The candidate id is developer
/// data: it interpolates <c>Claim.ToString()</c>, which prints the truth-log <c>EventId</c> as a
/// <c>#7</c> correlation suffix, so <c>answer:salvatore:PersonUsedViolence(tommy -&gt;
/// bellini-grocery#7)</c> would carry that counter across the boundary. The token is a stable hash of
/// the candidate id — deterministic, so identical runs produce identical tokens, and meaningless, so
/// it reveals nothing. <see cref="SimulationSession.Choose"/> maps it back.
///
/// <see cref="Description"/> is built from the candidate's typed fields by
/// <see cref="PlayerOption"/>, never from the generator's own wording. Neither the order of these
/// options nor anything in them says which one the model would take.
/// </summary>
public sealed record PendingOption(string Id, string Description);

/// <summary>
/// A deliberation stopped at the last question, waiting for a person to answer it.
///
/// <b>What is here, and why each thing is allowed.</b>
///
///  - <see cref="Options"/> — the candidates that survived his own redundancy, salience, knowledge,
///    capability and access filters, ordered by candidate id and worded from structured fields.
///    Authorized explicitly by milestone 009's scope: this is the player standing where the character
///    stands, which is what shared agency means.
///  - <see cref="Occasion"/> and <see cref="Focus"/> — <b>nullable</b>, chosen from
///    <see cref="PlayerOccasion"/>'s closed vocabulary, and null whenever nothing can be said without
///    asserting something he may not know. Milestone 009 originally passed
///    <c>ScheduledEvent.Cause</c> and <c>Agenda.Description</c> through verbatim, which handed the
///    owner of a delegated operation its outcome — "Bellini's grocery held out against force" — on a
///    day nobody had told him anything. See <see cref="PlayerOccasion"/> for the full account.
///
/// <b>What is deliberately absent.</b> Every score and score component; the noise draw; the
/// candidates that were rejected and the stage that rejected them; the salience notes; the trigger's
/// authored cause; the agenda's <c>Reason</c>; the raw candidate ids; and any fact the character does
/// not hold. The developer-facing form of all of it is
/// <see cref="CrimeSim.Decision.PreparedDecision"/> and none of it comes through here.
/// </summary>
public sealed record PendingDecision(
    DateTime At,
    string ActorId,
    string ActorName,
    string ActorRole,
    Pronouns ActorPronouns,
    string? Occasion,
    string? Focus,
    IReadOnlyList<PendingOption> Options)
{
    /// <summary>
    /// Frozen at construction. An <c>IReadOnlyList&lt;T&gt;</c> backed by a <c>List&lt;T&gt;</c> can
    /// be cast straight back and mutated, so the guarantee has to be a property of this type rather
    /// than a habit of whatever built it. See <see cref="Frozen"/>.
    /// </summary>
    public IReadOnlyList<PendingOption> Options { get; init; } = Frozen.List(Options);
}
