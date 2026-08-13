namespace CrimeSim.Decision;

using CrimeSim.Domain;
using CrimeSim.Sim;

/// <summary>
/// The developer-only record of one deliberation. It captures every stage the pipeline separates,
/// including the options that were discarded and why.
///
/// This is explicitly NOT player-facing. It contains the true trigger, the real score components
/// and the actual chosen strategy; showing it to a player would hand them the hidden state that
/// imperfect information depends on. The player-facing account is assembled separately in step 1b
/// from what a source could plausibly know.
/// </summary>
public sealed record DecisionRecord(
    long Id,
    DateTime At,
    string ActorId,
    string ActorName,
    long TriggerEventId,
    EventKind TriggerKind,
    string Trigger,
    Agenda Agenda,
    IReadOnlyList<InformationRecord> BeliefsUsed,
    IReadOnlyList<Candidate> Generated,
    IReadOnlyList<Rejection> Rejected,
    IReadOnlyList<ScoreBreakdown> Scored,
    ScoreBreakdown? Chosen,
    string Outcome,
    IReadOnlyList<string> Reconsideration,
    IReadOnlyList<string> SalienceNotes);
