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
    IReadOnlyList<string> SalienceNotes)
{
    /// <summary>
    /// What this character actually did, as structured fields — the unit of comparison for
    /// "do two configurations behave differently".
    ///
    /// Built from the record rather than from rendered text on purpose. `--compare` used to answer
    /// that question by hashing the whole printed trace, and reported "five configurations, five
    /// distinct histories" while two of them chose the identical action at every single decision and
    /// differed only in scores and wording reaching the summary. A claim nothing checks is milestone
    /// 006's closing lesson, and that one was sitting in the runner.
    ///
    /// The subject claims are carried as two fields, not coalesced into one. What a question is
    /// about and what a report is an answer to are different things, and this project's most
    /// repeated defect is two distinct values being collapsed into one.
    ///
    /// Score is deliberately excluded: this compares what he did, not how nearly he did something
    /// else. A change that moves every score without changing a single action is exactly the case
    /// this must be able to report as behaviourally identical.
    /// </summary>
    public string ChosenActionSignature()
    {
        if (Chosen is not { Candidate: var c })
            return $"{At:O}|{ActorId}|nothing-was-open";

        return $"{At:O}|{ActorId}|{c.Kind}|{c.Id}|{c.TargetId}|" +
               $"{c.AboutClaim}|{c.AnsweringClaim}|{c.Candor}";
    }
}
